# Qodana baseline

`baseline.sarif.json` is the Qodana baseline for this repository: the set of problems that already
existed when the baseline was taken. The Qodana workflow
(`.github/workflows/qodana_code_quality.yml`) passes it to the scan with `--baseline`, and uses
`--fail-threshold 0`, so:

- a problem that is in the baseline is reported as *unchanged* and does not count;
- a problem that is not in the baseline is *new*, and a single new problem fails the check.

Without a baseline, every run reported the same standing problems as "new" and the check could only
ever be `neutral` (#55).

## The file is generated, never hand-edited

The baseline must come from a CI run. Problem fingerprints include paths relative to the analysis
root, which in CI is the Actions workspace holding `ploch-commandline`, `ploch-common` and
`mrploch-development` side by side. A local run from the repository root produces different
paths, so a locally generated baseline would make every problem look new.

## When to refresh it

Refresh the baseline in the same pull request as any of these:

- **You fix a problem that is in the baseline.** Otherwise the fixed problem stays in the file,
  and if it is ever reintroduced it is matched as *unchanged* and slips through unreported.
- **You upgrade the Qodana linter** (`linter:` in the workflow and in `qodana.yaml`). A new linter
  release can add inspections, and everything they find would otherwise fail the check as new.
- **You change the analysis layout**: the sibling checkouts, the analysis root, the solution path
  or the `exclude` paths. These change the paths inside fingerprints.

Do not refresh it to make a genuinely new problem go away. Fix the problem instead.

## How to refresh it

1. Push the change to a branch, then run the workflow on that branch:
   `gh workflow run qodana_code_quality.yml --ref <branch>`.
2. When the run finishes, download its report:
   `gh run download <run-id> -n qodana-report -D qodana-report`.
3. Check the report contains no credential before committing it; this repository is public.
   For example, `grep -cE "gh[p]_|github[_]pat_" qodana-report/qodana.sarif.json` must print `0`.
   (The brackets match the same text; they only keep the literal token prefixes out of this file,
   so secret scanners do not flag the instruction itself.)
4. Replace `.qodana/baseline.sarif.json` with `qodana-report/qodana.sarif.json`, unmodified.
5. Commit, push, and confirm the next run reports no new problems.
