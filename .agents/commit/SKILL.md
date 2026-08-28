---
name: commit
description: Create a git commit following the conventional commit standards. Use when the user asks to commit changes, or after completing a task that should be committed.
allowed-tools: Bash(git:*), Glob, Read
---

# Git Commit

Create a well-structured git commit following the repository's commit message conventions.

## Process

1. **Check for uncommitted changes**:

   ```bash
   git status
   git diff
   git diff --staged
   ```

2. **Detect commit style** — Check the project's conventions:

   ```bash
   # Check for commitlint config (indicates conventional commits)
   ls -la commitlint.config.* .commitlintrc* 2>/dev/null

   # Check recent commits to understand the style
   git log -10 --oneline
   ```

3. **Follow the detected style**:
   - If commitlint config exists → Use conventional commits
   - Otherwise → Match the style of recent commits

4. Stage appropriate files with `git add`

5. Create the commit matching the project's style

## Commit Styles

### Conventional Commits (if commitlint config exists)

- **Format:** `type(scope): TICKET-ID Description`
- **Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- **Ticket ID:** Extract from branch name or use `NO-TICKET`

### Freeform (if no commitlint)

- Match the style of recent commits in the repository
- Keep the title under 72 characters
- Use imperative mood ("Add feature" not "Added feature")

## Important Rules

- **Never commit secrets** (.env, credentials, keys, etc.)
- **Always verify changes** before committing
- **Match the project's style** — check git log first
- **Use a HEREDOC** for multi-line messages:

```bash
git commit -m "$(cat <<'EOF'
Your commit message here
EOF
)"
```

## Examples

**Conventional commits:**

- `feat(auth): NT-1234 Add OAuth2 login flow`
- `fix(api): INC-567 Handle null response from payment gateway`

**Freeform:**

- `Add OAuth2 login flow`
- `Fix null response handling in payment gateway`
