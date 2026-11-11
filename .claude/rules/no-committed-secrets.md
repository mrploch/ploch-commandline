# Never Commit a Credential (Hard Rule)

**Scope:** every repository under the mrploch workspace root (`C:\DevNet\my\mrploch` on Windows, `~/DevNet/mrploch` on macOS), every file, every branch, every commit — including local-only commits that have not been pushed. No exceptions.

**Authority:** this rule is **non-negotiable** and overrides convenience, "it's just local", "it's a private repo", and "I'll clean it up before pushing". It was written on 2026-09-12 after three separate incidents in this workspace.

---

## The Rule

**A literal credential value must never enter a git repository.** Not in a tracked file, not in a commit, not in a branch you intend to squash, not in a file you plan to delete in the next commit.

A credential is any value that grants access to something. In this workspace that has concretely meant:

| Kind | Shape seen here |
|---|---|
| GitHub PAT (classic) | `ghp_` + 36 chars |
| GitHub PAT (fine-grained) | `github_pat_` + … |
| ContextStream API key | `cbiq_` + … |
| SonarQube / SonarCloud token | 40 hex chars |
| Codacy account/project token | 20 chars |
| NuGet API key | `oy2` + … |
| Ahrefs / third-party API key | vendor-specific |
| Connection string with credentials | `Password=`, `Pwd=`, `AccountKey=` |
| Private keys / certificates | `.pem`, `.pfx`, `.p12`, `BEGIN PRIVATE KEY` |
| Qodana / Codecov / Coveralls tokens | vendor-specific |

If a value would be embarrassing, dangerous, or costly in the hands of a stranger, it is a credential.

---

## Three fallacies that caused every incident here

### 1. "It's a private repository, so it's fine."

Private today is not private forever — repositories get opened, forked into other orgs, transferred, or cloned onto a machine that is later compromised. A private repo also gives every current and future collaborator, and every CI job and every installed GitHub App, read access to the value. **Visibility is not a security control for secrets.**

Real case: the SonarQube token in use across this workspace is committed and tracked in `mrploch/local-setup` (`ai/profile/.codex/config.toml`), a private repo. It happened to be in an unpushed local commit, so it never left the machine — that was luck, not process.

### 2. "I removed it in a later commit."

**Git history is permanent.** Removing a secret in a subsequent commit leaves the original blob reachable forever via the commit that introduced it. Anyone can read it with `git log -S`, `git show <sha>`, or by cloning. Removal stops *future* exposure only.

