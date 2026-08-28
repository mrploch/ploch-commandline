---
name: qa-explore
description: Perform exploratory QA testing and create a test report in Notion. Supports frontend (browser), backend (API), and library/package testing. Use when the user asks to QA test a deployment, or to QA test changes in a library repo.
allowed-tools: Bash(*), Read, Glob, Grep, WebFetch, mcp__notion__*, mcp__chrome-devtools__*
---

# QA Exploratory Testing

Perform exploratory QA testing and create a test report in Notion.

**Usage:**

- `/qa-explore <deployment-url> [ticket-url]` — Frontend or Backend testing
- `/qa-explore [ticket-url]` — Library testing (tests current repo)

Ticket URL is always optional. If not provided, the skill will:

1. Check the current PR for linked tickets
2. Search recent commit messages for ticket references (e.g., `NT-1234`, `INC-567`)
3. Proceed without a ticket if none found

## Input Validation

### Deployment URL Validation (FE/BE only)

The deployment URL **must** be a deployed environment (not localhost):

- **Valid FE:** `https://dev.equalsmoney.com`, `https://staging.equalsmoney.com`, `https://app.equalsmoney.com`
- **Valid BE:** `https://api-dev.equalsmoney.com`, `https://api.equalsmoney.com`, any deployed API endpoint
- **Invalid:** `http://localhost:3000`, `http://127.0.0.1:8080`, `http://0.0.0.0:5000`

If the URL contains `localhost`, `127.0.0.1`, or `0.0.0.0`, **stop and ask the user** for a deployed URL.

### Ticket Resolution (always optional)

If a ticket URL is provided, validate it's a Notion page URL:

- `https://www.notion.so/equalsgroup/NT-1234-Task-Title-abc123def456`
- `https://notion.so/abc123def456`

**If no ticket URL provided, auto-detect:**

1. **Check current PR** for linked tickets:

   ```bash
   gh pr view --json body,title | jq -r '.body, .title'
   ```

   Look for Notion URLs or ticket IDs (e.g., `NT-1234`, `INC-567`)

2. **Search recent commits** for ticket references:

   ```bash
   git log -20 --oneline | grep -oE '[A-Z]+-[0-9]+'
   ```

3. **If found**, fetch the ticket from Notion using search:

   ```
   mcp__notion__notion-search
   - query: "NT-1234"
   - query_type: "internal"
   ```

4. **If none found**, proceed without a ticket — link to PR/branch instead

## Determine Testing Type

Detect the testing approach based on inputs and context:

### Frontend Testing

Use when:

- Deployment URL is a web app (PWA, web portal)
- Ticket is tagged with "Front-end" domain
- Repository is `equals-money-apps` or similar FE repo

Testing approach: Browser-based testing with Chrome DevTools MCP

### Backend Testing

Use when:

- Deployment URL is an API endpoint
- Ticket is tagged with a backend domain
- Repository is a service/API repo (e.g., `em-transactions-api`, `banking-spectrum`)

Testing approach: API testing with curl/httpie, response validation, integration checks

### Library Testing

Use when:

- **No deployment URL provided**
- Repository is a package/library (e.g., `ai-rules`, shared libraries)
- `package.json` has no web app indicators (no `start` script pointing to a server)
- Contains `.csproj`/`.nuspec` (NuGet package) or is published to npm

Testing approach: Run test suites, verify CLI commands, test installation, validate outputs

**Library detection signals:**

- `package.json` with `bin` field (CLI tool)
- `package.json` with `main`/`exports` but no `start` script
- `.csproj` with `<PackAsTool>` or `<IsPackable>`
- Presence of `dotnet/` directory with tool/package structure

## Process Overview

1. **Validate inputs** and determine testing type
2. **Fetch ticket details** from Notion (if ticket provided)
3. **Create test report** in Notion (In Progress status)
4. **Plan test cases** based on ticket acceptance criteria or repo analysis
5. **Execute tests** using appropriate tools
6. **Create test cases** in Notion with results
7. **Update test report** with final status

## Step 1: Fetch Ticket Details (if provided)

If a ticket URL was provided:

