---
name: pr
description: Create or update a pull request from the current branch. Commits uncommitted changes, pushes, ensures the PR body follows the repository's template, and assigns the PR to the creator.
allowed-tools: Bash(git:*), Bash(gh:*), Read, Glob, Grep
---

# Pull Request

Create or update a pull request from the current branch.

## Pre-flight Checks

1. **Verify branch** - Ensure you're on a feature branch, not `main`, `master`, or `develop`:

   ```bash
   git branch --show-current
   ```

   If on a protected branch, stop and ask the user to switch branches.

2. **Check for uncommitted changes**:

   ```bash
   git status --porcelain
   ```

   If there are uncommitted changes, use the `/commit` skill flow to commit them first.

## Process

1. **Push the branch** (with upstream tracking if needed):

   ```bash
   git push -u origin HEAD
   ```

2. **Check for existing PR**:

   ```bash
   gh pr view --json number,url 2>/dev/null || echo "NO_PR"
   ```

3. **Read PR template** (if it exists):

   ```bash
   cat .github/pull_request_template.md 2>/dev/null || cat .github/PULL_REQUEST_TEMPLATE.md 2>/dev/null || echo "NO_TEMPLATE"
   ```

4. **Gather context**:

   ```bash
   # Get all commits on this branch vs base
   git log origin/main..HEAD --oneline 2>/dev/null || git log origin/master..HEAD --oneline

   # Get the full diff
   git diff origin/main..HEAD --stat 2>/dev/null || git diff origin/master..HEAD --stat
   ```

5. **Create or update PR**:
   - If NO existing PR: Create using `gh pr create`
   - If PR exists: Update using `gh pr edit`

## PR Body Requirements

- **Follow the template exactly** - Remove sections that don't apply, fill in all required sections
- **Include ticket link** - Extract from branch name or commits (e.g., `NT-1234`, `INC-567`)
- **Summarise all commits** - The PR description should cover ALL changes, not just the latest commit
- **Developer testing notes** - Include what was tested and how

## PR Title

Match the project's commit style (check for commitlint config or recent commits):

- **Conventional:** `type(scope): TICKET-ID Description`
- **Freeform:** `Description of changes`

## Creating a PR

```bash
gh pr create --title "Your title here" --body "$(cat <<'EOF'
[Body following PR template]
EOF
)" && gh pr edit --add-assignee @me
```

The `&&` ensures the assignment only runs if PR creation succeeds.

## Updating a PR

```bash
gh pr edit --title "Updated title" --body "$(cat <<'EOF'
[Updated body following template]
EOF
)"
```

## Important Rules

- **Never create PRs from protected branches** (main, master, develop)
- **Always follow the PR template** if one exists
- **Include ALL changes** from the branch, not just recent commits
- **Commit before creating PR** - Don't create PRs with uncommitted changes
