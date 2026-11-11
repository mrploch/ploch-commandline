---
metadata:
  version: "1.0"
---

# Never Commit a Credential (Hard Rule)

**Scope:** every repository under the mrploch workspace root (for example `C:\DevNet\my\mrploch` on Windows or `~/devnet/my/mrploch` on macOS), every file, every branch, every commit — including local-only commits that have not been pushed. No exceptions.

**Authority:** this rule is **non-negotiable** and overrides convenience, "it's just local", "it's a private repo", and "I'll clean it up before pushing". It was written on 2026-09-12 after three separate incidents in this workspace.

---

## The Rule

**A literal credential value must never enter a git repository.** Not in a tracked file, not in a commit, not in a branch you intend to squash, not in a file you plan to delete in the next commit.

A credential is any value that grants access to something. In this workspace that has concretely meant:

| Kind | Shape seen here |
|---|---|
| GitHub PAT (classic) and other GitHub tokens | `ghp_` + 36 chars; also `gho_` (OAuth, as used by the GitHub and Copilot CLIs), `ghu_`, `ghs_`, `ghr_` |
| GitHub PAT (fine-grained) | `github_pat_` + … |
| ContextStream API key | `cbiq_` + … |
| SonarQube / SonarCloud token | 40 hex chars |
| Codacy account/project token | 20 chars |
| NuGet API key | `oy2` + … |
| Ahrefs / third-party API key | vendor-specific |
| Connection string with credentials | `Password=`, `Pwd=`, `AccountKey=` |
| Private keys / certificates | `.pem`, `.pfx`, `.p12`, a `-----BEGIN … PRIVATE KEY-----` block |
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
| MCP server config | The client's own expansion syntax, with the real value in the shell environment or OS keychain: `${VAR_NAME}` (or `${VAR_NAME:-default}`) in Claude Code's `.mcp.json`, `${env:VAR_NAME}` in VS Code's `mcp.json`. Check the client's documentation; a syntax it does not expand is passed through as a literal string. |
| GitHub Actions | `${{ secrets.NAME }}`, scoped to the **step** that needs it (see the token-scoping precedent in [`release.yml`](../../.github/workflows/release.yml)) |
| .NET local development | `dotnet user-secrets`, or environment variables — never `appsettings.json` |
| NuGet feed credentials | `%VAR%` in `nuget.config` + the variable set outside the repo |
| CLI tools | the tool's own credential store — `sonar auth login` uses the OS keychain, `gh auth login` uses the system credential helper |
| Local machine config that cannot be parameterised | keep the file **out** of git: `.gitignore` it and commit a `*.example` variant with placeholders |

This is the same requirement [`local-setup-scripts.md`](./local-setup-scripts.md) already states for setup scripts ("never write secrets into the repo — token values arrive via parameter/env/ignored file"); this rule generalises it to every file in every repo.

---

## Before every commit

Staging is the last cheap moment to catch this. Check the **staged diff**, not the working tree:

