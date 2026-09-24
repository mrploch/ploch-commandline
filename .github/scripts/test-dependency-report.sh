#!/usr/bin/env bash
#
# Fixture test for dependency-report.sh. Runs the real script against a fake two-repository
# workspace with a mocked `dotnet` that returns canned `dotnet list package --format json`
# and `dotnet msbuild -getItem` output, so the parsing, deduplication, ownership attribution,
# failure handling and exit-code contract are pinned without a network, a restore, or a
# real solution. Mirrors test-publish-nuget-packages.sh; runs in build-dotnet.yml beside it.
#
# What this deliberately does not test: how MSBuild evaluates XML (comments, conditions,
# chained imports). The script delegates that to MSBuild precisely so that it does not
# have to reimplement it; the canned evaluation stands in for MSBuild's answer.
set -euo pipefail

script="$PWD/.github/scripts/dependency-report.sh"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

failures=0
fail() {
  echo "::error::$1" >&2
  failures=$((failures + 1))
}

# Prints the rows of one table ("### <heading>") inside one solution's section.
table() {
  local report="$1" solution="$2" heading="$3"
  awk -v solution="## \`$solution\`" -v heading="### $heading" '
    index($0, "## ") == 1 { in_solution = ($0 == solution); in_table = 0; next }
    in_solution && index($0, "### ") == 1 { in_table = ($0 == heading); next }
    in_solution && in_table && /^\| `/ { print }
  ' <<<"$report"
}

# Asserts that exactly one row names the package and that it ends with the expected owner.
# One row, not "at least one": that is how deduplication is proven.
expect_row() {
  local rows="$1" id="$2" owner="$3" matching
  matching="$(grep -F "| \`$id\` |" <<<"$rows" || true)"
  if [[ "$(grep -c . <<<"$matching")" -ne 1 ]]; then
    fail "expected exactly one row for $id, got: ${matching:-<none>}"
  elif [[ "$matching" != *"| $owner |" ]]; then
    fail "expected $id to be attributed to '$owner', got: $matching"
  fi
}

# Asserts that a complete row appears verbatim - versions, severity and link included.
expect_line() {
  local rows="$1" row="$2"
  grep -qxF "$row" <<<"$rows" || fail "expected the row: $row"
}

# Asserts how many rows name a package (case-sensitively, as rendered).
expect_count() {
  local rows="$1" id="$2" expected="$3" actual
  actual="$(grep -cF "| \`$id\` |" <<<"$rows" || true)"
  [[ "$actual" -eq "$expected" ]] || fail "expected $expected row(s) for $id, got $actual"
}

# The owner texts the report prints, and the MSBuild switch a probe overrides a property with.
local_owner='this repository'
probe_switch='-property:'
transitive='transitive — bump the package that brings it in'
transitive_unpinned="$transitive (its central pin is not applied: transitive pinning is off)"
updated="unclear — an \`Update\` in the import graph sets its version; find the one MSBuild applies last"

# --- Fake workspace ---------------------------------------------------------------------
# workspace/
#   mrploch-development/dependencies/Shared.Packages.props   the sibling's shared versions
#   repo/Directory.Packages.props                            imports the shared file
#   repo/Main.slnx
#   repo/samples/Sample/Directory.Packages.props              standalone, pins transitively
#   repo/samples/Sample/Sample.slnx
shared_file="$work/mrploch-development/dependencies/Shared.Packages.props"
main_props="$work/repo/Directory.Packages.props"
sample_props="$work/repo/samples/Sample/Directory.Packages.props"
mkdir -p "$(dirname "$shared_file")" "$(dirname "$sample_props")" "$work/bin" \
         "$work/fixtures/findings" "$work/fixtures/clean" "$work/fixtures/broken" \
         "$work/fixtures/failonly"

cat >"$shared_file" <<'XML'
<Project>
  <ItemGroup>
    <PackageVersion Include="Shared.Pkg" Version="1.0.0" />
    <PackageVersion Include="Transitive.InShared" Version="1.0.0" />
    <PackageVersion Include="Reinc.Pkg" Version="1.0.0" />
    <GlobalPackageReference Include="Analyzer.Pkg" Version="1.0.0" />
  </ItemGroup>
</Project>
XML

# Imported last by the main props: it rewrites a version the main file declared.
overrides_file="$work/mrploch-development/dependencies/Overrides.props"
cat >"$overrides_file" <<'XML'
<Project>
  <ItemGroup>
    <PackageVersion Update="Imp.Pkg" Version="11.0.0" />
  </ItemGroup>
</Project>
XML

# Declarations the property pointer must read correctly:
#   Local.Pkg   a commented-out declaration naming $(ObsoleteVersion) precedes the real one
#   Property.Pkg  single quotes
#   Spaced.Pkg  Version before Include, whitespace around both `=` signs
#   Cond.Pkg    a Condition="false" declaration MSBuild ignores names $(InactiveVersion)
#   Equal.Pkg   as Cond.Pkg, but the ignored property holds the SAME value as the active
#               literal - equality must not be mistaken for provenance
#   Gt.Pkg      a `>` inside a quoted attribute value precedes the Version attribute
#   Stale.Pkg   declared once through $(StalePkgVersion), but the version in effect is not
#               that property's value - the property no longer controls it
#   Upd.Pkg     declared through $(UpdOldVersion), then Updated in this file to the SAME
#               number - the property no longer controls it
#   Imp.Pkg     as Upd.Pkg, but the Update is in a later import (Overrides.props)
#   Reinc.Pkg   the shared file declares it; this file Removes and re-Includes it through
#               $(ReincVersion) - the live definition is certain, so it IS named
#   CondOnly.Pkg  a single live declaration behind a Condition - MSBuild confirms its
#               property controls the version, so it IS named
#   Dup.Pkg     two unconditional Includes through two properties holding the same value -
#               a package declared twice is never attributed to one of them
#   Flip.Pkg    overriding $(FlipVersion) flips a Condition so the property's declaration
#               becomes the live one - but today the literal is live and the property holds a
#               different value, so it does not control the version now and is not named
#   Multi.Pkg   the element is split across lines
#   Lower.Pkg   a lower-case `version=` attribute, which MSBuild reads as Version - named
#   SemiB.Pkg   the second id of a semicolon-separated Update - no location is claimed
#   FamA.Pkg / FamB.Pkg  one property pins both - both are named, from ONE probe
#   SqB.Pkg     targeted by a single-quoted Update - no location is claimed
# Upd.Pkg and Imp.Pkg carry an Update, so no location is claimed for them either: the
# evaluation's DefiningProjectFullPath is where the Include ran, not where the version was
# last set, and for Imp.Pkg that Update sits in the shared repository.
cat >"$main_props" <<'XML'
<Project>
  <Import Project="../mrploch-development/dependencies/Shared.Packages.props" />
  <ItemGroup>
    <!-- <PackageVersion Include="Local.Pkg" Version="$(ObsoleteVersion)" /> -->
    <PackageVersion Include="Local.Pkg" Version="1.0.0" />
    <PackageVersion Include='Property.Pkg' Version='$(PropertyPkgVersion)' />
    <PackageVersion Version = "$(SpacedPkgVersion)" Include = "Spaced.Pkg" />
    <PackageVersion Include="Cond.Pkg" Version="$(InactiveVersion)" Condition="false" />
    <PackageVersion Include="Cond.Pkg" Version="5.0.0" />
    <PackageVersion Include="Equal.Pkg" Version="$(EqualInactiveVersion)" Condition="false" />
    <PackageVersion Include="Equal.Pkg" Version="7.0.0" />
    <PackageVersion Include="Gt.Pkg" Note="'2' > '1'" Version="$(GtPkgVersion)" />
    <PackageVersion Include="Stale.Pkg" Version="$(StalePkgVersion)" />
    <PackageVersion Include="Upd.Pkg" Version="$(UpdOldVersion)" />
    <PackageVersion Update="Upd.Pkg" Version="10.0.0" />
    <PackageVersion Include="Imp.Pkg" Version="$(ImpOldVersion)" />
    <PackageVersion Remove="Reinc.Pkg" />
    <PackageVersion Include="Reinc.Pkg" Version="$(ReincVersion)" />
    <PackageVersion Include="CondOnly.Pkg" Version="$(CondOnlyVersion)" Condition="'$(NoSuchSwitch)' == ''" />
    <PackageVersion Include="Dup.Pkg" Version="$(DupFirstVersion)" />
    <PackageVersion Include="Dup.Pkg" Version="$(DupSecondVersion)" />
    <PackageVersion Include="Flip.Pkg" Version="$(FlipVersion)" Condition="'$(FlipVersion)' != '2.0.0'" />
    <PackageVersion Include="Flip.Pkg" Version="1.0.0" Condition="'$(FlipVersion)' == '2.0.0'" />
    <PackageVersion Include="Multi.Pkg"
                    Version="$(MultiPkgVersion)" />
    <PackageVersion Include="Lower.Pkg" version="$(LowerPkgVersion)" />
    <PackageVersion Include="SemiA.Pkg" Version="1.0.0" />
    <PackageVersion Include="SemiB.Pkg" Version="1.0.0" />
    <PackageVersion Update="SemiA.Pkg; SemiB.Pkg" Version="16.0.0" />
    <PackageVersion Include="FamA.Pkg" Version="$(FamVersion)" />
    <PackageVersion Include="FamB.Pkg" Version="$(FamVersion)" />
    <PackageVersion Include="SqB.Pkg" Version="1.0.0" />
    <PackageVersion Update='SqB.Pkg' Version='18.0.0' />
  </ItemGroup>
  <Import Project="../mrploch-development/dependencies/Overrides.props" />
</Project>
XML

cat >"$sample_props" <<'XML'
<Project>
  <PropertyGroup>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Sample.Pkg" Version="1.0.0" />
    <PackageVersion Include="Pinned.Transitive" Version="2.0.0" />
  </ItemGroup>
</Project>
XML
touch "$work/repo/Main.slnx" "$work/repo/samples/Sample/Sample.slnx"

# --- Canned MSBuild preprocessing (`dotnet msbuild <props> -preprocess`) ----------------
# The whole import graph in evaluation order: the shared file is imported at the top of the
# main props, Overrides.props at the bottom.
cat "$shared_file" "$main_props" "$overrides_file" >"$work/fixtures/Main.preprocessed.xml"
cp "$sample_props" "$work/fixtures/Sample.preprocessed.xml"

# --- Canned MSBuild evaluation (`dotnet msbuild <props> -getProperty -getItem`) ----------
# DefiningProjectFullPath is what MSBuild reports as the file that declared each version,
# and Version the value in effect. The main solution does not pin transitively; the sample
# does ("True" also proves the comparison is case-insensitive, as MSBuild booleans are).
# InactiveVersion evaluates to something other than Cond.Pkg's effective 5.0.0 - which is
# exactly why it must not be named.
cat >"$work/fixtures/Main.evaluation.json" <<JSON
{ "Properties": { "CentralPackageTransitivePinningEnabled": "",
                  "PropertyPkgVersion": "3.0.0", "SpacedPkgVersion": "4.0.0",
                  "InactiveVersion": "1.0.0", "MultiPkgVersion": "6.0.0",
                  "EqualInactiveVersion": "7.0.0", "GtPkgVersion": "8.0.0",
                  "StalePkgVersion": "1.0.0", "UpdOldVersion": "10.0.0",
                  "ImpOldVersion": "11.0.0", "ReincVersion": "12.0.0",
                  "CondOnlyVersion": "13.0.0", "DupFirstVersion": "14.0.0",
                  "DupSecondVersion": "14.0.0", "FlipVersion": "2.0.0",
                  "LowerPkgVersion": "15.0.0", "FamVersion": "17.0.0" },
  "Items": {
    "PackageVersion": [
      { "Identity": "Shared.Pkg",          "Version": "1.0.0", "DefiningProjectFullPath": "$shared_file" },
      { "Identity": "Transitive.InShared", "Version": "1.0.0", "DefiningProjectFullPath": "$shared_file" },
      { "Identity": "Local.Pkg",           "Version": "1.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Property.Pkg",        "Version": "3.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Spaced.Pkg",          "Version": "4.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Cond.Pkg",            "Version": "5.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Multi.Pkg",           "Version": "6.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Equal.Pkg",           "Version": "7.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Gt.Pkg",              "Version": "8.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Stale.Pkg",           "Version": "9.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Upd.Pkg",             "Version": "10.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Imp.Pkg",             "Version": "11.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Reinc.Pkg",           "Version": "12.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "CondOnly.Pkg",        "Version": "13.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Dup.Pkg",             "Version": "14.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Dup.Pkg",             "Version": "14.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Flip.Pkg",            "Version": "1.0.0",  "DefiningProjectFullPath": "$main_props" },
      { "Identity": "Lower.Pkg",           "Version": "15.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "SemiA.Pkg",           "Version": "16.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "SemiB.Pkg",           "Version": "16.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "FamA.Pkg",            "Version": "17.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "FamB.Pkg",            "Version": "17.0.0", "DefiningProjectFullPath": "$main_props" },
      { "Identity": "SqB.Pkg",             "Version": "18.0.0", "DefiningProjectFullPath": "$main_props" } ],
    "GlobalPackageReference": [
      { "Identity": "Analyzer.Pkg",        "Version": "1.0.0", "DefiningProjectFullPath": "$shared_file" } ] } }
JSON

cat >"$work/fixtures/Sample.evaluation.json" <<JSON
{ "Properties": { "CentralPackageTransitivePinningEnabled": "True" },
  "Items": {
    "PackageVersion": [
      { "Identity": "Sample.Pkg",        "Version": "1.0.0", "DefiningProjectFullPath": "$sample_props" },
      { "Identity": "Pinned.Transitive", "Version": "2.0.0", "DefiningProjectFullPath": "$sample_props" } ] } }
JSON

# --- Canned `dotnet list package` output ------------------------------------------------
# Main, outdated: Shared.Pkg is reported by two projects, the second spelling it
# `shared.pkg` (deduplication, case-insensitively), and a third project has no
# `frameworks` key at all, which is what the SDK emits for a project with nothing to report.
cat >"$work/fixtures/findings/Main.outdated.json" <<'JSON'
{ "version": 1, "projects": [
  { "path": "A.csproj", "frameworks": [ { "framework": "net10.0", "topLevelPackages": [
      { "id": "Shared.Pkg",   "resolvedVersion": "1.0.0", "latestVersion": "2.0.0" },
      { "id": "Local.Pkg",    "resolvedVersion": "1.0.0", "latestVersion": "1.1.0" },
      { "id": "Property.Pkg", "resolvedVersion": "3.0.0", "latestVersion": "3.1.0" },
      { "id": "Spaced.Pkg",   "resolvedVersion": "4.0.0", "latestVersion": "4.2.0" },
      { "id": "Cond.Pkg",     "resolvedVersion": "5.0.0", "latestVersion": "5.1.0" },
      { "id": "Multi.Pkg",    "resolvedVersion": "6.0.0", "latestVersion": "6.1.0" },
      { "id": "Equal.Pkg",    "resolvedVersion": "7.0.0", "latestVersion": "7.1.0" },
      { "id": "Gt.Pkg",       "resolvedVersion": "8.0.0", "latestVersion": "8.1.0" },
      { "id": "Stale.Pkg",    "resolvedVersion": "9.0.0", "latestVersion": "9.1.0" },
      { "id": "Upd.Pkg",      "resolvedVersion": "10.0.0", "latestVersion": "10.1.0" },
      { "id": "Imp.Pkg",      "resolvedVersion": "11.0.0", "latestVersion": "11.1.0" },
      { "id": "Reinc.Pkg",    "resolvedVersion": "12.0.0", "latestVersion": "12.1.0" },
      { "id": "CondOnly.Pkg", "resolvedVersion": "13.0.0", "latestVersion": "13.1.0" },
      { "id": "Dup.Pkg",      "resolvedVersion": "14.0.0", "latestVersion": "14.1.0" },
      { "id": "Flip.Pkg",     "resolvedVersion": "1.0.0",  "latestVersion": "1.1.0" },
      { "id": "Lower.Pkg",    "resolvedVersion": "15.0.0", "latestVersion": "15.1.0" },
      { "id": "SemiB.Pkg",    "resolvedVersion": "16.0.0", "latestVersion": "16.1.0" },
      { "id": "FamA.Pkg",     "resolvedVersion": "17.0.0", "latestVersion": "17.1.0" },
      { "id": "FamB.Pkg",     "resolvedVersion": "17.0.0", "latestVersion": "17.1.0" },
      { "id": "SqB.Pkg",      "resolvedVersion": "18.0.0", "latestVersion": "18.1.0" },
      { "id": "Analyzer.Pkg", "resolvedVersion": "1.0.0", "latestVersion": "1.2.0" } ] } ] },
  { "path": "B.csproj", "frameworks": [ { "framework": "net10.0", "topLevelPackages": [
      { "id": "shared.pkg",   "resolvedVersion": "1.0.0", "latestVersion": "2.0.0" } ] } ] },
  { "path": "C.csproj" } ] }
JSON

# Main, vulnerable (transitive pinning is off here):
#   Shared.Pkg   direct in A and B (B spells it SHARED.PKG), one advisory  -> one row, shared pin
#                transitive-only in D at another version                    -> its own row, pin not in effect
#                transitive-only in E at the same version as A              -> its own row, pin not in effect
#   Local.Pkg    direct, two distinct advisories                           -> two rows
#   Transitive.InShared  transitive only; the shared file pins it          -> pin not in effect
cat >"$work/fixtures/findings/Main.vulnerable.json" <<'JSON'
{ "version": 1, "projects": [
  { "path": "A.csproj", "frameworks": [ { "framework": "net10.0",
      "topLevelPackages": [
        { "id": "Shared.Pkg", "resolvedVersion": "1.0.0",
          "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-shared" } ] },
        { "id": "Local.Pkg", "resolvedVersion": "1.0.0",
          "vulnerabilities": [ { "severity": "High",     "advisoryurl": "https://example.test/GHSA-local-1" },
                               { "severity": "Moderate", "advisoryurl": "https://example.test/GHSA-local-2" } ] },
        { "id": "Property.Pkg", "resolvedVersion": "3.0.0",
          "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-prop-1" },
                               { "severity": "Low",  "advisoryurl": "https://example.test/GHSA-prop-2" } ] } ],
      "transitivePackages": [
        { "id": "Transitive.InShared", "resolvedVersion": "1.0.0",
          "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-main" } ] } ] } ] },
  { "path": "B.csproj", "frameworks": [ { "framework": "net10.0", "topLevelPackages": [
        { "id": "SHARED.PKG", "resolvedVersion": "1.0.0",
          "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-shared" } ] } ] } ] },
  { "path": "C.csproj" },
  { "path": "D.csproj", "frameworks": [ { "framework": "net10.0", "transitivePackages": [
        { "id": "Shared.Pkg", "resolvedVersion": "0.9.0",
          "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-shared" } ] } ] } ] },
  { "path": "E.csproj", "frameworks": [ { "framework": "net10.0", "transitivePackages": [
        { "id": "shared.pkg", "resolvedVersion": "1.0.0",
          "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-shared" } ] } ] } ] } ] }
JSON

cat >"$work/fixtures/findings/Sample.outdated.json" <<'JSON'
{ "version": 1, "projects": [
  { "path": "Sample.csproj", "frameworks": [ { "framework": "net10.0", "topLevelPackages": [
      { "id": "Sample.Pkg", "resolvedVersion": "1.0.0", "latestVersion": "1.5.0" } ] } ] } ] }
JSON

# Sample, vulnerable - the sample pins transitively:
#   Transitive.InShared  transitive, not pinned by the sample; the shared file it never
#                        imports is irrelevant
#   Pinned.Transitive    transitive in one project and direct in another; the sample's own
#                        pin governs both, so the two occurrences are one row
cat >"$work/fixtures/findings/Sample.vulnerable.json" <<'JSON'
{ "version": 1, "projects": [
  { "path": "Sample.csproj", "frameworks": [ { "framework": "net10.0", "transitivePackages": [
      { "id": "Transitive.InShared", "resolvedVersion": "1.0.0",
        "vulnerabilities": [ { "severity": "High", "advisoryurl": "https://example.test/GHSA-sample" } ] },
      { "id": "Pinned.Transitive", "resolvedVersion": "2.0.0",
        "vulnerabilities": [ { "severity": "Low", "advisoryurl": "https://example.test/GHSA-pinned" } ] } ] } ] },
  { "path": "Sample.Tests.csproj", "frameworks": [ { "framework": "net10.0", "topLevelPackages": [
      { "id": "Pinned.Transitive", "resolvedVersion": "2.0.0",
        "vulnerabilities": [ { "severity": "Low", "advisoryurl": "https://example.test/GHSA-pinned" } ] } ] } ] } ] }
JSON

# The clean scenario: nothing vulnerable, and the sample has nothing outdated either.
nothing='{ "version": 1, "projects": [ { "path": "X.csproj" } ] }'
cp "$work/fixtures/findings/Main.outdated.json" "$work/fixtures/clean/"
echo "$nothing" >"$work/fixtures/clean/Sample.outdated.json"
echo "$nothing" >"$work/fixtures/clean/Main.vulnerable.json"
echo "$nothing" >"$work/fixtures/clean/Sample.vulnerable.json"

# The broken scenario: the main solution reports its vulnerabilities, then the SDK fails on
# the sample's vulnerability query. `.fail` files are printed to stdout with a non-zero
# exit, which is how the JSON renderer reports its problems.
problem='{ "version": 1, "problems": [ { "level": "error", "text": "NU1301: Unable to load the service index for source." } ] }'
cp "$work/fixtures/findings/Main.outdated.json" "$work/fixtures/findings/Main.vulnerable.json" \
   "$work/fixtures/findings/Sample.outdated.json" "$work/fixtures/broken/"
echo "$problem" >"$work/fixtures/broken/Sample.vulnerable.fail"

# The failure-only scenario: nothing vulnerable anywhere, and one check fails. Proves that
# a check that could not run fails the run on its own, rather than riding on a finding.
cp "$work/fixtures/clean/Main.outdated.json" "$work/fixtures/clean/Main.vulnerable.json" \
   "$work/fixtures/clean/Sample.vulnerable.json" "$work/fixtures/failonly/"
echo "$problem" >"$work/fixtures/failonly/Sample.outdated.fail"

# Which property genuinely controls which package's version: what MSBuild's re-evaluation
# with that property overridden would show. Stale.Pkg, Upd.Pkg, Imp.Pkg, Cond.Pkg and
# Equal.Pkg are absent on purpose - each names a property in its XML that does not control
# its version, which is exactly what the probe exists to catch. Dup.Pkg is declared twice,
# so even a controlling property must not be named.
cat >"$work/fixtures/Main.controllers.json" <<'JSON'
{ "Property.Pkg": "PropertyPkgVersion", "Spaced.Pkg": "SpacedPkgVersion",
  "Multi.Pkg": "MultiPkgVersion", "Gt.Pkg": "GtPkgVersion", "Reinc.Pkg": "ReincVersion",
  "CondOnly.Pkg": "CondOnlyVersion", "Dup.Pkg": "DupFirstVersion", "Flip.Pkg": "FlipVersion",
  "Lower.Pkg": "LowerPkgVersion", "FamA.Pkg": "FamVersion", "FamB.Pkg": "FamVersion" }
JSON
echo '{}' >"$work/fixtures/Sample.controllers.json"

# The probe-failure scenario: the findings data, but MSBuild refuses every probe. Naming a
# property is an enrichment, so the report must degrade - no property named anywhere -
# rather than fail or guess.
mkdir -p "$work/fixtures/probefail"
cp "$work/fixtures/findings/"*.json "$work/fixtures/probefail/"
: >"$work/fixtures/probefail/probe.fail"

# Mock `dotnet`: records every call, then answers `msbuild -preprocess` with the canned
# import graph, a `msbuild ... -property:NAME=VALUE` probe as MSBuild would (every package
# that NAME genuinely controls - per the controllers table - takes VALUE; nothing else
# changes), other `msbuild` calls with the canned evaluation, and `list <solution> package
# ...` with the fixture for that solution and mode. A list call that names neither mode is
# refused, so dropping a mode flag cannot pass unnoticed.
cat >"$work/bin/dotnet" <<'MOCK'
#!/usr/bin/env bash
set -euo pipefail
echo "$*" >>"$CALLS"
if [[ "$1" == msbuild ]]; then
  name=Main
  [[ "$2" == *samples* ]] && name=Sample
  override=''
  for argument in "$@"; do
    [[ "$argument" == -property:* ]] && override="${argument#-property:}"
  done
  if [[ " $* " == *' -preprocess '* ]]; then
    cat "$FIXTURES/$name.preprocessed.xml"
  elif [[ -n "$override" ]]; then
    if [[ -f "$FIXTURES/$SCENARIO/probe.fail" ]]; then
      echo 'error MSB1006: Property is not valid.' >&2
      exit 1
    fi
    jq --arg name "${override%%=*}" --arg value "${override#*=}" \
       --slurpfile controllers "$FIXTURES/$name.controllers.json" '
         { Items: (.Items | with_entries(.value |= map(
             if $controllers[0][.Identity] == $name then .Version = $value else . end))) }' \
       "$FIXTURES/$name.evaluation.json"
  else
    cat "$FIXTURES/$name.evaluation.json"
  fi
  exit 0
fi
mode=''
for argument in "$@"; do
  case "$argument" in
    --outdated)   mode=outdated ;;
    --vulnerable) mode=vulnerable ;;
  esac
