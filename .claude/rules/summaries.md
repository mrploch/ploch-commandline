# Summary Reports

When producing a summary, report, or analysis (e.g. pipeline status, build results, investigation findings, architecture overviews):

1. **Save to file** — Write the summary as a Markdown file under the repository's own `temp/`
   directory, with a descriptive, timestamped filename (e.g.
   `temp/2026-03-10-ploch-common-pipeline-status.md`). Resolve `temp/` relative to the repository
   root (`git rev-parse --show-toplevel`) rather than hard-coding an absolute path, so the rule
   works in any checkout on any machine. Add `temp/` to `.gitignore` if it is not already ignored.
2. **Open automatically** — After writing the file, open it with the operating system's default
   handler for the platform in use:

   | Platform | Command |
   |---|---|
   | Windows | `start "" "<file-path>"` |
   | macOS | `open "<file-path>"` |
   | Linux | `xdg-open "<file-path>"` |

   Opening the file is a convenience, not a requirement. If no handler is available — a headless
   session, a CI runner, a remote shell — skip this step rather than failing the task.
3. **Still display inline** — Continue showing a concise version of the summary in the
   conversation as normal.