Real case: [ploch-commandline#41](https://github.com/mrploch/ploch-commandline/pull/41) replaced a committed ContextStream API key with `${env:CONTEXTSTREAM_API_KEY}`. Correct fix going forward — but the repository is **public**, so the original key remains publicly readable in history. The fix was necessary and insufficient on its own; the key also had to be revoked.

### 3. "It's only in a config file nobody looks at."

Secrets do not stay where you put them. They get copied into logs, caches, editor state, and backups — none of which anyone thinks to audit.

Real case: the same SonarQube token appears in **`ai/profile/.codex/logs_2.sqlite`** as well as the config file. A binary SQLite log is invisible to a casual `grep` and to most secret scanners.

---

## What to do instead

Credentials reach code through **indirection**, never a literal:

| Context | Correct mechanism |
|---|---|
| MCP server config (`.mcp.json`) | `${env:VAR_NAME}` expansion, with the real value in the shell environment or OS keychain |
| GitHub Actions | `${{ secrets.NAME }}`, scoped to the **step** that needs it (see [`response-links.md`](./response-links.md) sibling rules and the token-scoping precedent in `release.yml`) |
| .NET local development | `dotnet user-secrets`, or environment variables — never `appsettings.json` |
| NuGet feed credentials | `%VAR%` in `nuget.config` + the variable set outside the repo |
| CLI tools | the tool's own credential store — `sonar auth login` uses the OS keychain, `gh auth login` uses the system credential helper |
| Local machine config that cannot be parameterised | keep the file **out** of git: `.gitignore` it and commit a `*.example` variant with placeholders |

This is the same requirement [`local-setup-scripts.md`](./local-setup-scripts.md) already states for setup scripts ("never write secrets into the repo — token values arrive via parameter/env/ignored file"); this rule generalises it to every file in every repo.

---

## Before every commit

Staging is the last cheap moment to catch this. Check the **staged diff**, not the working tree:

```bash
# Review what is actually about to be committed
git diff --cached

# Pattern sweep over the staged content
git diff --cached -U0 | grep -nEi \
  'ghp_[A-Za-z0-9]{36}|github_pat_|cbiq_|oy2[a-z0-9]{43}|BEGIN [A-Z ]*PRIVATE KEY|(password|pwd|secret|api[_-]?key|token|accountkey)[\"'"'"' ]*[:=][\"'"'"' ]*[A-Za-z0-9/+_\-]{16,}'
```

Any hit stops the commit. `git reset <file>`, parameterise the value, then re-stage.

**Never stage a whole directory blind.** `git add -A` and `git add .` are how `.codex/config.toml` and a SQLite log got committed in the first place. Stage named paths, then verify with `git diff --cached --name-only` that the list is exactly what you intended. This matters doubly for tool-managed directories (`.codex/`, `.claude/`, `.cursor/`, `ai/profile/`) which write credentials without telling you.

Binary and generated files deserve particular suspicion: `*.sqlite`, `*.db`, `*.log`, `*.bak`, editor state. Prefer `.gitignore`-ing them wholesale over auditing them.

---

## When a credential has already been committed

Order matters. **Revoke first** — everything else is cosmetic until the value is dead.

1. **Revoke or rotate the credential at the issuer**, immediately. Not "replace it in the file" — *revoke the old value* so it cannot be used. Replacing without revoking leaves a live secret in history, which is what makes a "fixed" leak still exploitable.
2. **Determine exposure** before deciding how far to go:
   ```bash
   gh repo view --json visibility --jq .visibility        # public = assume compromised, full stop
   git log --all --oneline -S'<value>'                    # which commits
   git branch -r --contains <sha>                         # did it ever leave the machine?
   ```
   Not pushed and private is recoverable. Pushed, or public at any point, means assume it is compromised regardless of how quickly it was removed.
3. **Remove it from the working tree** and parameterise, as above.
4. **Consider history rewrite** (`git filter-repo`, BFG) only for a repo whose history can safely be rewritten, and understand it invalidates every existing clone and does not retract anything already fetched, forked, or indexed. For anything public, treat rewrite as damage limitation, never as a substitute for revocation.
5. **Record it** — an issue in the affected repo so the rotation is auditable and the next person knows the old value is dead.

---

## Red flags — STOP

- About to run `git add -A` or `git add .` in any repository.
- About to commit anything under `.codex/`, `.claude/`, `.cursor/`, `ai/profile/`, or another tool-managed directory without reading the staged diff.
- About to commit a `.sqlite`, `.db`, `.log`, or `.bak` file.
- About to write a literal token into a `.json`, `.toml`, `.yml`, `.props`, `.config` or `appsettings*.json` file.
- Thinking "it's private, so it's fine" or "I'll take it out before pushing".
- About to "fix" a committed secret by editing the file, without revoking the value at the issuer.
- About to report a leak as resolved because the current value differs from the committed one — confirm the **old** value is revoked, not just superseded.

## Also: do not echo credentials into a session

When inspecting a config file to check which servers or keys it defines, read the **structure**, not the values — parse and print field names, lengths, or masked prefixes. Session transcripts may be captured and persisted to a remote service (this workspace has ContextStream transcript capture enabled), so dumping a config file with live tokens copies them somewhere new. When a value must be compared, compare it in a variable and print only the verdict.