done
if [[ -z "$mode" ]]; then
  echo "mock dotnet: list called without --outdated or --vulnerable: $*" >&2
  exit 64
fi
fixture="$FIXTURES/$SCENARIO/$(basename "$2" .slnx).$mode"
if [[ -f "$fixture.fail" ]]; then
  cat "$fixture.fail"
  exit 1
fi
cat "$fixture.json"
MOCK
chmod +x "$work/bin/dotnet"
export PATH="$work/bin:$PATH" FIXTURES="$work/fixtures" CALLS="$work/calls.log"
unset GITHUB_STEP_SUMMARY

# Runs the report for one scenario. Its stdout - the report alone - is the function's output;
# its stderr, where every diagnostic and the closing errors go, is kept in $diagnostics_log.
diagnostics_log="$work/stderr.log"
run_report() {
  local scenario="$1"
  (cd "$work/repo" && SCENARIO="$scenario" bash "$script" ./Main.slnx ./samples/Sample/Sample.slnx) \
    2>"$diagnostics_log"
}

# --- Scenario: findings -----------------------------------------------------------------
echo 'Scenario: vulnerable packages present'
: >"$CALLS"
set +e
output="$(run_report findings)"
status=$?
set -e

[[ $status -eq 1 ]] || fail "expected exit code 1 when vulnerabilities exist, got $status"

