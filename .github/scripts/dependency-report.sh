#!/usr/bin/env bash
#
# Reports outdated and vulnerable NuGet packages for every solution passed as an argument,
# writing a deduplicated summary to stdout and, when running under Actions, to
# $GITHUB_STEP_SUMMARY as well. Both, deliberately: the summary is what a person reads,
# but only the log is retrievable through the API afterwards, so a report living solely in
# the summary could never be checked after the fact.
#
# Exits non-zero only when a vulnerable package is found. An outdated package is
# information; see docs/dependency-updates.md for why that distinction is deliberate.
#
# Requires the workspace layout: ../mrploch-development must exist beside the repository
# root, because Directory.Packages.props imports the shared version files from it.
set -euo pipefail

if [[ $# -eq 0 ]]; then
  echo 'usage: dependency-report.sh <solution-path> [<solution-path> ...]' >&2
  exit 2
fi

shared_versions='../mrploch-development/dependencies'
if [[ ! -d "$shared_versions" ]]; then
  # Without the sibling, restore could not have succeeded and every package would be
  # mislabelled as transitive. Fail loudly rather than emit a quietly wrong report.
  echo "::error::$shared_versions not found - clone mrploch-development beside this repository." >&2
  exit 2
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
report="$work/report.md"
vulnerable_total=0

# Every project in a solution reports the same centrally managed package, so the raw
# output repeats each id once per project per framework. The reports below collapse that
# to one row per distinct package, which is the unit of work someone acts on.
#
# `.frameworks` is absent entirely on a project with nothing to report, hence the `// []`
# guards rather than a plain index.
flatten='[ .projects[]? | (.frameworks // [])[]
           | ((.topLevelPackages // []) + (.transitivePackages // []))[] ]'

# The Directory.Packages.props that governs the solution currently being reported. Set by
# report_solution, because it is NOT always the one at the repository root: the sample
# application under samples/ deliberately keeps its own standalone set of pins, and
# searching every props file in the repository at once made the sample's copies of
# Serilog, Spectre.Console and xunit shadow the shared attribution for the main solution.
local_version_file=''

# Finds the Directory.Packages.props MSBuild itself would apply to a solution: the nearest
# one at or above the solution's own directory.
find_version_file() {
  local directory
  directory="$(cd "$(dirname "$1")" && pwd)"
  while [[ "$directory" != '/' && -n "$directory" ]]; do
    if [[ -f "$directory/Directory.Packages.props" ]]; then
      echo "$directory/Directory.Packages.props"
      return 0
    fi
    directory="$(dirname "$directory")"
  done
  echo "::error::no Directory.Packages.props found at or above $1." >&2
  return 1
}

# Says where the reader has to go to change a version.
#
# Matching is deliberately lenient about XML shape - either quote style, any run of
# whitespace, and a case-insensitive id, because NuGet package ids and MSBuild item names
# are both case-insensitive. Both item types are matched too: the shared analysers are
# declared as GlobalPackageReference (which carries its own Version) rather than
# PackageVersion, and matching only the latter mislabelled every analyser as transitive.
package_owner() {
  # Package ids are full of dots, which are regex wildcards, so escape them.
  local id="${1//./\\.}"
  local pattern="(PackageVersion|GlobalPackageReference)[[:space:]]+Include[[:space:]]*=[[:space:]]*[\"']${id}[\"']"
  local declaration property

  declaration="$(grep -hiE "$pattern" "$local_version_file" 2>/dev/null | head -n 1 || true)"
  if [[ -n "$declaration" ]]; then
    # A declaration whose Version is an MSBuild property reference is only half local: the
    # id is pinned here, but the number it resolves to lives in a PropertyGroup that can
    # be anywhere in the import graph, including a shared file, and can itself be another
    # property reference (Ploch.Common.Apps.Shared is pinned here as
    # $(PlochAppsSharedVersion), which defaults to the shared $(PlochCommonPackagesVersion)).
    # Chasing that chain in shell would be fragile, so the property is named instead - the
    # actionable pointer - rather than claiming a location that may be wrong.
    #
    # A `case` pattern rather than a quoted comparison: the marker contains `$(`, and in
    # single quotes ShellCheck reads that as an expansion someone forgot to enable
    # (SC2016). Here the backslashes make it unambiguously literal. Both quote styles are
    # matched because MSBuild accepts either.
    case "$declaration" in
      *Version=\"\$\(* | *Version=\'\$\(*)
        property="$(sed -nE 's/.*Version[[:space:]]*=[[:space:]]*["'"'"']\$\(([^)]+)\).*/\1/p' <<<"$declaration")"
        echo "pinned here as \`\$($property)\` — change that property" ;;
      *)
        echo 'this repository' ;;
    esac
  elif grep -rhiqE "$pattern" "$shared_versions"; then
    echo 'mrploch-development'
  else
    echo 'transitive — bump the package that brings it in'
  fi
}

# Renders one solution's two tables into $report and returns the number of vulnerability
# rows it found.
report_solution() {
  local solution="$1"
  local outdated="$work/outdated.tsv" vulnerable="$work/vulnerable.tsv"
  local id resolved latest severity advisory

  local_version_file="$(find_version_file "$solution")"

  echo "## \`$solution\`" >>"$report"
  echo >>"$report"

  # --- Outdated -----------------------------------------------------------------------
  # Direct packages only, deliberately. `--outdated --include-transitive` aborts the SDK
  # outright on the sample solution - `error: Sequence contains no matching element`,
  # reproducible on .NET 10 while either flag alone succeeds - so adding it would trade a
  # low-value table (a transitive package cannot be bumped directly; the advice would only
  # ever be "bump its parent") for a report that cannot run at all. The vulnerability
  # table below, where transitive coverage genuinely matters, does include them.
  dotnet list "$solution" package --outdated --no-restore \
    --format json --output-version 1 >"$work/outdated.json"

  jq -r "$flatten
         | unique_by(.id)
         | .[]
         | [.id, .resolvedVersion, .latestVersion] | @tsv" \
    "$work/outdated.json" >"$outdated"

  {
    echo '### Outdated packages'
    echo
    if [[ ! -s "$outdated" ]]; then
      echo 'None — every package is at its latest available version.'
    else
      echo '| Package | Resolved | Latest | Bump in |'
      echo '|---|---|---|---|'
      while IFS=$'\t' read -r id resolved latest; do
        echo "| \`$id\` | $resolved | $latest | $(package_owner "$id") |"
      done <"$outdated"
    fi
    echo
  } >>"$report"

  # --- Vulnerable ---------------------------------------------------------------------
  # Transitive packages are included for the same reason as above, and because a
  # vulnerability reached through a dependency of a dependency is just as exploitable.
  # The JSON field really is all-lowercase `advisoryurl` in output version 1 - verified
  # against a deliberately vulnerable probe project, not assumed.
  dotnet list "$solution" package --vulnerable --include-transitive --no-restore \
    --format json --output-version 1 >"$work/vulnerable.json"

  jq -r "$flatten
         | map(. as \$package
               | (\$package.vulnerabilities // [])
               | map([\$package.id, \$package.resolvedVersion, .severity, .advisoryurl]))
         | flatten(1)
         | unique
         | .[] | @tsv" \
    "$work/vulnerable.json" >"$vulnerable"

  {
    echo '### Vulnerable packages'
    echo
    if [[ ! -s "$vulnerable" ]]; then
      echo 'None.'
    else
      echo '| Package | Resolved | Severity | Advisory | Bump in |'
      echo '|---|---|---|---|---|'
      while IFS=$'\t' read -r id resolved severity advisory; do
        echo "| \`$id\` | $resolved | $severity | [advisory]($advisory) | $(package_owner "$id") |"
      done <"$vulnerable"
    fi
    echo
  } >>"$report"

  vulnerable_total=$(( vulnerable_total + $(wc -l <"$vulnerable") ))
}

{
  echo '# NuGet dependency report'
  echo
} >>"$report"

for solution in "$@"; do
  report_solution "$solution"
done

cat "$report"
if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  cat "$report" >>"$GITHUB_STEP_SUMMARY"
fi

if (( vulnerable_total > 0 )); then
  echo "::error::$vulnerable_total vulnerable NuGet package finding(s) — see the report above."
  exit 1
fi
