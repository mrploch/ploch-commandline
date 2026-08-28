#!/usr/bin/env bash
#
# Publishes every NuGet package the build produced to the given feed.
#
# Usage:  publish-nuget-packages.sh <feed-url>
# Reads:  GH_PACKAGES_TOKEN - the feed API key. Taken from the environment so that this
#         script's own arguments carry nothing secret. That narrows the exposure, it does
#         not remove it: `dotnet nuget push -k` still places the key in the dotnet
#         process's command line, where it is readable from /proc for the life of that
#         process. NuGet.Config already carries packageSourceCredentials for the "github"
#         source using %GH_PACKAGES_TOKEN%, so pushing to the source name instead of the
#         URL would drop `-k` and keep the key off every command line. That is deferred to
#         #45 rather than changed here: this publish path is not exercised by CI on a PR
#         targeting a feature branch, so a credential-resolution failure would first show
#         up on a push to main - a bad place to discover it, in the very step this branch
#         exists to make reliable.
#
# Why a script and not two inline `run:` blocks: the main-branch and pull-request publish
# steps previously carried a copy each of this logic, and the `./**/*.nupkg` glob bug that
# published nothing was therefore present twice. One implementation cannot drift from itself.
set -euo pipefail

FEED_URL="${1:?Usage: publish-nuget-packages.sh <feed-url>}"
: "${GH_PACKAGES_TOKEN:?GH_PACKAGES_TOKEN must be set}"

# Enumerated with `find`, never a glob: globstar is off by default and GitHub runs steps
# with `bash -e`, so `./**/*.nupkg` matches a single directory level while packages are
# produced four levels down. An unmatched glob is then passed through literally and
# `dotnet nuget push` exits 0 on it - the step reports success having published nothing.
#
# Restricted to bin/Release: Directory.Build.props sets GeneratePackageOnBuild=true for every
# non-test project, so a library packs on EVERY build, not only the Release one. The workflow
# builds these projects more than once, and a Debug pack therefore sits beside the Release pack
# carrying the identical version. Unfiltered, `sort` orders 'bin/Debug' before 'bin/Release', so
# the Debug artefact claims the version on the feed and --skip-duplicate silently swallows the
# Release one that follows. That is not hypothetical: run 33169218883 on this very branch pushed
# the Debug build of all four packages and reported success --
#
#   Publishing ./src/Spectre/CommandLine.Spectre/bin/Debug/...nupkg   -> Your package was pushed.
#   Publishing ./src/Spectre/CommandLine.Spectre/bin/Release/...nupkg -> already exists at feed
#
# which is the same shape of failure this script exists to remove: a green step shipping the
# wrong thing.
#
# tests/ and samples/ are excluded defensively rather than because they would otherwise pack:
# test projects set IsPackable=false, and samples/SampleApp/Directory.Build.props sets
# GeneratePackageOnBuild=false and IsPackable=false. `-ipath` keeps every path predicate honest
# if a directory is ever renamed with different casing.
find_packages() {
  local pattern="$1"

  find . -type f -name "$pattern" -ipath '*/bin/Release/*' -not -ipath './tests/*' -not -ipath './samples/*' | sort
}

# Captured into a variable rather than piped into `mapfile` through a process substitution:
# `mapfile < <(find ...)` discards find's exit status, so a traversal error that occurred
# after a partial result would publish a subset of the packages and report success.
# `set -o pipefail` makes the failing `find` fail the whole `find | sort` pipeline.
if ! packages_found=$(find_packages '*.nupkg'); then
  echo "::error::Package discovery failed while enumerating .nupkg files." >&2
  exit 1
fi

if [[ -z "$packages_found" ]]; then
  echo "::error::No Release .nupkg files were found. Refusing to report a successful publish." >&2
  exit 1
fi

mapfile -t packages <<< "$packages_found"

for pkg in "${packages[@]}"; do
  echo "Publishing $pkg"
  dotnet nuget push "$pkg" --source "$FEED_URL" --skip-duplicate -k "$GH_PACKAGES_TOKEN"
done

# Symbol packages are best-effort: a missing or unpushable .snupkg is not a failed release,
# so a discovery failure here warns instead of exiting.
if ! symbols_found=$(find_packages '*.snupkg'); then
  echo "::warning::Symbol package discovery failed; publishing without symbol packages."
  symbols_found=''
fi

if [[ -n "$symbols_found" ]]; then
  mapfile -t symbols <<< "$symbols_found"
  for sym in "${symbols[@]}"; do
    echo "Publishing $sym"
    dotnet nuget push "$sym" --source "$FEED_URL" --skip-duplicate -k "$GH_PACKAGES_TOKEN" || true
  done
fi