main_outdated="$(table "$output" ./Main.slnx 'Outdated packages')"
expect_row  "$main_outdated" Shared.Pkg   'mrploch-development'
expect_row  "$main_outdated" Analyzer.Pkg 'mrploch-development'
expect_row  "$main_outdated" Local.Pkg    "$local_owner"
expect_row  "$main_outdated" Property.Pkg "pinned here as \`\$(PropertyPkgVersion)\` — change that property"
expect_row  "$main_outdated" Spaced.Pkg   "pinned here as \`\$(SpacedPkgVersion)\` — change that property"
expect_row  "$main_outdated" Cond.Pkg     "$local_owner"
expect_row  "$main_outdated" Multi.Pkg    "pinned here as \`\$(MultiPkgVersion)\` — change that property"
expect_row  "$main_outdated" Equal.Pkg    "$local_owner"
expect_row  "$main_outdated" Gt.Pkg       "pinned here as \`\$(GtPkgVersion)\` — change that property"
expect_row  "$main_outdated" Stale.Pkg    "$local_owner"
expect_row  "$main_outdated" Upd.Pkg      "$updated"
expect_row  "$main_outdated" Imp.Pkg      "$updated"
expect_row  "$main_outdated" SemiB.Pkg    "$updated"
expect_row  "$main_outdated" SqB.Pkg      "$updated"
expect_row  "$main_outdated" Lower.Pkg    "pinned here as \`\$(LowerPkgVersion)\` — change that property"
expect_row  "$main_outdated" FamA.Pkg     "pinned here as \`\$(FamVersion)\` — change that property"
expect_row  "$main_outdated" FamB.Pkg     "pinned here as \`\$(FamVersion)\` — change that property"
expect_row  "$main_outdated" Reinc.Pkg    "pinned here as \`\$(ReincVersion)\` — change that property"
expect_row  "$main_outdated" CondOnly.Pkg "pinned here as \`\$(CondOnlyVersion)\` — change that property"
expect_row  "$main_outdated" Dup.Pkg      "$local_owner"
expect_row  "$main_outdated" Flip.Pkg     "$local_owner"
expect_line "$main_outdated" "| \`Shared.Pkg\` | 1.0.0 | 2.0.0 | mrploch-development |"
expect_count "$main_outdated" shared.pkg 0

