#!/usr/bin/env bash
#
# Reports outdated and vulnerable NuGet packages for every solution passed as an argument,
# writing a deduplicated summary to stdout and, when running under Actions, to
# $GITHUB_STEP_SUMMARY as well. Both, deliberately: the summary is what a person reads,
# but only the log is retrievable through the API afterwards, so a report living solely in
# the summary could never be checked after the fact.
#
# Exits non-zero when a vulnerable package is found, or when a solution could not be
# checked at all. An outdated package is information; see docs/dependency-updates.md for
# why that distinction is deliberate.
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
  # Without the sibling, restore could not have succeeded and MSBuild could not evaluate the
  # version file. Fail loudly rather than emit a quietly wrong report.
  echo "::error::$shared_versions not found - clone mrploch-development beside this repository." >&2
  exit 2
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
report="$work/report.md"
vulnerable_total=0
failed_checks=0

# Whether the solution being reported pins transitive packages centrally. Set by
# report_solution from MSBuild's evaluation, read by package_owner.
transitive_pinning=''

# The Directory.Packages.props governing the solution being reported. Set by
# report_solution; version_property re-evaluates it to test a property.
governing_file=''

# The version elements of the governing file's whole import graph, one per line; written by
# report_solution from `dotnet msbuild -preprocess`. Used only to PROPOSE which property
# might control a package - MSBuild decides (see version_property).
graph_elements="$work/elements.txt"

# The ids some `Update` element in that import graph targets; written by report_solution.
graph_updates="$work/updates.txt"

# Where version_property caches its MSBuild probes; one directory per solution, set by
# report_solution.
probes_dir=''
solution_count=0

# The value a candidate property is overridden to when testing whether it controls a
# version. Nothing real can produce it.
probe_sentinel='__ploch_version_probe__'

transitive_advice='transitive — bump the package that brings it in'