```
mcp__notion__notion-fetch
- id: <ticket-page-id>
```

Extract from the ticket:

- **Title** - What feature/fix to test
- **Description** - Detailed requirements
- **Acceptance criteria** - What defines success
- **Domain** - Determines FE vs BE testing approach
- **Repository** - Confirms testing type
- **Linked specs/PRs** - Related documentation

**If no ticket provided (library testing):**

Analyse the current repository to understand what to test:

1. Read `README.md` for project overview
2. Read `package.json` or `.csproj` for available scripts/commands
3. Check recent commits/PR for what changed: `git log -10 --oneline`
4. Identify test commands, build commands, and CLI entry points

## Step 2: Create Test Report in Notion

Create a test report in the Test Reports database:

```
mcp__notion__notion-create-pages
- parent: { "data_source_id": "0a15a9dc-e66e-40bd-8605-55b9bd0cb819" }
- pages: [{
    "properties": {
      "Test Report Description": "<Ticket ID or Repo Name> - <Brief description>",
      "Overall Pass/Fail": "In Progress",
      "Task(s) Covered": "[<ticket-url>]",
      "Test Environment": "[\"Dev\"]",
      "date:Date Started:start": "<today's date YYYY-MM-DD>",
      "date:Date Started:is_datetime": 0
    },
    "content": "### Test Scope\n<description of what's being tested>\n\n### Testing Type\n<Frontend / Backend / Library>\n\n### Notes\nAutomated QA testing in progress..."
  }]
```

**Test Environment options:** `Dev`, `staging`, `Prod`

**Overall Pass/Fail options:** `Draft`, `In Progress`, `Pass`, `Fail`, `Blocked`

**For library testing without a ticket:**

- Use repo name and branch/PR in the description: `ai-rules (PR #12) - QA skill testing`
- Omit `Task(s) Covered` or link to the PR URL if available
- Add PR/branch reference in the content

## Step 3: Plan Test Cases

Based on the ticket's acceptance criteria, plan test cases covering:

- **Happy path** - Core functionality works as expected
- **Edge cases** - Boundary conditions, empty states, limits
- **Error handling** - Invalid inputs, error responses
- **Security** - Authentication, authorisation checks
- **Regression** - Existing functionality not broken
- **Integration** - Upstream/downstream system impacts (especially for BE)

## Step 4: Execute Tests

### Frontend Testing (Browser-based)

Use Chrome DevTools MCP tools:

```
# Navigate to the deployment
mcp__chrome-devtools__navigate_page
- url: <deployment-url>

# Take snapshots to understand the page
mcp__chrome-devtools__take_snapshot

# Interact with elements
mcp__chrome-devtools__click
mcp__chrome-devtools__fill

# Check for console errors
mcp__chrome-devtools__list_console_messages

# Check network requests
mcp__chrome-devtools__list_network_requests

# Take screenshots as evidence
mcp__chrome-devtools__take_screenshot
- filePath: /tmp/qa-evidence/<test-name>.png
```

### Backend Testing (API-based)

Use curl for API testing:

```bash
# GET request
curl -s -X GET "<api-url>/endpoint" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" | jq .

# POST request
curl -s -X POST "<api-url>/endpoint" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"key": "value"}' | jq .

# Check response status
curl -s -o /dev/null -w "%{http_code}" "<api-url>/endpoint"

# Save response for evidence
curl -s -X GET "<api-url>/endpoint" \
  -H "Authorization: Bearer <token>" > /tmp/qa-evidence/<test-name>.json
```

**Backend test considerations:**

- Validate response schema matches expected contract
- Check error responses (400, 401, 403, 404, 500)
- Test with valid and invalid authentication
- Verify data integrity (check database state if accessible)
- Test integration with upstream/downstream services
- Check for proper error messages and codes

### Library Testing (Package/CLI)

**Note:** Automated tests (unit, lint, types) are already visible in the PR. Library QA focuses on **exploratory verification** that automated tests can't cover.

**What to test:**

1. **Real-world installation** — Does the package install correctly in a fresh project?
2. **CLI behaviour** — Do commands work as documented with real inputs?
3. **Output verification** — Are generated files correct and complete?
4. **Edge cases** — Unusual configs, missing files, error scenarios
5. **Documentation accuracy** — Does README match actual behaviour?