main_vulnerable="$(table "$output" ./Main.slnx 'Vulnerable packages')"
expect_count "$main_vulnerable" Shared.Pkg 3
expect_count "$main_vulnerable" shared.pkg 0
expect_count "$main_vulnerable" SHARED.PKG 0
expect_line "$main_vulnerable" "| \`Shared.Pkg\` | 1.0.0 | High | [advisory](https://example.test/GHSA-shared) | mrploch-development |"
expect_line "$main_vulnerable" "| \`Shared.Pkg\` | 1.0.0 | High | [advisory](https://example.test/GHSA-shared) | $transitive_unpinned |"
expect_line "$main_vulnerable" "| \`Shared.Pkg\` | 0.9.0 | High | [advisory](https://example.test/GHSA-shared) | $transitive_unpinned |"
expect_row  "$main_vulnerable" Transitive.InShared "$transitive_unpinned"
expect_line "$main_vulnerable" "| \`Local.Pkg\` | 1.0.0 | High | [advisory](https://example.test/GHSA-local-1) | $local_owner |"
expect_line "$main_vulnerable" "| \`Local.Pkg\` | 1.0.0 | Moderate | [advisory](https://example.test/GHSA-local-2) | $local_owner |"
expect_line "$main_vulnerable" "| \`Property.Pkg\` | 3.0.0 | High | [advisory](https://example.test/GHSA-prop-1) | pinned here as \`\$(PropertyPkgVersion)\` — change that property |"
expect_line "$main_vulnerable" "| \`Property.Pkg\` | 3.0.0 | Low | [advisory](https://example.test/GHSA-prop-2) | pinned here as \`\$(PropertyPkgVersion)\` — change that property |"

