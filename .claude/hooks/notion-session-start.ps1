# SessionStart hook — Personal Dev Daily Notes auto-log
#
# Triggered by Claude Code at session start / resume / clear / compact when cwd
# is under the MrPloch workspace. Reads the session payload from stdin, writes
# (or refreshes) C:\DevNet\my\mrploch\.claude\.notion-current-task.json, and
# injects a system reminder telling the assistant to create a Notion entry as
# soon as the first substantive task is clear.
#
# Outputs JSON on stdout in the hookSpecificOutput format so Claude Code injects
# `additionalContext` into the session.

$ErrorActionPreference = 'SilentlyContinue'

# --- Read hook payload from stdin ---
$rawInput = [Console]::In.ReadToEnd()
$payload  = $null
if ($rawInput) {
    try { $payload = $rawInput | ConvertFrom-Json } catch { $payload = $null }
}

$sessionId = if ($payload -and $payload.session_id) { $payload.session_id } else { 'unknown' }
$source    = if ($payload -and $payload.source)     { $payload.source }     else { 'startup' }
$cwd       = if ($payload -and $payload.cwd)        { $payload.cwd }        else { (Get-Location).Path }

# --- Scope guard: only run when cwd is under the workspace root ---
$workspaceRoot = 'C:\DevNet\my\mrploch'
if (-not $cwd.StartsWith($workspaceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    exit 0
}

# --- Paths ---
$stateDir  = Join-Path $workspaceRoot '.claude'
$stateFile = Join-Path $stateDir '.notion-current-task.json'

# --- Initialise / refresh state file ---
# On startup or clear: write a fresh state with empty current_entry.
# On resume / compact: preserve any existing current_entry so we keep updating
# the same Notion page rather than creating a duplicate.
$shouldReset = ($source -eq 'startup') -or ($source -eq 'clear')

$existing = $null
if ((Test-Path $stateFile) -and -not $shouldReset) {
    try { $existing = Get-Content -Raw -Path $stateFile -Encoding UTF8 | ConvertFrom-Json } catch { $existing = $null }
}

$state = [ordered]@{
    conversation_id    = $sessionId
    session_started_at = (Get-Date).ToUniversalTime().ToString('o')
    source             = $source
    cwd                = $cwd
    current_entry      = if ($existing -and $existing.current_entry) { $existing.current_entry } else { $null }
}

$json = $state | ConvertTo-Json -Depth 6
Set-Content -Path $stateFile -Value $json -Encoding UTF8

# --- Build the system reminder ---
$reminder = @"
[NOTION_AUTO_LOG] You are in the MrPloch workspace (cwd: $cwd).

This session's Claude Code conversation id is: $sessionId
State file: $stateFile (current_entry tracks the active Notion page for this task; do not edit by hand — read it, then update via the Notion MCP and rewrite the file)

ACTION REQUIRED — log substantive tasks to the 'Personal Dev Daily Notes' Notion DB BEFORE doing the work:

1. Wait for the user's first SUBSTANTIVE task message. Substantive = anything that has you read files, run commands, write code/docs, or otherwise change state. Skip pure conceptual Q&A, greetings, or meta-questions about Claude/the workspace. If the first message is non-substantive, wait for the next one.

2. When a substantive task IS clear, BEFORE doing the work:
   - Read .claude/.notion-current-task.json to confirm no active entry already covers this topic.
   - Create a new page in the Personal Dev Daily Notes DB (id: 341a5394d58b80839ebbf6dade6eb252) with:
       Title          = short imperative summary of the task (<=80 chars)
       Summary        = 2-4 sentence neutral summary of the goal, scope, and current understanding (no chat content)
       Conversation Id= $sessionId
       Working Folder = $cwd  (MANDATORY - the folder Claude Code is operating in; the single path column)
       Status         = 'In progress'
       Categories     = relevant relation page IDs (see workspace CLAUDE.md > 'Personal Dev Daily Notes Auto-Log')
       Tags           = relevant relation page IDs (same section)
       Projects       = relevant relation page IDs (MANDATORY - always set at least one; fallback: 'mrploch (General)'; the Repository Name rollup derives from this relation)
       Tasks          = relation to the Personal Dev Tasks DB (MANDATORY - find-or-create the task this session advances; set the task's 'GitHub Issue' URL when a repo issue/PR exists; keep task Status in sync)
       Page body      = sections per the 'Page body structure' in the rule, INCLUDING a '## Work Log' section
   - Update .claude/.notion-current-task.json with the new page_id, title, topic_slug, and created_at. Keep conversation_id unchanged.
   - THEN proceed to the actual work.

3. Mid-session topic switches: when the user clearly pivots to a new, unrelated task, repeat step 2 (create a new page, update state file, KEEP the same conversation_id).

4. Progress updates: at meaningful checkpoints (PR created, CI green, decision made, branch switch), update the current Notion page's body via notion-update-page, APPENDING each step to the '## Work Log' section (append-only: commands run, files changed, PR numbers, commit hashes, findings). Keep the page body in sync with the work — do not let it go stale.

5. SESSION END (HARD trigger — never skip): before writing the final summary of any substantive session, update the Notion page to its final state — Work Log complete and backfilled, Outstanding/next steps accurate (including the exact next action for a fresh session), Outcome filled if done, Status set ('In progress' or 'Done'). If no entry was created earlier, create it NOW with a full retrospective work log. The page body must let a brand-new session continue the work without this conversation.

The full schema, field IDs, page-body template, and recommended Categories/Tags/Projects relation IDs live in C:\DevNet\my\mrploch\CLAUDE.md under 'Personal Dev Daily Notes Auto-Log'. Read that section before creating the first entry.

This rule REPLACES the global 'AI Saved Information' Notion rule for everything under $workspaceRoot.
"@

# --- Emit hookSpecificOutput JSON so Claude Code injects the reminder ---
$out = @{
    hookSpecificOutput = @{
        hookEventName     = 'SessionStart'
        additionalContext = $reminder
    }
} | ConvertTo-Json -Depth 4

Write-Output $out
exit 0
