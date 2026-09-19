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

The baseline must come from a CI run. The scan runs inside the Qodana container against the CI
workspace, which holds `ploch-commandline` and `mrploch-development` side by side,
with packages restored the way CI restores them. Result locations in the report are relative to
the solution directory inside that container. A local run differs in both respects, so a locally
generated baseline is not guaranteed to match, and every problem could look new.

## When to refresh it

Refresh the baseline in the same pull request as any of these:

- **You fix a problem that is in the baseline.** Otherwise the fixed problem stays in the file,
  and if it is ever reintroduced it is matched as *unchanged* and slips through unreported.
- **The Qodana linter changes**: you change `linter:` in the workflow and in `qodana.yaml`, or a
  new patch release is published under the pinned `2026.2` tag. A linter release can add
  inspections, and everything they find would otherwise fail the check as new.
- **You change the analysis layout**: the sibling checkouts, the analysis root, the solution path
  or the `exclude` paths. These change which files are analysed and where their results point.

Do not refresh it to make a genuinely new problem go away. Fix the problem instead.

## How to refresh it

1. Push the change to a branch and let the Qodana workflow run, either on the pull request or
   with `gh workflow run qodana_code_quality.yml --ref <branch>`.
2. When the run finishes, check it first. **If it reported new problems you did not intend to
   baseline, stop, fix them and push again.** Refreshing from that run would bake those problems into
   the baseline, and the confirming run in step 5 would then report `NEW: 0` for them. The only new
   problems a refresh may absorb are the ones its trigger explains, such as findings from a linter
   upgrade.

   Then download the report and extract the SARIF. The `qodana-report` artifact contains a single
   zip of the results directory, with `qodana.sarif.json` at its root:

   ```bash
   gh run download <run-id> -n qodana-report -D qodana-report
   python -c "import zipfile; z=zipfile.ZipFile('qodana-report/qodana-report.zip'); z.extract(min((n for n in z.namelist() if n.endswith('qodana.sarif.json')), key=len), 'qodana-report')"
   ```

   The command extracts the top-level `qodana.sarif.json`, the shortest matching entry, whether or
   not the zip stores it with a leading `/`. The zip also holds a byte-identical copy under
   `report/results/`. `unzip` would extract everything, but on this zip it prints a
   "stripped absolute path spec" warning per entry and exits with status 1, which stops a script
   running under `set -e`.
3. Check the report contains no credential before committing it; this repository is public.
   For example, `grep -cE "gh[p]_|github[_]pat_" qodana-report/qodana.sarif.json` must print `0`.
   (The brackets match the same text; they only keep the literal token prefixes out of this file,
   so secret scanners do not flag the instruction itself.) A broader scanner such as `gitleaks` is
   a reasonable addition.
4. Replace `.qodana/baseline.sarif.json` with `qodana-report/qodana.sarif.json`, unmodified.
5. Commit and push, then confirm the next run's log reports
   `Grouping problems according to baseline: UNCHANGED: <n>, NEW: 0` and the
   `Qodana Community for .NET` check succeeds. This step is required: once the baseline is in use,
   the report from step 2 carries a `baselineState` on every problem, and the confirming run is
   what proves the refreshed file still matches.