sample_outdated="$(table "$output" ./samples/Sample/Sample.slnx 'Outdated packages')"
sample_vulnerable="$(table "$output" ./samples/Sample/Sample.slnx 'Vulnerable packages')"
expect_row "$sample_outdated"   Sample.Pkg          "$local_owner"
expect_row "$sample_vulnerable" Transitive.InShared "$transitive"
expect_row "$sample_vulnerable" Pinned.Transitive   "$local_owner"

# 8 rows for the main solution (Shared.Pkg x3, Local.Pkg x2, Property.Pkg x2,
# Transitive.InShared) and 2 for the sample.
grep -qF '10 vulnerable NuGet package finding(s)' "$diagnostics_log" \
  || fail 'expected the vulnerability total (10) in the closing error'
# The closing errors are diagnostics: stdout carries the report alone.
grep -qF '::error::' <<<"$output" && fail 'expected no ::error:: line in the report on stdout'

# Probes are cached per property: Property.Pkg appears in the outdated table and twice in
# the vulnerable one, yet MSBuild is asked to confirm its property once.
duplicate_probes="$(grep -- "$probe_switch" "$CALLS" | sort | uniq -d)"
[[ -z "$duplicate_probes" ]] || fail "expected each property probe to run once, repeated: $duplicate_probes"
[[ "$(grep -c -- "${probe_switch}PropertyPkgVersion=" "$CALLS")" -eq 1 ]] \
  || fail "expected \$(PropertyPkgVersion) to be probed exactly once"
