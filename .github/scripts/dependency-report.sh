#!/usr/bin/env bash
#
# Reports outdated and vulnerable NuGet packages for the solution passed as $1, writing a
# deduplicated summary to stdout and, when running under Actions, to $GITHUB_STEP_SUMMARY
# as well. Both, deliberately: the summary is what a person reads, but only the log is
# retrievable through the API afterwards, so a report living solely in the summary could
# never be checked after the fact.
#
# Exits non-zero only when a vulnerable package is found. An outdated package is
# information; see docs/dependency-updates.md for why that distinction is deliberate.
#
# Requires the workspace layout: ../mrploch-development must exist beside the repository
# root, because Directory.Packages.props imports the shared version files from it.
set -euo pipefail

solution="${1:?usage: dependency-report.sh <solution-path>}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
report="$work/report.md"

# Every project in the solution reports the same centrally managed package, so the raw
# output repeats each id once per project per framework. The reports below collapse that
# to one row per distinct package, which is the unit of work someone acts on.
#
# `.frameworks` is absent entirely on a project with nothing to report, hence the `// []`
# guards rather than a plain index.
flatten='[ .projects[]? | (.frameworks // [])[]
           | ((.topLevelPackages // []) + (.transitivePackages // []))[] ]'

# Says where the reader has to go to change the version. A package pinned in this
# repository's own Directory.Packages.props is ours to bump; one that comes from a shared
# file has to be bumped in mrploch-development, for every repository at once; a transitive
# package is pinned nowhere and moves only when its parent does.
#
# Both item types are matched: shared analysers are declared as GlobalPackageReference
# (which carries its own Version) rather than PackageVersion, so matching only the latter
# mislabelled every analyser as transitive.
package_owner() {
  # Package ids are full of dots, which are regex wildcards, so escape them.
  local id="${1//./\\.}"
  local pattern="(PackageVersion|GlobalPackageReference) Include=\"$id\""
  if grep -qE "$pattern" Directory.Packages.props; then
    echo 'this repository'
  elif grep -rqE "$pattern" ../mrploch-development/dependencies/; then
    echo 'mrploch-development'
  else
    echo 'transitive — bump the package that brings it in'
  fi
}

{
  echo '# NuGet dependency report'
  echo
} >>"$report"

# --- Outdated ------------------------------------------------------------------------
dotnet list "$solution" package --outdated --no-restore \
  --format json --output-version 1 >"$work/outdated.json"

jq -r "$flatten
       | unique_by(.id)
       | .[]
       | [.id, .resolvedVersion, .latestVersion] | @tsv" \
  "$work/outdated.json" >"$work/outdated.tsv"

{
  echo '## Outdated packages'
  echo
  if [[ ! -s "$work/outdated.tsv" ]]; then
    echo 'None — every package is at its latest available version.'
  else
    echo '| Package | Resolved | Latest | Bump in |'
    echo '|---|---|---|---|'
    while IFS=$'\t' read -r id resolved latest; do
      echo "| \`$id\` | $resolved | $latest | $(package_owner "$id") |"
    done <"$work/outdated.tsv"
  fi
  echo
} >>"$report"

# --- Vulnerable ----------------------------------------------------------------------
# Transitive packages are included deliberately: a vulnerability reached through a
# dependency of a dependency is just as exploitable, and Dependabot's version updates
# would not have surfaced it either.
dotnet list "$solution" package --vulnerable --include-transitive --no-restore \
  --format json --output-version 1 >"$work/vulnerable.json"

jq -r "$flatten
       | map(. as \$package
             | (\$package.vulnerabilities // [])
             | map([\$package.id, \$package.resolvedVersion, .severity, .advisoryurl]))
       | flatten(1)
       | unique
       | .[] | @tsv" \
  "$work/vulnerable.json" >"$work/vulnerable.tsv"

{
  echo '## Vulnerable packages'
  echo
  if [[ ! -s "$work/vulnerable.tsv" ]]; then
    echo 'None.'
  else
    echo '| Package | Resolved | Severity | Advisory | Bump in |'
    echo '|---|---|---|---|---|'
    while IFS=$'\t' read -r id resolved severity advisory; do
      echo "| \`$id\` | $resolved | $severity | [advisory]($advisory) | $(package_owner "$id") |"
    done <"$work/vulnerable.tsv"
  fi
  echo
} >>"$report"

cat "$report"
if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  cat "$report" >>"$GITHUB_STEP_SUMMARY"
fi

if [[ -s "$work/vulnerable.tsv" ]]; then
  echo "::error::$(wc -l <"$work/vulnerable.tsv" | tr -d ' ') vulnerable NuGet package(s) found — see the report above."
  exit 1
fi
