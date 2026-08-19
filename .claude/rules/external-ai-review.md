# External AI Review — Reviewer Panel & Invocation Contract

**Scope:** every skill or ad-hoc task that asks a model *outside this session* for a review, a second opinion, or a consultation. Covers whole-branch pre-merge reviews, per-fix validation gates, and plan reviews.

**Authority:** this file is the single source of truth for *which* external reviewers exist, *how* each is invoked, and *which model* each runs. Skills reference this rule rather than restating the flags — when a model ID, flag, or fallback changes, it changes **here only**.

Skills that consume this rule: [`dev-finishing-touches`](../skills/dev-finishing-touches/SKILL.md), [`dotnet-dev-finishing-touches`](../skills/dotnet-dev-finishing-touches/SKILL.md), [`implement-issue`](../skills/implement-issue/SKILL.md).

---

## The Panel

Three reviewers, three deliberately different model families. The point of the panel is **provider diversity** — a bug that Codex's and Claude's shared blind spots hide is exactly the bug a third family catches. Running two reviewers from the same family is not a panel.

| # | Reviewer | Transport | Model | Lens it contributes |
|---|----------|-----------|-------|---------------------|
| 1 | **Codex** | MCP — `mcp__codex-cli__review` (preferred) or `mcp__codex-cli__codex` | OpenAI GPT-5.6-Sol / Codex family | OpenAI reasoning; strongest on precise diff-level correctness |
| 2 | **Gemini** | MCP — `mcp__gemini-cli__gemini` (fallback `mcp__gemini__gemini-analyze-code`) | Google Gemini 3.x Pro | Google reasoning; strongest on long-context whole-file sweeps |
| 3 | **Copilot** | **Shell-out** — `copilot` CLI via `Bash` | **xAI Grok 4.6** at `--effort high` | xAI reasoning; strongest on long-horizon multi-step and terminal-native tasks |

**Copilot is not an MCP server.** GitHub Copilot CLI *consumes* MCP servers and can act as an ACP server (`--acp`), but it exposes no MCP interface. It is therefore invoked by shelling out through the `Bash` tool — never via `ToolSearch`/`mcp__*`. Do not go looking for a `mcp__copilot__*` tool; it does not exist.

### Why Grok 4.6 for the Copilot slot

The Copilot slot exists to add a model family the other two reviewers do not already cover, so its model is chosen by **exclusion**:

- **Anthropic models are excluded** — that is the session model running the skill. A Claude reviewing Claude is a mirror, not a second opinion.
- **Gemini models are excluded** — reviewer 2 already covers Google.
- **GPT-5.6-Sol and the Codex family are excluded** — reviewer 1 already covers OpenAI.

That leaves xAI and Moonshot. **Grok 4.6** wins: GitHub added it to Copilot CLI on 2026-08-14 and states it "performed especially well on longer-horizon tasks requiring sustained reasoning and tool use", specifically on terminal-based coding in the CLI — which is exactly the shape of a whole-branch review. **Kimi K3** is the documented alternate if Grok is unavailable or policy-blocked.

**Plan gate:** Grok 4.6 requires Copilot Pro / Pro+ / Max / Business / Enterprise. On **Business and Enterprise the Grok 4.6 policy is off by default** and an administrator must enable it. Grok runs under usage-based billing at provider list pricing, so a triple review costs real credits — that is the accepted price of the panel, not a reason to silently drop a reviewer.

---

## Copilot CLI Invocation Contract

### Canonical command

```bash
copilot -p "$BRIEF" \
  --model grok-4.6 \
  --effort high \
  --allow-all-tools \
  --deny-tool 'write' \
  --deny-tool 'shell(git commit)' --deny-tool 'shell(git push)' \
  --deny-tool 'shell(git add)'    --deny-tool 'shell(git reset)' \
  --deny-tool 'shell(git checkout)' --deny-tool 'shell(git restore)' \
  --deny-tool 'shell(gh pr create)' --deny-tool 'shell(gh pr merge)' \
  --deny-tool 'shell(gh pr comment)' --deny-tool 'shell(gh pr review)' \
  --disable-builtin-mcps \
  --no-ask-user \
  --no-color \
  --log-level none \
  -s \
  -C "$REPO_ROOT"
```

### Why each flag is there