# A probe is also shared between packages: $(FamVersion) pins FamA.Pkg and FamB.Pkg, and
# one re-evaluation answers for both.
[[ "$(grep -c -- "${probe_switch}FamVersion=" "$CALLS")" -eq 1 ]] \
  || fail "expected \$(FamVersion) to be probed once for both packages it pins"

# The SDK arguments the documentation calls load-bearing.
grep -q '^list ' "$CALLS" || fail 'expected dotnet list to be called'
while read -r call; do
  [[ "$call" == *'--format json'* && "$call" == *'--output-version 1'* && "$call" == *'--no-restore'* ]] \
    || fail "dotnet list called without the JSON format or with a restore: $call"
  case "$call" in
    *--outdated*--vulnerable* | *--vulnerable*--outdated*)
      fail "dotnet list called with both modes: $call" ;;
    *--outdated*--include-transitive* | *--include-transitive*--outdated*)
      fail "--include-transitive on the outdated query aborts the SDK on the sample: $call" ;;
    *--vulnerable*)
      [[ "$call" == *--include-transitive* ]] || fail "vulnerable query must include transitive packages: $call" ;;
    *--outdated*) ;;
    *)
      fail "dotnet list called without a mode: $call" ;;
  esac
done < <(grep '^list ' "$CALLS")
while read -r call; do
  [[ "$call" == *-getProperty:CentralPackageTransitivePinningEnabled* \
     && "$call" == *-getItem:PackageVersion* && "$call" == *-getItem:GlobalPackageReference* ]] \
    || fail "dotnet msbuild evaluation is missing a property or item type: $call"