```bash
# Test installation in a fresh project
cd /tmp && mkdir qa-test && cd qa-test
pnpm init -y
pnpm add <package-path>   # or: pnpm link <package-path>

# Test CLI commands with real inputs
pnpm exec <cli-name> --help
pnpm exec <cli-name> <command> [args]

# Verify output files are correct
ls -la <output-directory>
cat <output-file> | head -50

# Test edge cases
# - Missing config file
# - Invalid config options
# - Different presets/options
```

**Library QA considerations:**

- Package installs without errors in a consumer project
- CLI commands produce expected output
- Generated files are correct and complete
- Error messages are helpful for invalid inputs
- Documentation matches actual behaviour

**For `ai-rules` specifically:**

```bash
# Test in a fresh directory (simulates consumer project)
cd /tmp && mkdir qa-ai-rules && cd qa-ai-rules
echo '{"preset": "frontend"}' > ai-rules.config.json

# Test the install script
INIT_CWD=$PWD node /path/to/ai-rules/scripts/install-rules.js

# Verify output files
ls -la .cursor/rules/      # Check rule files generated
cat CLAUDE.md | head -50   # Check CLAUDE.md content
cat AGENTS.md | head -50   # Check AGENTS.md content

# Test different presets
echo '{"preset": "dotnet-service"}' > ai-rules.config.json
INIT_CWD=$PWD node /path/to/ai-rules/scripts/install-rules.js
ls -la .cursor/rules/      # Should have different rules

# Test error handling
echo '{"preset": "invalid"}' > ai-rules.config.json
INIT_CWD=$PWD node /path/to/ai-rules/scripts/install-rules.js  # Should error gracefully
```

## Step 5: Create Test Cases in Notion

For each test executed, create a test case. **Important:** Note the test case page URL from the response — you'll need it to link back to the test report.

```
mcp__notion__notion-create-pages
- parent: { "data_source_id": "40510dca-df16-4221-90e4-f3a320d1c7bb" }
- pages: [{
    "properties": {
      "Test Cases": "<Test case title>",
      "Status": "Passed",
      "Test Environment": "[\"Dev\"]",
      "🧪 Test Reports": "[\"https://www.notion.so/<test-report-id>\"]"
    },
    "content": "### Test Steps\n1. Step one\n2. Step two\n3. Step three\n\n### Expected Results\n- Result one\n- Result two\n\n### Actual Results\n- What actually happened\n\n### Evidence\n<API responses, screenshots, or logs>"
  }]
```

**Status options:** `to do`, `Skipped`, `Blocked`, `Not required`, `Not started`, `needs review`, `in progress`, `Failed`, `complete`, `Passed`

### Test Case Format for Backend

````markdown
### Test Steps

1. Send POST request to `/api/v2/payments` with valid payload
2. Verify response status is 201
3. Verify response body contains `paymentId`
4. Verify payment appears in database/downstream system

### Expected Results

- Response status: 201 Created
- Response body: `{ "paymentId": "uuid", "status": "pending" }`
- Payment visible in MTS

### Actual Results

- Response status: 201
- Response body: `{ "paymentId": "abc-123", "status": "pending" }`
- Payment confirmed in MTS

### Evidence

Request:

```json
POST /api/v2/payments
{
  "amount": 100.00,
  "currency": "GBP",
  "recipientId": "..."
}
```

Response:

```json
{
  "paymentId": "abc-123",
  "status": "pending",
  "createdAt": "2026-01-22T10:00:00Z"
}
```
````

### Test Case Format for Frontend

```markdown
### Test Steps

1. Navigate to Payments page
2. Click "New Payment" button
3. Fill in recipient and amount
4. Click "Submit"
5. Verify success message appears

### Expected Results

- Payment form displays correctly
- Form validates required fields
- Success toast shows after submission
- Payment appears in payment list

### Actual Results

- Form displayed correctly
- Validation worked as expected
- Success message: "Payment submitted successfully"
- Payment visible in list

### Evidence

[Screenshot attached]
```

