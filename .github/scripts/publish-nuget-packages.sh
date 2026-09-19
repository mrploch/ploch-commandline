#!/usr/bin/env bash
#
# Publishes every NuGet package the build produced to the given feed.
#
# Usage:  publish-nuget-packages.sh [--list] [--dir <dir>] [<feed-url>]
# Reads:  NUGET_PUSH_TOKEN - the feed API key (if pushing). Taken from the environment so that this
#         script's own arguments carry nothing secret. That narrows the exposure, it does
#         not remove it: `dotnet nuget push -k` still places the key in the dotnet
#         process's command line, where it is readable from /proc for the life of that
#         process.
#         SYMBOLS_REQUIRED - If true, symbol package discovery and push failures are fatal. Default false.
#
# Why a script and not two inline `run:` blocks: the main-branch and pull-request publish
# steps previously carried a copy each of this logic, and the `./**/*.nupkg` glob bug that
# published nothing was therefore present twice. One implementation cannot drift from itself.
set -euo pipefail

LIST_ONLY="false"
SEARCH_DIR="."
FEED_URL=""

while [[ $# -gt 0 ]]; do
  case $1 in
    --list)
      LIST_ONLY="true"
      shift
      ;;
    --dir)
      if [[ $# -lt 2 ]]; then
        echo "Usage: publish-nuget-packages.sh [--list] [--dir <dir>] [<feed-url>]" >&2
        exit 1
      fi
      SEARCH_DIR="$2"
      shift 2
      ;;
    *)
      if [[ -z "$FEED_URL" ]]; then
        FEED_URL="$1"
        shift
      else
        echo "Unknown argument: $1" >&2
        exit 1
      fi
      ;;
  esac
done

if [[ "$LIST_ONLY" == "false" && -z "$FEED_URL" ]]; then
  echo "Usage: publish-nuget-packages.sh [--list] [--dir <dir>] <feed-url>" >&2
  exit 1
fi

: "${NUGET_PUSH_TOKEN:=}"
SYMBOLS_REQUIRED="${SYMBOLS_REQUIRED:-false}"

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
  if [[ "$SEARCH_DIR" == "." ]]; then
    find "$SEARCH_DIR" -type f -name "$pattern" -ipath '*/bin/Release/*' -not -ipath '*/bin/Debug/*' -not -ipath '*/tests/*' -not -ipath '*/samples/*' | sort
  else
    find "$SEARCH_DIR" -type f -name "$pattern" -not -ipath '*/bin/Debug/*' -not -ipath '*/tests/*' -not -ipath '*/samples/*' | sort
  fi
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
  echo "::error::No .nupkg files were found. Refusing to report a successful publish." >&2
  exit 1
fi

if [[ "$LIST_ONLY" == "true" ]]; then
  echo "$packages_found"
  exit 0
fi

if [[ -z "$NUGET_PUSH_TOKEN" ]]; then
  echo "NUGET_PUSH_TOKEN must be set when not using --list." >&2
  exit 1
fi

mapfile -t packages <<< "$packages_found"

if [[ "$SYMBOLS_REQUIRED" == "true" ]]; then
  if ! symbols_found=$(find_packages '*.snupkg'); then
    echo "::error::Symbol package discovery failed." >&2
    exit 1
  fi

  if [[ -z "$symbols_found" ]]; then
    echo "::error::SYMBOLS_REQUIRED is true, but no .snupkg files were found." >&2
    exit 1
  fi

  missing_symbols=()
  for pkg in "${packages[@]}"; do
    pkg_base="${pkg%.nupkg}"
    if [[ ! -f "${pkg_base}.snupkg" ]]; then
      missing_symbols+=("${pkg_base}.snupkg")
    fi
  done

  if [[ ${#missing_symbols[@]} -gt 0 ]]; then
    echo "::error::SYMBOLS_REQUIRED is true, but the following matching .snupkg files are missing:" >&2
    for missing in "${missing_symbols[@]}"; do
      echo "  $missing" >&2
    done
    exit 1
  fi
else
  if ! symbols_found=$(find_packages '*.snupkg'); then
    echo "::warning::Symbol package discovery failed; publishing without symbol packages."
    symbols_found=''
  fi
fi

for pkg in "${packages[@]}"; do
  echo "Publishing $pkg"
  dotnet nuget push "$pkg" --source "$FEED_URL" --skip-duplicate -k "$NUGET_PUSH_TOKEN"
done

if [[ -n "$symbols_found" ]]; then
  mapfile -t symbols <<< "$symbols_found"
  for sym in "${symbols[@]}"; do
    echo "Publishing $sym"
    if [[ "$SYMBOLS_REQUIRED" == "true" ]]; then
      dotnet nuget push "$sym" --source "$FEED_URL" --skip-duplicate -k "$NUGET_PUSH_TOKEN"
    else
      dotnet nuget push "$sym" --source "$FEED_URL" --skip-duplicate -k "$NUGET_PUSH_TOKEN" || true
    fi
  done
fi