done < <(grep '^msbuild .*-getItem' "$CALLS" | grep -v -- "$probe_switch")
[[ "$(grep -c '^msbuild .* -preprocess' "$CALLS")" -eq 2 ]] \
  || fail 'expected each solution to preprocess its version file exactly once'
# A property is named only after MSBuild confirms it: each probe overrides exactly one
# property, and one did run for a property the report then names.
while read -r call; do
  [[ "$(grep -o -- "$probe_switch" <<<"$call" | grep -c .)" -eq 1 ]] \
    || fail "a probe must override exactly one property: $call"
done < <(grep '^msbuild .*-property:' "$CALLS")
grep -qF -- "${probe_switch}ReincVersion=" "$CALLS" \
  || fail "expected MSBuild to be asked to confirm \$(ReincVersion) before it is named"
main_evaluation="$(grep '^msbuild .*Directory\.Packages\.props.*-getItem' "$CALLS" | grep -v samples | grep -v -- "$probe_switch" || true)"
for property in PropertyPkgVersion SpacedPkgVersion InactiveVersion MultiPkgVersion \
                GtPkgVersion StalePkgVersion UpdOldVersion ImpOldVersion ReincVersion; do
  [[ "$main_evaluation" == *"-getProperty:$property"* ]] \
    || fail "expected the referenced property $property to be requested from MSBuild"
done
[[ "$main_evaluation" != *ObsoleteVersion* ]] \
  || fail 'a property named only inside an XML comment must not be requested'

# --- Scenario: clean --------------------------------------------------------------------
echo 'Scenario: nothing vulnerable'
: >"$CALLS"
summary="$work/summary.md"
: >"$summary"
set +e
output="$(GITHUB_STEP_SUMMARY="$summary" run_report clean)"
status=$?
set -e