### Test Case Format for Library

````markdown
### Test Steps

1. Create fresh directory with config: `{"preset": "frontend"}`
2. Run install script from package
3. Verify `.cursor/rules/` contains expected rule files
4. Verify `CLAUDE.md` includes frontend-specific sections
5. Test with invalid preset to verify error handling

### Expected Results

- Install completes without errors
- 15+ rule files in `.cursor/rules/`
- `CLAUDE.md` generated with frontend-specific content
- Invalid preset shows helpful error message

### Actual Results

- Install completed successfully
- 18 rule files generated including `react.mdc`, `jest.mdc`
- `CLAUDE.md` contains React and Jest sections
- Invalid preset error: "Unknown preset 'invalid'"

### Evidence

Directory listing:

```
.cursor/rules/
├── agent.mdc
├── code-quality.mdc
├── jest.mdc
├── react.mdc
└── ... (18 files total)
```

Error handling test:

```
$ echo '{"preset": "invalid"}' > ai-rules.config.json
$ node scripts/install-rules.js
Error: Unknown preset 'invalid'. Available presets: core, frontend, node-service, dotnet-service
```
````

## Step 6: Update Test Report

After all tests are complete, update the test report to link the test cases:

```
mcp__notion__notion-update-page
- data: {
    "page_id": "<test-report-id>",
    "command": "update_properties",
    "properties": {
      "Overall Pass/Fail": "Pass",
      "Test Cases": "[\"https://www.notion.so/<test-case-id-1>\", \"https://www.notion.so/<test-case-id-2>\"]",
      "date:Date Completed:start": "<today's date YYYY-MM-DD>",
      "date:Date Completed:is_datetime": 0
    }
  }
```

**Note:** The `Test Cases` property is a relation — use an array of full Notion page URLs.

Optionally update the content with a summary (the report content can also be left blank if all info is in properties):

```
mcp__notion__notion-update-page
- data: {
    "page_id": "<test-report-id>",
    "command": "replace_content",
    "new_str": "### Test Scope\n<description>\n\n### Testing Type\n<Frontend / Backend / Library>\n\n### Test Summary\n- **Total tests:** X\n- **Passed:** Y\n- **Failed:** Z\n\n### Issues Found\n<list any bugs or issues>\n\n### Not Covered\n<areas not tested and why>"
  }
```

## Critical Rules

**For FE/BE testing:**

- **Never test localhost** - Only deployed environments
- **Never analyse source code** - Only test the deployed application/API

**For Library testing:**

- **Test the actual package** - Run real tests, not just read code
- **Verify outputs** - Check generated files, CLI output, build artifacts

**For all testing types:**

- **Create evidence** - Screenshots for FE, API responses for BE, command output for Library
- **Document everything** - Entry criteria, steps, results
- **Link back to ticket** - Test report must reference the task (or PR if no ticket)
- **Be thorough** - Cover happy paths, edge cases, and error scenarios

## Determining Pass/Fail

- **Pass** - All acceptance criteria met, no critical bugs found
- **Fail** - Any acceptance criterion not met, or critical bugs found
- **Blocked** - Cannot complete testing due to environment issues or dependencies

## After Testing

1. Ensure all test cases are linked to the test report
2. Update the ticket status if appropriate
3. Report summary to the user with:
   - Link to the test report in Notion
   - Pass/fail status
   - Key findings
   - Any bugs raised

## Workflow Context

This skill fits into the development lifecycle:

**Standard flow (with ticket):**

1. **Ticket** → Development → PR → Code Review
2. **QA Testing** (this skill) → Test Report created
3. **RAR** → links to Test Report → Release Approval
4. **Deploy** to production

**Urgent flow (no ticket, e.g., incident fix):**

1. Development → PR → Code Review
2. **QA Testing** (this skill) → Test Report linked to PR
3. **RAR** → links to Test Report → Release Approval
4. **Deploy** to production

The test report created by this skill will be linked to:

- The original ticket/task (via `Task(s) Covered`) — if available
- The PR — if no ticket
- Individual test cases (via `Test Cases` relation)
- Eventually to a RAR when `/rar` is used