| Flag | Reason it is mandatory |
|------|------------------------|
| `-p, --prompt` | Non-interactive mode; the CLI runs the prompt and exits. Without it the CLI opens a TUI and the tool call hangs. |
| `--allow-all-tools` | **Required by the CLI for non-interactive mode.** Without it Copilot blocks on an approval prompt that nothing can answer. |
| `--deny-tool …` | **Denial always takes precedence over `--allow-all-tools`** — this is the documented precedence rule and it is what makes a read-only reviewer possible despite the blanket allow. `write` covers all file create/modify tools; the `shell(...)` patterns block the mutating `git`/`gh` subcommands while leaving `git diff`, `git log`, `gh pr view` available for the reviewer to gather its own evidence. |
| `--model grok-4.6` | Pins the reviewer's lens. Never leave this to `auto` — auto-selection may land on a model the panel already covers, collapsing the diversity the panel exists for. |
| `--effort high` | Reasoning budget is a **separate axis from model choice** — a high-capability model at low effort skims. **`high` is Grok 4.6's ceiling**, so there is nothing deeper to escalate to; see Supported effort levels below. |
| `--disable-builtin-mcps` | Suppresses the built-in `github-mcp-server`, which is slow to start and a frequent source of startup errors. The reviewer receives its context inline and does not need it. |
| `--no-ask-user` | Disables the `ask_user` tool so the agent works autonomously instead of stalling on a clarifying question no one can answer. |
| `-s, --silent` | Emits only the agent response, no session statistics — required for clean parsing. |
| `--no-color` / `--log-level none` | Strips ANSI escapes and log chatter from the captured output. |
| `-C "$REPO_ROOT"` | Anchors the working directory so relative paths in the findings match the repo. |

### Structured output

For programmatic triage, add `--output-format json` (JSONL — one JSON object per line, **not** a single JSON document; parse line-by-line). For a human-readable review that gets triaged by hand, the default `text` with `-s` is sufficient and cheaper to read. Prefer `text` unless the skill genuinely parses fields.

`--share <path>` writes the full session transcript to markdown after a non-interactive run — useful when a reviewer's findings need to be attached to a PR or a report.

### Post-run write check (mandatory)

`--deny-tool 'write'` does **not** cover shell redirections (`echo … > file`), which `--allow-all-tools` permits. After every Copilot review, confirm the reviewer changed nothing:

```bash
git status --porcelain
```

The output must be **identical** to what it was before the review. If it is not, treat the difference as an unintended write: revert it, and record the incident in the skill's report. Never commit a file a reviewer touched.

---

## Preflight — Run Before Every Panel

Copilot CLI fails *loudly but late*: an expired session surfaces as `Error: Failed to load models … 421 "Misdirected Request"` on every invocation, including the review itself. Detect it cheaply first:

```bash
copilot -p "Reply with exactly: READY" --model grok-4.6 --effort low --disable-builtin-mcps \
  --allow-all-tools --deny-tool 'write' --no-ask-user -s --log-level none
```

**Supported effort levels (verified on Copilot CLI 1.0.80, 2026-08-19):** Grok 4.6 accepts **only `low`, `medium`, `high`**. The CLI's `--effort` choice list is generic across all models, but each model supports a subset and rejects the rest at request time:

| Value | Grok 4.6 |
|---|---|
| `none` | Rejected — `CAPIError: 400 ... This model does not support ``reasoning_effort`` value ``none``.` |
| `minimal` | Rejected — `Error: Reasoning effort "minimal" is not supported for model "grok-4.6".` |
| `low` / `medium` / `high` | Supported. `high` is the ceiling. |
| `xhigh` / `max` | Rejected — same "not supported for model" error. |

**Never escalate a Grok review to `xhigh` or `max`** — the call fails outright rather than degrading. If a deeper pass than `high` is genuinely wanted, change the *model*, not the effort.

**Match the preflight on "output contains `READY`", not on equality.** Workspace MCP servers (ContextStream in particular) can make the agent narrate a preamble before answering, so an exact-match check produces false negatives.

| Preflight result | Meaning | Action |
|---|---|---|
| `READY` | Auth, model access and policy are all good | Proceed with the review |
| `421 Misdirected Request` / `Failed to load models` | Copilot OAuth session is stale, or a `ghp_…` PAT in `GITHUB_TOKEN`/`GH_TOKEN`/`COPILOT_GITHUB_TOKEN` is being preferred over the CLI login (`api.githubcopilot.com` rejects classic PATs outright) | Retry once with those variables stripped (`env -u GITHUB_TOKEN -u GH_TOKEN -u COPILOT_GITHUB_TOKEN copilot …`). If it still fails, the user must run `copilot` interactively and `/login`. Follow the fallback ladder. |
| `Reasoning effort "…" is not supported for model` | The `--effort` value is outside the model's supported subset | Drop to a supported level (`low`/`medium`/`high` for Grok 4.6). Do **not** treat this as the reviewer being unavailable. |
| Unknown-model error | The `grok-4.6` identifier is wrong for this account, or the Grok policy is disabled | Ask the user to run `/model` in an interactive `copilot` session and report the exact identifier; fall back to **Kimi K3**. Update this rule with the confirmed string. |