Sweep **first, quietly**, and only then look at the diff. Printing the diff, or printing matching lines, would copy a staged credential into the session transcript, which may be persisted remotely (see the end of this rule) (PR #70 review):

```bash
# 1. Quiet sweep: names files with a hit, never the matched values
PATTERN='gh[pousr]_[A-Za-z0-9]{36}|github_pat_[A-Za-z0-9_]{22,}|cbiq_[A-Za-z0-9]{16,}|oy2[a-z0-9]{43}|BEGIN [A-Z ]*PRIVATE KEY|(authorization|proxy-authorization)[[:blank:]"'"'"']*[:=][[:blank:]"'"'"']*(bearer|basic|token) +[A-Za-z0-9._~+/=-]{8,}|bearer +[A-Za-z0-9._~+/-]{16,}=*|(password|pwd|secret|api[_-]?key|token|accountkey)[[:blank:]"'"'"']*[:=][[:blank:]"'"'"']*[^[:space:]"'"'"']{12,}|cleartextpassword["'"'"'[:blank:]]+value=["'"'"']?[^[:space:]"'"'"'/>]{8,}'
# No backslashes inside the bracket expressions: in ERE a '\' there is a literal character, which
# would split a value such as a domain password at its backslash.
# Both sides of every staged path are scanned: the index entry (what will be committed) and the
# HEAD entry (what step 2 would print as removed lines, so replacing or deleting a credential does
# not print it). --no-renames lists a rename as delete + add, so the old path is scanned too.
# Each side is read from Git's own metadata (ls-files -s, ls-tree), so "absent" is never guessed
# from a failed read. The BLOB is scanned, not the textual diff: git diff reduces a binary file
# (.sqlite, .db, .pfx) to a "Binary files differ" marker, which would hide a credential in it.
# Run from the top level with --no-relative: with diff.relative=true, a sweep started in a
# subdirectory would silently skip staged files outside it. Every failure prints a line, so an
# empty result always means "scanned and clean", never "could not scan".
ROOT=$(git rev-parse --show-toplevel 2>/dev/null) || ROOT=
HITS=$( set +e +x -f   # +x: no trace output; each status is checked explicitly (errexit would skip the checks); -f: no globbing of path words
export GIT_LITERAL_PATHSPECS=1   # a staged name containing * or ? is a path, not a pattern
{ [ -n "$ROOT" ] && cd "$ROOT"; } || { echo 'NOT IN A GIT WORK TREE (sweep not run)'; exit; }
U=$(git ls-files -u) || { echo 'INDEX UNREADABLE (sweep not run)'; exit; }
[ -z "$U" ] || { echo 'UNMERGED ENTRIES (resolve the conflict first; sweep not run)'; exit; }
if git rev-parse -q --verify 'HEAD^{commit}' >/dev/null; then HAS_HEAD=yes   # HEAD names a readable commit
else
  REF=$(git symbolic-ref -q HEAD); git show-ref -q --verify "$REF"; rc=$?   # show-ref: 1 = no such ref, 128 = error
  if [ -n "$REF" ] && [ "$rc" = 1 ]; then HAS_HEAD=no                      # unborn branch: no commit yet
  else echo 'HEAD UNREADABLE (sweep not run)'; exit; fi
fi
scan() {  # $1 = object id, $2 = label. Silent only when the blob was read and did not match.
  git cat-file blob "$1" 2>/dev/null | LC_ALL=C grep -aqEi "$PATTERN"
  st=("${PIPESTATUS[@]}")                          # grep: 0 = match, 1 = no match, 2 = error
  case "${st[1]}" in
    (0) printf 'POSSIBLE SECRET in %s (value not shown)\n' "$2" ;;
    (1) [ "${st[0]}" = 0 ] || printf 'UNREADABLE %s (not scanned)\n' "$2" ;;
    (*) printf 'SCAN ERROR in %s (not scanned)\n' "$2" ;;
  esac
}
side() {  # $1 = "mode object ..." line from ls-files -s / ls-tree (empty: no such side), $2 = label
  [ -n "$1" ] || return 0
  set -- $1 "$2"; m=$1; o=$2
  case "$2" in (blob|tree|commit) o=$3 ;; esac     # ls-tree prints "mode type object", ls-files -s "mode object stage"
  shift $(( $# - 1 ))                              # leaves only the label
  case "$m" in
    (100644|100755|120000)                         # file or symlink: scan it (a key store is only reported)
      if [ "$ks" = yes ]; then printf 'CREDENTIAL FILE TYPE %s (review manually)\n' "$1"; else scan "$o" "$1"; fi ;;
    (040000|160000) ;;                             # a directory's files are listed separately; a submodule holds no content here
    (*) printf 'UNEXPECTED ENTRY %s (mode %s, not scanned)\n' "$1" "$m" ;;
  esac
}
# NUL-delimited, so a staged name with spaces is read whole.
git diff --cached --no-relative --no-renames --name-only -z --diff-filter=ACDMT | while IFS= read -r -d '' f; do
  # Key-store extensions in any letter case (cert.PFX too), without tr: bash 3.2 has no ${f,,}.
  # The leading '(' keeps bash 3.2 (macOS) from ending $( ) at the pattern's ')'.
  case "$f" in
    (*.[pP][fF][xX]|*.[pP]12|*.[pP][eE][mM]|*.[kK][eE][yY]|*.[jJ][kK][sS]|*.[kK][eE][yY][sS][tT][oO][rR][eE]|*.[sS][nN][kK])
      ks=yes ;;   # binary key stores cannot be pattern-scanned: each side is reported, not scanned
    (*) ks=no ;;
  esac
  E=$(git ls-files -s -- "$f") || { printf 'UNREADABLE %s (index entry, not scanned)\n' "$f"; continue; }
  side "$E" "$f"
  [ "$HAS_HEAD" = yes ] || continue
  E=$(git ls-tree --full-tree HEAD -- "$f") || { printf 'UNREADABLE %s (committed entry, not scanned)\n' "$f"; continue; }
  side "$E" "$f (committed version)"
done
st=("${PIPESTATUS[@]}")   # both sides of the pipe: the listing and the scanning loop
[ "${st[0]}" = 0 ] || echo 'LISTING STAGED FILES FAILED (sweep incomplete)'
[ "${st[1]}" = 0 ] || echo "SCAN LOOP FAILED (status ${st[1]}, sweep incomplete)"
) || HITS="SWEEP ABORTED (status $?, not completed)
$HITS"   # a killed or failed sweep never reads as clean

# 2. Only after a clean sweep: name the staged files and their size of change, without content.
#    A clean sweep is not proof of safety (see below), so content is shown per file, on request,
#    only for files whose content you wrote yourself in this session.
if [ -n "$HITS" ]; then
  printf '%s\n' "$HITS"; echo "Commit stopped: inspect the files above in the editor."; false
else
  git -C "$ROOT" diff --cached --no-relative --no-ext-diff --no-textconv --submodule=short --stat
fi
# To read one file's staged change: --no-ext-diff --no-textconv stop a diff driver or textconv filter
# (sqlite3 .dump, sops, gpg) decoding what the sweep saw raw; --submodule=short stops a submodule's
# own, unscanned changes being expanded.
#   git -C "$ROOT" diff --cached --no-relative --no-ext-diff --no-textconv --submodule=short -- "$FILE"
```

Any hit stops the commit, and so does **every** line the sweep prints — `POSSIBLE SECRET`, `CREDENTIAL FILE TYPE`, `UNREADABLE`, `SCAN ERROR`, `UNEXPECTED ENTRY`, `UNMERGED ENTRIES`, `INDEX UNREADABLE`, `HEAD UNREADABLE`, `NOT IN A GIT WORK TREE`, `LISTING STAGED FILES FAILED`, `SCAN LOOP FAILED`, `SWEEP ABORTED`: an unscanned file is never treated as clean. **A clean sweep is not proof of safety:** a pattern scan misses credentials in formats it does not know (a 40-hex Sonar token, for one), so still review each staged file in the editor, and treat any file holding keys, certificates or connection strings as needing a manual decision. Do not print the hit to inspect it: open the file in the editor. The patterns require a credential-shaped body after each prefix, so documented prefixes and placeholders (`ghp_…`, `github_pat_` + …) do not match; a hit that the editor shows to be placeholder text is a false positive, and the commit may go ahead. A hit labelled **`(committed version)`** — a `POSSIBLE SECRET` or a `CREDENTIAL FILE TYPE` — is different: the value is already in history and the staged change probably removes it. **Never unstage that file** (that would put the credential back in the commit); follow [When a credential has already been committed](#when-a-credential-has-already-been-committed), and never print that file's diff. Once the old value is revoked at the issuer, the commit that removes it may go ahead. For a hit on the staged version that is a real credential, unstage it with `git reset -- "$FILE"` (quoted, after `--`, so no file name can be read as an option), parameterise the value and re-stage; if that value was ever committed before, follow the revocation steps below first.

**Never stage a whole directory blind.** `git add -A` and `git add .` are how `.codex/config.toml` and a SQLite log got committed in the first place. Stage named paths, then verify with `git diff --cached --name-only` that the list is exactly what you intended. The same applies to every other diff an agent prints, such as a branch or staged diff read for review: run a quiet check first, and print nothing if it reports anything. Run it in the same shell as `PATTERN` above, with `BASE_REF` set to the **fetched base ref** (for example `BASE_REF=origin/main` after `git fetch origin`); each `Bash` call is a new shell, so paste all three together. It exits non-zero whenever it prints anything:

```bash
( set +e +x -f   # no errexit (each status is read below), no trace output, no globbing
  [ -n "${PATTERN:-}" ] || { echo "PATTERN NOT SET (not checked)"; exit 1; }
  if [ -z "${BASE_REF:-}" ] || ! git rev-parse -q --verify "${BASE_REF}^{commit}" >/dev/null; then
    echo "DIFF FAILED: BASE_REF is unset or not a commit (not checked)"; exit 1
  fi
  found=no
  for RANGE in "$BASE_REF...HEAD" "--cached" ""; do   # committed branch changes, staged, unstaged
    # --text: binary hunks are scanned too; --no-relative: the whole repository, whatever the folder
    git diff --no-relative --text --no-ext-diff --no-textconv --submodule=short ${RANGE:+"$RANGE"} -- \
      | LC_ALL=C grep -qaEi "$PATTERN"
    st=("${PIPESTATUS[@]}")                     # grep: 0 = match, 1 = no match, 2 = error
    case "${st[1]}" in
      (0) echo "POSSIBLE SECRET in git diff ${RANGE:-(unstaged)} (not printed)"; found=yes ;;
      (1) [ "${st[0]}" = 0 ] || { echo "DIFF FAILED for git diff ${RANGE:-(unstaged)} (not checked)"; found=yes; } ;;
      (*) echo "SCAN ERROR on git diff ${RANGE:-(unstaged)} (not checked)"; found=yes ;;
    esac
  done
  [ "$found" = no ] )
```

Print those diffs only when this loop printed nothing.

This matters doubly for tool-managed directories (`.codex/`, `.claude/`, `.cursor/`, `ai/profile/`) which write credentials without telling you.

Binary and generated files deserve particular suspicion: `*.sqlite`, `*.db`, `*.log`, `*.bak`, editor state. Prefer `.gitignore`-ing them wholesale over auditing them.

---

## When a credential has already been committed

Order matters. **Revoke first** — everything else is cosmetic until the value is dead.

1. **Revoke or rotate the credential at the issuer**, immediately. Not "replace it in the file" — *revoke the old value* so it cannot be used. Replacing without revoking leaves a live secret in history, which is what makes a "fixed" leak still exploitable.
2. **Determine exposure** before deciding how far to go:
   ```bash
   gh repo view --json visibility --jq .visibility        # public = assume compromised, full stop
   git branch -r --contains <sha>                         # did it ever leave the machine?
   ```

   Which commits hold the value — **run this yourself, in your own terminal, pasted on its own** (not through `!` in Claude Code, which has no terminal for `read`). It runs under `bash` whatever your login shell is (macOS defaults to zsh, which has no `PIPESTATUS`), reads the value from the terminal at a silent prompt, and keeps it out of every command line:

   ```bash
   bash <<'SEARCH'
   ( set +e +a +x +v; unset LEAKED   # +a/unset: never exported; +x/+v: no trace prints the value;
     # the value lives only in this subshell and is gone when it ends, even on Ctrl-C
     printf 'Value (not echoed): ' >&2
     IFS= read -rs LEAKED < /dev/tty || { echo 'NO INPUT (search not run)'; exit 1; }; echo >&2
     LEAKED=${LEAKED%$'\r'}                          # a CRLF paste
     [ -n "$LEAKED" ] || { echo 'EMPTY INPUT (search not run)'; exit 1; }
     TOP=$(git rev-parse --show-toplevel 2>/dev/null) && [ -n "$TOP" ] && cd -- "$TOP" \
       || { echo 'NOT IN A GIT WORK TREE (search not run)'; exit 1; }
     # Every object in the repository, reachable or not, binary files included. Each blob holding the
     # value is printed with the commits that add or remove it (git log --find-object).
     git cat-file --batch-all-objects --batch-check='%(objecttype) %(objectname)' | while read -r type id; do
       [ "$type" = blob ] || continue
       git cat-file blob "$id" | LC_ALL=C grep -qaF -f <(printf '%s\n' "$LEAKED")
       st=("${PIPESTATUS[@]}")                      # grep: 0 = found, 1 = not found, 2 = error
       case "${st[1]}" in
         (0) echo "blob $id"
             git log --all -m --no-ext-diff --no-textconv --format='  commit %h' --find-object="$id" \
               || echo "  COMMIT LOOKUP FAILED for blob $id" ;;
         (1) [ "${st[0]}" = 0 ] || echo "UNREADABLE blob $id (search incomplete)" ;;
         (*) echo "SCAN ERROR on blob $id (search incomplete)" ;;
       esac
     done
     st=("${PIPESTATUS[@]}")
     [ "${st[0]}" = 0 ] || echo 'LISTING OBJECTS FAILED (search incomplete)'
     [ "${st[1]}" = 0 ] || echo "SEARCH LOOP FAILED (status ${st[1]}, search incomplete)"
   )   # prints object and commit hashes only, never content; any "incomplete" line voids a clean result
   SEARCH
   ```
   `read -rs` takes the value without echoing it, so it reaches neither the shell history nor a session transcript. It stays in a subshell variable (never exported, so no child process's environment carries it) and reaches `grep` through a process substitution (`-f <(printf …)`, a pipe; `printf` is a shell builtin), never as a command argument: any local user can read a process's arguments with `ps`. Do not replace this with `git log -S"$LEAKED"`, which puts the value in Git's argument list. The search reads every blob, including binary ones such as a SQLite log, so it takes a while on a large repository; run it yourself as described above, because an agent must not type the value. These are **current-state** checks: `visibility` reports today's visibility and `branch -r` lists only the remote refs this clone knows now. Neither proves that the repository was never public or that the commit was never pushed and later deleted. When that history cannot be verified, treat the credential as compromised.

   Not pushed and private is recoverable. Pushed, or public at any point, means assume it is compromised regardless of how quickly it was removed.
3. **Remove it from the working tree** and parameterise, as above.
4. **Consider history rewrite** (`git filter-repo`, BFG) only with the user's explicit approval, and only for a repo whose history can safely be rewritten; and understand it invalidates every existing clone and does not retract anything already fetched, forked, or indexed. For anything public, treat rewrite as damage limitation, never as a substitute for revocation.
5. **Record it** — a Linear issue in the repository's project (per [`issue-tracking.md`](./issue-tracking.md); never `gh issue create`), so the rotation is auditable and the next person knows the old value is dead. Name the credential kind, never the value.

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

When inspecting a config file to check which servers or keys it defines, read the **structure**, not the values — parse and print field names, lengths, or masked prefixes. Session transcripts may be captured and persisted to a remote service, so dumping a config file with live tokens copies them somewhere new. When a value must be compared, compare it in a variable and print only the verdict.