[[ $status -eq 0 ]] || fail "expected exit code 0 when nothing is vulnerable, got $status"
[[ "$(grep -c '^None\.$' <<<"$output")" -eq 2 ]] \
  || fail 'expected an empty vulnerability table ("None.") for both solutions'
grep -qxF 'None — every package is at its latest available version.' <<<"$output" \
  || fail 'expected the empty outdated table for the sample'
# The summary is the same report, not a different rendering of it.
[[ "$(cat "$summary")" == "$output" ]] || fail 'expected the job summary to carry the same report as the log'

# --- Scenario: SDK failure after findings ---------------------------------------------
echo 'Scenario: the SDK fails on the second solution'
: >"$CALLS"
set +e
output="$(run_report broken)"
status=$?
set -e

[[ $status -eq 1 ]] || fail "expected exit code 1 when a check could not run, got $status"
expect_line "$(table "$output" ./Main.slnx 'Vulnerable packages')" \
  "| \`Shared.Pkg\` | 1.0.0 | High | [advisory](https://example.test/GHSA-shared) | mrploch-development |"
grep -qF '**Not checked** — listing vulnerable packages failed.' <<<"$output" \
  || fail 'expected the failed check to be marked in the report'
grep -qF 'NU1301' "$diagnostics_log" || fail "expected the SDK's own diagnostics on stderr"
grep -qF '1 dependency check(s) could not be run' "$diagnostics_log" \
  || fail 'expected the failed-check total in the closing error'

# --- Scenario: SDK failure with nothing vulnerable ------------------------------------
echo 'Scenario: a check fails and nothing is vulnerable'
: >"$CALLS"
set +e
output="$(run_report failonly)"
status=$?
set -e

[[ $status -eq 1 ]] || fail "expected a failed check alone to fail the run, got $status"
grep -qF 'vulnerable NuGet package finding(s)' "$diagnostics_log" \
  && fail 'expected no vulnerability error when nothing is vulnerable'
grep -qF '**Not checked** — listing outdated packages failed.' <<<"$output" \
  || fail 'expected the failed outdated check to be marked in the report'
grep -qF '1 dependency check(s) could not be run' "$diagnostics_log" \
  || fail 'expected the failed-check total in the closing error'
# A failed outdated query must not cost the same solution its vulnerability check.
grep -q '^list \./samples/Sample/Sample\.slnx package --vulnerable' "$CALLS" \
  || fail "expected the sample's vulnerability query to run after its outdated query failed"
sample_vulnerable_section="$(awk '
  index($0, "## ") == 1 { in_sample = ($0 == "## `./samples/Sample/Sample.slnx`"); in_table = 0; next }
  in_sample && index($0, "### ") == 1 { in_table = ($0 == "### Vulnerable packages"); next }
  in_sample && in_table { print }' <<<"$output")"
grep -qxF 'None.' <<<"$sample_vulnerable_section" \
  || fail "expected the sample's vulnerability section to complete after its outdated query failed"

# --- Scenario: MSBuild refuses the probe ------------------------------------------------
echo 'Scenario: a property probe cannot run'
: >"$CALLS"
set +e
output="$(run_report probefail)"
status=$?
set -e
[[ $status -eq 1 ]] || fail "expected only the vulnerability findings to fail the run, got $status"
grep -qF 'dependency check(s) could not be run' "$diagnostics_log" \
  && fail 'a failed probe is an enrichment failing, not a check that could not run'
probefail_outdated="$(table "$output" ./Main.slnx 'Outdated packages')"
expect_row "$probefail_outdated" Property.Pkg "$local_owner"
expect_row "$probefail_outdated" Reinc.Pkg    "$local_owner"
expect_row "$probefail_outdated" Shared.Pkg   'mrploch-development'
grep -qF '::warning::could not test whether' "$diagnostics_log" \
  || fail 'expected a warning when a property probe cannot run'
# The SDK's own reason reaches the log, and only once per failed property: the work
# directory holding it is deleted on exit.
diagnostics="$(grep -cF 'MSB1006' "$diagnostics_log" || true)"
reevaluations="$(grep -cF 'could not re-evaluate' "$diagnostics_log" || true)"
if (( diagnostics == 0 || diagnostics != reevaluations )); then
  fail "expected MSBuild's own diagnostics once per failed probe, got $diagnostics for $reevaluations"
fi
[[ "$(grep -cF "could not re-evaluate with \$(PropertyPkgVersion)" "$diagnostics_log")" -eq 1 ]] \
  || fail 'expected the diagnostics of a failed probe to be shown once, not per package'

# --- Scenario: missing sibling ----------------------------------------------------------
echo 'Scenario: mrploch-development not checked out'
mv "$work/mrploch-development" "$work/mrploch-development.away"
set +e
(cd "$work/repo" && SCENARIO=clean bash "$script" ./Main.slnx >/dev/null 2>&1)
status=$?
set -e
mv "$work/mrploch-development.away" "$work/mrploch-development"
[[ $status -eq 2 ]] || fail "expected exit code 2 without the sibling repository, got $status"

if (( failures > 0 )); then
  echo "$failures assertion(s) failed." >&2
  exit 1
fi
echo 'All dependency-report assertions passed.'
