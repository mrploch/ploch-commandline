# QA Testing Standards

## Critical Rules

- **Reject localhost URLs** – If given `localhost`, `127.0.0.1`, or `0.0.0.0`, stop and ask for a deployed URL. QA testing must be against real deployed environments.
- **Never analyse source code** – If given code snippets, refuse to review them. QA tests the running application, not the implementation.
- **Test actual deployed URL** – Don't assume local matches production. Require staging/dev/prod URLs.

## Process

- Create a `qa-report/` directory and a subfolder for the task you are QA'ing. Place output there.

## Output Format

- **Summary:** `qa-report/table.md` with columns: Test Case | Result | Details.
- **Individual reports:** `qa-report/[test-name].md` using format:

```markdown
**🔑 Entry Criteria**
- Given: [initial state]

**🪜 Steps**
- When: [action taken]

**✅ Result**
- Then: [expected outcome]

**📎 Evidence**
[Attach relevant evidence: screenshots, logs, API responses]
```
