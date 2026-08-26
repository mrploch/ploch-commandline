#!/usr/bin/env bash
#
# Publishes every NuGet package the build produced to the given feed.
#
# Usage:  publish-nuget-packages.sh <feed-url>
# Reads:  GH_PACKAGES_TOKEN - the feed API key. Taken from the environment so that this
#         script's own arguments carry nothing secret. That narrows the exposure, it does
#         not remove it: `dotnet nuget push -k` still places the key in the dotnet
#         process's command line, where it is readable from /proc for the life of that
#         process. Eliminating it entirely would need an auth mechanism `nuget push`
#         does not currently offer.
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
# tests/ and samples/ are excluded defensively rather than because they would otherwise
# pack: test projects set IsPackable=false, and samples/SampleApp/Directory.Build.props
# sets GeneratePackageOnBuild=false and IsPackable=false. `-ipath` keeps the exclusion
# honest if either directory is ever renamed with different casing.
find_packages() {
  find . -type f -name "$1" -not -ipath './tests/*' -not -ipath './samples/*' | sort
}

# Captured into a variable rather than piped into `mapfile` through a process substitution:
# `mapfile < <(find ...)` discards find's exit status, so a traversal error that occurred
# after a partial result would publish a subset of the packages and report success.
# `set -o pipefail` makes the failing `find` fail the whole `find | sort` pipeline.
if ! packages_found=$(find_packages '*.nupkg'); then
  echo "::error::Package discovery failed while enumerating .nupkg files."
  exit 1
fi

if [ -z "$packages_found" ]; then
  echo "::error::No .nupkg files were found. Refusing to report a successful publish."
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

if [ -n "$symbols_found" ]; then
  mapfile -t symbols <<< "$symbols_found"
  for sym in "${symbols[@]}"; do
    echo "Publishing $sym"
    dotnet nuget push "$sym" --source "$FEED_URL" --skip-duplicate -k "$GH_PACKAGES_TOKEN" || true
  done
fi
