# Contributing to Ploch.CommandLine

Thanks for considering a contribution. This file covers what you need to build the repository,
and one policy — **Ploch dependencies are always stable** — that exists so that a pull request
from a fork is not *guaranteed* to fail. It does not buy full parity with a run in this
repository: those have a GitHub Packages token and a fork never can, so a tokenless restore stays
best-effort (see below). What the policy removes is the deterministic failure.

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
keep the build clean rather than suppressing it. CI enforces the same bar: scanner-enabled runs
build twice, because the SonarScanner for .NET switches warnings-as-errors off for the build it
wraps, so a plain Release build before the scanner is the gate; fork and Dependabot runs build
once without the scanner. Only the NuGet audit advisories `NU1901` and `NU1902` stay warnings.

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

**Leave the variable unset; never set it to an empty string.** An empty value is substituted as
the feed password, and NuGet on Linux then fails outright — `Value cannot be null or empty
string. (Parameter 'password')` — in both `dotnet tool restore` and `dotnet restore`, with no
fallback to nuget.org. CI exports the variable only when the run has a token for the same reason.

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
   forks, so no token is available and restore fails hard — with
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

## The public API baseline

Every packable project under `src/Spectre/` carries two files next to its project file,
`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, checked by
[Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md),
which `Directory.Build.props` references for every non-test project. The files list every
externally visible type and member in the analyser's notation, and they are the permanent form
of the one-off surface diff that proved PLO-779 removed only the intended members before the 1.0
freeze (PLO-788). The build fails when the assembly and the files disagree:

- `RS0016`: a public type or member exists that the files do not list.
- `RS0017`: the files list something that no longer exists.

Both run at error severity through the repository's `.globalconfig` rather than relying on
`TreatWarningsAsErrors`: the SonarScanner step in CI switches warnings-as-errors off for the build
it wraps (PLO-823), and `RS0017` is reported inside the baseline file, which an `.editorconfig`
section cannot reach. The SDK discovers `.globalconfig` on its own; do not add it as an item.

How the files are maintained:

- **Adding public API.** Let the analyser's code fix write the entry rather than typing it:
  `dotnet format analyzers <project>.csproj --diagnostics RS0016 --severity warn` (run it per
  project; the solution-level form only processes the first project), or use the "Add to public
  API" light bulb in the IDE. Review the diff like any other change: the entry *is* the API review.
- **Removing or renaming unreleased API.** Delete its line from `PublicAPI.Unshipped.txt` (and
  add the new line for a rename).
- **Until 1.0 ships** `PublicAPI.Shipped.txt` holds only the `#nullable enable` header and every
  entry lives in `PublicAPI.Unshipped.txt`. Before `release.yml` is dispatched, a reviewed commit
  moves every entry from `Unshipped` to `Shipped`, leaving `Unshipped` with the header alone;
  `release.yml` checks this before it restores and refuses to release (or dry-run) otherwise.
- **After 1.0** removing or changing a shipped member is a breaking change. Keep its line in
  `Shipped`, add the same line to `Unshipped` prefixed with `*REMOVED*`, and add the new
  signature as an ordinary entry; the next release promotion deletes the removed line from
  `Shipped` and drops the marker.
- Both files start with `#nullable enable`, so nullability is part of the recorded signature; a
  `~` prefix marks an entry whose nullability is not annotated.

Two companion rules, `RS0026` and `RS0027`, object to a method having several public overloads
with optional parameters. The output pipeline in `src/Spectre/CommandLine.Spectre/Output/` pairs a
typed overload with an `object?` / `FormattableString?` one on purpose, so for that folder alone
`.editorconfig` lowers the two rules to a suggestion; whether to keep that shape before 1.0 is
tracked separately (see the comment in `.editorconfig`).

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