# Every package the SDK reports, one entry per project, framework and occurrence. Each keeps
# whether it came from `topLevelPackages` or `transitivePackages`: that - per occurrence,
# not per package id - decides whether a central version pin governs it (see
# package_owner). `.frameworks` is absent entirely on a project with nothing to report,
# hence the `// []` guards rather than a plain index.
flatten='[ .projects[]? | (.frameworks // [])[]
           | ((.topLevelPackages // [])[] | . + {transitive: false}),
             ((.transitivePackages // [])[] | . + {transitive: true}) ]'

# NuGet package ids are case-insensitive, and the SDK can spell the same package
# differently for a direct reference (as the project wrote it) and a transitive one (as
# the package declares itself). Normalise every occurrence to one spelling - a direct
# reference's where there is one - so that grouping and deduplication see one package.
canonical="(group_by(.id | ascii_downcase)
            | map({key: (.[0].id | ascii_downcase), value: (sort_by(.transitive) | .[0].id)})
            | from_entries) as \$spelling
           | map(.id = \$spelling[.id | ascii_downcase])"

# Finds the Directory.Packages.props that governs a solution: the nearest one at or above
# the solution's own directory. MSBuild actually resolves it per project, not per solution;
# for the two solutions in this repository every project sits below its solution's version
# file, so the solution directory is a faithful proxy.
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

# Runs one SDK command with its stdout captured into a file. On failure it prints the
# command's own diagnostics and returns 1, rather than letting `set -e` end the run: that
# would also delete the work directory, and with it both those diagnostics (the JSON
# renderer writes its `problems` to stdout) and every section already rendered for the
# solutions checked before this one.
run_sdk() {
  local output="$1"
  shift
  if "$@" >"$output" 2>"$output.stderr"; then
    return 0
  fi
  echo "::error::'$*' failed:" >&2
  cat "$output" "$output.stderr" >&2
  return 1
}

# Records a check that could not run, in the report and in the failure count.
report_unchecked() {
  echo "**Not checked** — $1 See the job log for the SDK's own diagnostics." >>"$report"
  echo >>"$report"
  failed_checks=$(( failed_checks + 1 ))
}

# The file's text with XML comments removed and line breaks flattened, so a commented-out
# declaration is invisible and an element split across lines reads as one.
declarations_text() {
  perl -0777 -pe 's/<!--.*?-->//gs' "$1" | tr '\r\n' '  '
}

# Every version element in the text, one per line. Quote-aware: a `>` inside an attribute
# value - `Condition="'2' > '1'"` - does not end the element.
version_elements() {
  grep -oiE "<(PackageVersion|GlobalPackageReference)([^>\"']|\"[^\"]*\"|'[^']*')*>" || true
}

# The property an element's Version attribute points at, or nothing. The attribute name is
# matched case-insensitively, as MSBuild matches metadata names: `version="$(X)"` is the
# same declaration as `Version="$(X)"`.
version_property_name() {
  sed -nE 's/.*[Vv][Ee][Rr][Ss][Ii][Oo][Nn][[:space:]]*=[[:space:]]*["'"'"'][[:space:]]*\$\(([A-Za-z_][A-Za-z0-9_.-]*)\).*/\1/p'
}

# The ids an `Update` targets anywhere in the import graph, lower-cased, one per line. An
# Update can list several ids separated by semicolons.
updated_ids() {
  sed -nE 's/.*[[:space:]][Uu][Pp][Dd][Aa][Tt][Ee][[:space:]]*=[[:space:]]*"([^"]*)".*/\1/p; s/.*[[:space:]][Uu][Pp][Dd][Aa][Tt][Ee][[:space:]]*=[[:space:]]*'"'"'([^'"'"']*)'"'"'.*/\1/p' \
      "$graph_elements" \
    | tr ';' '\n' | tr -d '[:blank:]' | tr '[:upper:]' '[:lower:]' | grep . | sort -u || true
}

# The names of the properties the import graph's version declarations point at. They are
# requested from MSBuild alongside the items, so that a named property can be checked
# against the version actually in effect before the report tells anyone to change it.
referenced_properties() {
  version_property_name <"$graph_elements" | sort -u
}

# The evaluated Version of every item declaring the package in an evaluation's output, one
# per line.
package_versions() {
  jq -r --arg id "$2" '
      [ (.Items.PackageVersion // [])[], (.Items.GlobalPackageReference // [])[]
        | select((.Identity | ascii_downcase) == ($id | ascii_downcase)) ]
      | .[] | .Version // ""' "$1"
}

# The $(Property) that controls a local version, or nothing.
#
# The claim a pointer makes is "change this property and the version changes". Reading
# the XML cannot establish that: an Update elsewhere, child <Version> metadata, an
# ItemGroup whose condition is false, an item operation inside a target that evaluation
# never runs - each defeats any textual reconstruction of what MSBuild does. So the claim
# is tested by making exactly that change: MSBuild re-evaluates the governing file with the
# candidate property overridden to a sentinel, and the property is named only if the
# package's evaluated version becomes exactly that sentinel - one item, one value, so a
# package declared twice is never attributed - and the property's real value is the version
# in effect today. That last condition matters: overriding a property can flip a Condition
# that selects a different declaration, making a property look like it controls a version
# it does not control now. The text only proposes candidates; MSBuild decides. If no
# candidate is confirmed, or the probe cannot run, nothing is named and the owner reported
# is still correct, only less specific.
version_property() {
  local id="$1" version="$2" candidate value probe confirmed=''
  while IFS= read -r candidate; do
    # Guarded: this runs inside the caller's command substitution, where an unguarded failure
    # would end the subshell under `set -e` and leave the report's "Bump in" cell empty with no
    # word of why. Naming a property is an enrichment, so a failure degrades instead.
    if ! value="$(jq -r --arg name "$candidate" '.Properties[$name] // empty' "$work/evaluation.json")"; then
      echo "::warning::could not read \$($candidate) from MSBuild's evaluation; naming no property for $id." >&2
      return 0
    fi
    if [[ -z "$value" || "$value" != "$version" ]]; then
      continue
    fi
    # One probe answers for every package, so it runs once per property per solution: one
    # property commonly pins a whole family (the sample's four Ploch.* packages). The cache
    # is on disk because this function runs in a command substitution, whose variables die
    # with it. A failed probe is remembered too - retrying it would only fail again.
    probe="$probes_dir/$candidate.json"
    if [[ ! -e "$probe" && ! -e "$probe.failed" ]]; then
      if dotnet msbuild "$governing_file" -nologo "-property:$candidate=$probe_sentinel" \
           -getItem:PackageVersion -getItem:GlobalPackageReference \
           >"$probe.partial" 2>"$probe.stderr"; then
        mv "$probe.partial" "$probe"
      else
        : >"$probe.failed"
        # The SDK's own diagnostics, once, when the probe first fails: the EXIT trap deletes
        # the work directory, so this is the only chance to show them (the trap run_sdk also
        # avoids). Later packages hitting the cached failure only repeat the warning below.
        echo "::warning::MSBuild could not re-evaluate with \$($candidate) overridden:" >&2
        cat "$probe.partial" "$probe.stderr" >&2
      fi
    fi
    if [[ -e "$probe.failed" ]]; then
      echo "::warning::could not test whether \$($candidate) controls $id; naming no property." >&2
      return 0
    fi
    if [[ "$(package_versions "$probe" "$id")" == "$probe_sentinel" ]]; then
      if [[ -n "$confirmed" ]]; then
        return 0
      fi
      confirmed="$candidate"
    fi
  done < <(grep -iF -- "$id" "$graph_elements" | version_property_name | sort -u)
  echo "$confirmed"
}

# Says where the reader has to go to change a version, for one occurrence of a package.
#
# Ownership is read from MSBuild's evaluation of the governing version file rather than
# parsed out of the XML: `DefiningProjectFullPath` names the file that actually declared
# the version, after comments, conditions and chained imports have all been resolved by
# the only tool that resolves them correctly.
#
# A central pin governs a transitive occurrence when - and only when -
# CentralPackageTransitivePinningEnabled is on. Without it NuGet resolves the version the
# parent asks for, whatever the pin says: in the main solution, Microsoft.Extensions.
# DependencyModel resolves at 10.0.0 while the shared file pins 10.0.5. This is decided per
# occurrence, not per package: one project referencing a package directly says nothing
# about another project that only receives it transitively.
#
# DefiningProjectFullPath is where the item was CREATED - the Include - not where its
# version was last SET. An `Update` in a later import rewrites the version but not that
# provenance, so a package some Update targets would be attributed to a file whose edit
# changes nothing. Telling which file's Update wins means reconstructing MSBuild's import
# order by hand, which is exactly what this script avoids; so such a package names no
# location and says why. Nothing in the real graph uses Update today.
package_owner() {
  local id="$1" transitive="$2" defining_file version property

  IFS=$'\t' read -r defining_file version < <(jq -r --arg id "$id" '
      [ (.Items.PackageVersion // [])[], (.Items.GlobalPackageReference // [])[]
        | select((.Identity | ascii_downcase) == ($id | ascii_downcase)) ]
      | first
      | [.DefiningProjectFullPath // "", .Version // ""] | @tsv' "$work/evaluation.json")

  if [[ -z "$defining_file" ]]; then
    echo "$transitive_advice"
  elif [[ "$transitive" == true && "$transitive_pinning" != true ]]; then
    echo "$transitive_advice (its central pin is not applied: transitive pinning is off)"
  elif grep -qxF -- "$(tr '[:upper:]' '[:lower:]' <<<"$id")" "$graph_updates"; then
    echo "unclear — an \`Update\` in the import graph sets its version; find the one MSBuild applies last"
  elif [[ "$defining_file" == *[\\/]mrploch-development[\\/]* ]]; then
    echo 'mrploch-development'
  else
    property="$(version_property "$id" "$version")"
    if [[ -n "$property" ]]; then
      # The number lives in a PropertyGroup that can be anywhere in the import graph and
      # can itself be another property reference, so name the property - the actionable
      # pointer - rather than claim a location that may be wrong.
      echo "pinned here as \`\$($property)\` — change that property"
    else
      echo 'this repository'
    fi
  fi
}

# Renders one solution's two tables into $report, adding to vulnerable_total and
# failed_checks.
report_solution() {
  local solution="$1" version_file
  local outdated="$work/outdated.tsv" vulnerable="$work/vulnerable.tsv" rows="$work/rows.md"
  local id resolved latest severity advisory transitive
  local -a properties=()

  echo "## \`$solution\`" >>"$report"
  echo >>"$report"

  if ! version_file="$(find_version_file "$solution")"; then
    report_unchecked 'no Directory.Packages.props governs this solution.'
    return 0
  fi
  # The whole import graph as MSBuild sees it, so the property walk in version_property
  # counts elements in imported files too, in the order MSBuild evaluates them.
  if ! run_sdk "$work/preprocessed.xml" dotnet msbuild "$version_file" -nologo -preprocess; then
    report_unchecked 'the governing Directory.Packages.props could not be preprocessed.'
    return 0
  fi
  declarations_text "$work/preprocessed.xml" | version_elements >"$graph_elements"
  updated_ids >"$graph_updates"
  governing_file="$version_file"
  # A fresh probe cache per solution: the same property name can mean something different
  # under another version file.
  solution_count=$(( solution_count + 1 ))
  probes_dir="$work/probes/$solution_count"
  mkdir -p "$probes_dir"

  mapfile -t properties < <(referenced_properties)
  if ! run_sdk "$work/evaluation.json" dotnet msbuild "$version_file" -nologo \
         -getProperty:CentralPackageTransitivePinningEnabled "${properties[@]/#/-getProperty:}" \
         -getItem:PackageVersion -getItem:GlobalPackageReference; then
    report_unchecked 'the governing Directory.Packages.props could not be evaluated.'
    return 0
  fi

  # MSBuild booleans are case-insensitive.
  transitive_pinning="$(jq -r '.Properties.CentralPackageTransitivePinningEnabled // ""' \
                          "$work/evaluation.json" | tr '[:upper:]' '[:lower:]')"

  # --- Outdated -----------------------------------------------------------------------
  # Direct packages only, deliberately. `--outdated --include-transitive` aborts the SDK
  # outright on the sample solution - `error: Sequence contains no matching element`,
  # reproducible on .NET 10 while either flag alone succeeds - so adding it would trade a
  # low-value table for a report that cannot run at all. The vulnerability table below,
  # where transitive coverage genuinely matters, does include them.
  echo '### Outdated packages' >>"$report"
  echo >>"$report"
  if ! run_sdk "$work/outdated.json" dotnet list "$solution" package --outdated --no-restore \
         --format json --output-version 1; then
    report_unchecked 'listing outdated packages failed.'
  else
    jq -r "$flatten
           | $canonical
           | unique_by(.id)
           | .[]
           | [.id, .resolvedVersion, .latestVersion] | @tsv" \
      "$work/outdated.json" >"$outdated"

    {
      if [[ ! -s "$outdated" ]]; then
        echo 'None — every package is at its latest available version.'
      else
        echo '| Package | Resolved | Latest | Bump in |'
        echo '|---|---|---|---|'
        while IFS=$'\t' read -r id resolved latest; do
          echo "| \`$id\` | $resolved | $latest | $(package_owner "$id" false) |"
        done <"$outdated"
      fi
      echo
    } >>"$report"
  fi

  # --- Vulnerable ---------------------------------------------------------------------
  # Transitive packages are included for the same reason as above, and because a
  # vulnerability reached through a dependency of a dependency is just as exploitable.
  # The JSON field really is all-lowercase `advisoryurl` in output version 1 - verified
  # against a deliberately vulnerable probe project, not assumed.
  echo '### Vulnerable packages' >>"$report"
  echo >>"$report"
  if ! run_sdk "$work/vulnerable.json" dotnet list "$solution" package --vulnerable \
         --include-transitive --no-restore --format json --output-version 1; then
    report_unchecked 'listing vulnerable packages failed.'
    return 0
  fi

  jq -r "$flatten
         | $canonical
         | map(. as \$package
               | (\$package.vulnerabilities // [])
               | map([\$package.id, \$package.resolvedVersion, .severity, .advisoryurl,
                      (\$package.transitive | tostring)]))
         | flatten(1)
         | unique
         | .[] | @tsv" \
    "$work/vulnerable.json" >"$vulnerable"

  # Rows are deduplicated after the owner is resolved, not before: a direct and a
  # transitive occurrence of the same finding collapse into one row when they need the same
  # change, and stay two rows when they do not.
  : >"$rows"
  while IFS=$'\t' read -r id resolved severity advisory transitive; do
    echo "| \`$id\` | $resolved | $severity | [advisory]($advisory) | $(package_owner "$id" "$transitive") |" >>"$rows"
  done <"$vulnerable"
  awk '!seen[$0]++' "$rows" >"$rows.unique"

  {
    if [[ ! -s "$rows.unique" ]]; then
      echo 'None.'
    else
      echo '| Package | Resolved | Severity | Advisory | Bump in |'
      echo '|---|---|---|---|---|'
      cat "$rows.unique"
    fi
    echo
  } >>"$report"

  vulnerable_total=$(( vulnerable_total + $(wc -l <"$rows.unique") ))
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

status=0
if (( vulnerable_total > 0 )); then
  echo "::error::$vulnerable_total vulnerable NuGet package finding(s) — see the report above."
  status=1
fi
if (( failed_checks > 0 )); then
  echo "::error::$failed_checks dependency check(s) could not be run — see the errors above."
  status=1
fi
exit "$status"
