# Contributing to Ploch.CommandLine

Thanks for considering a contribution. This file covers what you need to build the repository,
and one policy — **Ploch dependencies are always stable** — that exists specifically so that a
pull request from a fork builds exactly like one from this repository.

## Repository layout: clone the siblings

This repository is developed as part of a workspace of sibling checkouts, and it reads shared
build configuration from one of them. Clone them next to each other:

```text
<workspace>/
  ploch-commandline/      # this repository
  mrploch-development/    # required — shared package versions and build props
  ploch-common/           # optional — only for project-reference mode (see below)
```

```bash
git clone https://github.com/mrploch/ploch-commandline.git
git clone https://github.com/mrploch/mrploch-development.git
git clone https://github.com/mrploch/ploch-common.git   # optional
```

`Directory.Packages.props` imports the shared version files from `../mrploch-development`
unconditionally, so **without that sibling checkout restore fails immediately**. All three
repositories are public, so no credentials are needed to clone them. This is the same layout CI
reproduces — `build-dotnet.yml` clones the siblings beside the checkout before restoring.

## Build and test

```bash
dotnet build Ploch.CommandLine.Spectre.slnx
dotnet test  Ploch.CommandLine.Spectre.slnx
```

The repository builds with `TreatWarningsAsErrors`, so a new warning is a broken build. Please
keep the build clean rather than suppressing it.

The sample application is a separate, deliberately standalone solution and needs its own command
line — see [`samples/SampleApp/README.md`](samples/SampleApp/README.md).

## Building the main solution without a token is best-effort

`nuget.config` maps `Ploch.*` to two feeds: `nuget.org` and the `github` feed
(`nuget.pkg.github.com/mrploch`), which authenticates with the `GH_PACKAGES_TOKEN` environment
variable. **For the main solution that variable is optional.** Every Ploch version the main
solution references is a stable release present on nuget.org, so a build with no token is expected
to restore — usually it does, and it is not guaranteed to. The caveat is spelled out below; read it
before concluding that a failed restore means you have done something wrong.

You will see warnings from the unauthenticated feed and they are expected:

```text
warning : Your request could not be authenticated by the GitHub Packages service.
  Response status code does not indicate success: 401 (Unauthorized).
```

NuGet retries and falls back to nuget.org. Measured on 2026-09-20 (UTC) with the variable unset,
`--no-cache` and an empty packages folder: exit code 0. That is one dated measurement, not a
guarantee — see below.

**Best-effort, not guaranteed.** The GitHub Packages source stays eligible while unauthenticated.
NuGet queries sources concurrently and rethrows a terminal protocol failure from one of them
rather than ignoring it, so if that feed exhausts its retries before nuget.org returns the match,
restore can fail with `NU1301` even though the package is on nuget.org. That is a race, not a
certainty — the measurement above is one sample of it. If you hit it, retry, or set a token
(a personal access token with `read:packages`), which removes the race. Do not put a token in any
tracked file — export it in your shell.

**The sample application is a separate case.** `samples/SampleApp` is a standalone solution with
its own `Directory.Packages.props`, and it deliberately pins a *prerelease* build of this
repository's own `Ploch.CommandLine.*` packages, which exist only on GitHub Packages. Its default
(package) mode therefore does need a token. Build it the way CI does — with
`-p:UsePlochProjectReferences=true`, which needs no feed access at all. See
[`samples/SampleApp/README.md`](samples/SampleApp/README.md).

## Ploch dependencies are always stable (policy)

> The **main solution** must never reference a **prerelease** `Ploch.*` version. Prerelease
> consumption is opt-in local work only.

The policy is about the main solution's `ploch-common` dependencies. `samples/SampleApp` is
outside it by design — it is a separate central-package root pinning a prerelease build of this
repository's *own* packages, which is a different thing and is documented in its own files.

The versions come from `../mrploch-development/dependencies/Ploch.Packages.props`, which CI reads
from that repository's moving `main` branch — so this policy has to be honoured *there* as well
as here.

Two things break the moment a prerelease version is referenced:

1. **Fork pull requests cannot build.** Prerelease Ploch builds are published to GitHub Packages
   only, never to nuget.org. GitHub withholds repository secrets from pull requests opened from
   forks, so `GH_PACKAGES_TOKEN` arrives empty and restore fails hard — with
   `error NU1301: … Response status code does not indicate success: 401 (Unauthorized)`, an error
   about a secret the contributor cannot be given. The same applies to any local build without a
   token.
2. **The release build would pack a stable package with a prerelease dependency**, which is the
   `NU5104` defect that [#47](https://github.com/mrploch/ploch-commandline/issues/47) removed.

### The guard

`build-dotnet.yml` runs a check before restore that fails the build when any resolved `Ploch.*`
`PackageVersion` carries a prerelease suffix. Run it yourself at any time:

```bash
dotnet msbuild src/Spectre/CommandLine.Spectre/Ploch.CommandLine.Spectre.csproj -t:VerifyPlochPackageVersionsAreStable
```

It is not attached to `Build` or `Restore`, so it costs nothing unless you ask for it. The target
is defined in [`Directory.Build.props`](Directory.Build.props).

### If you genuinely need unreleased Ploch code

Do not pin a prerelease. Build against the sibling sources instead:

```bash
dotnet build Ploch.CommandLine.Spectre.slnx -p:UsePlochProjectReferences=true
dotnet test  Ploch.CommandLine.Spectre.slnx -p:UsePlochProjectReferences=true
```

This swaps the `Ploch.Common*` `PackageReference`s for `ProjectReference`s into
`../ploch-common`, and needs no feed access at all. Two caveats:

- It **requires the `ploch-common` sibling checkout**. It is not a fix for a missing sibling.
- `ploch-common`'s `main` may be ahead of the released packages, so the two modes are not
  interchangeable — code that compiles in project-reference mode may not compile against the
  released packages, and vice versa. CI only exercises the default (package) mode for the main
  solution.

Never commit a change that only builds in project-reference mode.

## Commits, branches and pull requests

- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/).
- User-visible changes get a file in [`change-log/`](change-log/README.md); CI, test-only and
  refactoring changes do not.
- Tests use xUnit v3 with FluentAssertions and AutoFixture.
- Prose, comments and documentation are written in British English.

## Scope of this policy

The stable-only policy and the fork-restore hazard apply to every repository in the workspace
that consumes Ploch packages from the mapped GitHub Packages feed, not only this one. This
repository documents and enforces it for itself; propagating it is tracked separately.