**Model identifier — verified.** GitHub does not publish the literal `--model` strings, but `grok-4.6` is confirmed working on this machine (Copilot CLI 1.0.80, 2026-08-19):

- `--model` **is** validated — an unknown ID fails fast with `Error: Model "…" from --model flag is not available.`, so a typo can never silently fall back to a different model.
- `copilot -p "State only your underlying model name and vendor." --model grok-4.6` returns **`Grok 4.6, xAI`**.

Re-run that identity probe after any Copilot CLI upgrade or account change. The authoritative catalogue is `/model` inside an interactive `copilot` session.

---

## The Shared Review Contract

All three reviewers receive **the same package**, in this order, and the brief **last**:

1. **Repo primer** — what the project is, its conventions, its quality bar.
2. **Branch intent** — PR title and body (or intended description), linked issue, branch name.
3. **The complete diff** — `git diff "$BASE_BRANCH"...HEAD` plus any uncommitted changes in scope.
4. **Full current contents of every modified file** — not just hunks; reviewers need surrounding code.
5. **Verification already performed** — build/test/analyser output, coverage, checks already run.
6. **The review brief** — what to review, and the response format below.

**Entire context first, then the review request.** If the package exceeds a transport's input limit, split it into a numbered multi-part upload ("context part 1/3…") and send the brief only after the final part.

### Response format demanded of every reviewer

Each finding returns: **severity** (`must-fix` / `should-fix` / `nit`), **file + line**, **what is wrong**, **evidence**, **concrete suggested fix**. A category with no findings is stated explicitly ("no security issues found") rather than omitted. Each reviewer ends with a verdict: `APPROVE`, `APPROVE_WITH_NOTES`, or `REQUEST_CHANGES`.

### Dispatch and triage

- **Dispatch concurrently.** Put the two MCP reviewer calls and the Copilot `Bash` call in a **single tool-call block** so all three run at once. Serial dispatch triples the wall-clock cost for no benefit.
- **Merge and deduplicate.** Same file + line + concern from two reviewers → one item, crediting both. Agreement between two independent families raises confidence; note it.
- **One tracked item per `must-fix` and `should-fix`**, tagged with its source (`codex` / `gemini` / `copilot`). `nit`s are batched into one item and either applied where cheap or explicitly declined.
- **Triage each finding** using the seven-category model in [`pr-checks-completion-gate.md`](./pr-checks-completion-gate.md) § "Conversations Must Be Addressed". A declined finding is recorded **with the evidence for declining** — never silently dropped.
- **Verdict handling.** Any reviewer returning `REQUEST_CHANGES` blocks progress until every one of its `must-fix` items is fixed or declined with auditable evidence, then that reviewer is re-run on the updated diff for `APPROVE` / `APPROVE_WITH_NOTES` (or an explicit user override).
- **Disagreement between reviewers** is judged on the evidence. If it is genuinely ambiguous *and* impactful, surface both positions to the user rather than picking silently.

---

## Fallback Ladder — When a Reviewer Is Unavailable

Applied per reviewer, in order. **Never silently downgrade the panel.**

1. **Retry once.** MCP reviewers: re-load via `ToolSearch`. Copilot: re-run the preflight with the token environment variables stripped.
2. **Substitute within the family.** Codex: try an alternative model before concluding it is unavailable (`gpt-5.5` is a known-good setting on this machine when the `*-codex` defaults fail under ChatGPT-account auth). Copilot: fall back to **Kimi K3**, which preserves provider diversity.
3. **Substitute the transport.** Gemini: `mcp__gemini__gemini-analyze-code` in place of `mcp__gemini-cli__gemini`.
4. **Ask the user.** State which reviewer is down, what was tried, and whether to proceed with a reduced panel. **Record the answer in the skill's final report.**
5. **Last resort** — if the user approves proceeding without an external reviewer, substitute an independent local review agent (e.g. `feature-dev:code-reviewer`) with the same brief, and record the substitution in both the PR description and the completion report.

A reviewer that was skipped, substituted, or downgraded is **always** named in the final report with the reason. "The panel ran" is only true when all three returned a verdict.

---

## Red Flags — STOP

- About to **run fewer than three reviewers** without the user's explicit sign-off.
- About to **let `--model` default to `auto`** — this silently collapses the panel's provider diversity.
- About to **omit `--effort`** — the default reasoning budget is not a thorough review.
- About to **run Copilot without the `--deny-tool` set**, or to skip the post-run `git status --porcelain` check.
- About to **commit a file a reviewer modified**.
- About to **silently drop a `must-fix`** because it is inconvenient, or because "it's only a bot".
- About to **report the review as passed while the preflight is failing** — a 421 is a broken reviewer, not a passing one.
