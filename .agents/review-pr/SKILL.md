---
name: review-pr
description: Review a pull request for code quality, potential issues, and adherence to project standards. Use when the user asks to review a PR or when reviewing changes before merging.
allowed-tools: Bash(git:*), Bash(gh:*)
---

# Pull Request Review

Review a pull request thoroughly for code quality, potential issues, and adherence to project standards.

## Process

1. **Fetch PR information** using GitHub CLI:

   ```bash
   gh pr view <PR_NUMBER> --json title,body,files,additions,deletions,commits
   ```

2. **Review the changed files**:

   ```bash
   gh pr diff <PR_NUMBER>
   ```

3. **Check the commit history**:

   ```bash
   gh pr view <PR_NUMBER> --json commits
   ```

4. **Review PR comments and status**:
   ```bash
   gh pr view <PR_NUMBER> --json reviews,comments,statusCheckRollup
   ```

## Review Checklist

### Code Quality

- [ ] Code is readable and follows project conventions
- [ ] No unnecessary complexity or over-engineering
- [ ] Functions have single responsibility
- [ ] Naming is clear and descriptive

### Security

- [ ] No hardcoded secrets or credentials
- [ ] Input validation on user data
- [ ] No SQL injection or XSS vulnerabilities
- [ ] Sensitive data is handled appropriately

### Testing

- [ ] Tests are included for new functionality
- [ ] Tests cover edge cases
- [ ] Existing tests still pass

### Documentation

- [ ] Code changes are documented where necessary
- [ ] API changes are documented
- [ ] README updated if needed

### Breaking Changes

- [ ] No unintended breaking changes
- [ ] Breaking changes are documented and justified

## Output Format

Provide a structured review with:

1. **Summary** - Brief overview of the changes
2. **Strengths** - What's done well
3. **Concerns** - Issues that should be addressed
4. **Suggestions** - Optional improvements
5. **Verdict** - Approve, Request Changes, or Comment

## Adding Review Comments

To add a review comment:

```bash
gh pr review <PR_NUMBER> --comment --body "Your review here"
```

To approve:

```bash
gh pr review <PR_NUMBER> --approve --body "LGTM"
```

To request changes:

```bash
gh pr review <PR_NUMBER> --request-changes --body "Please address the following..."
```
