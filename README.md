# Ploch CommandLine Applications

[![NuGet](https://img.shields.io/nuget/v/Ploch.CommandLine.Spectre.svg)](https://www.nuget.org/packages/Ploch.CommandLine.Spectre)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ploch.CommandLine.Spectre.svg)](https://www.nuget.org/packages/Ploch.CommandLine.Spectre)
[![Build](https://github.com/mrploch/ploch-commandline/actions/workflows/build-dotnet.yml/badge.svg)](https://github.com/mrploch/ploch-commandline/actions/workflows/build-dotnet.yml)
[![Publish Docs](https://github.com/mrploch/ploch-commandline/actions/workflows/publish-docs.yml/badge.svg)](https://commandline.github.ploch.dev/)
[![Qodana](https://github.com/mrploch/ploch-commandline/actions/workflows/qodana_code_quality.yml/badge.svg)](https://github.com/mrploch/ploch-commandline/actions/workflows/qodana_code_quality.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=mrploch_ploch-commandline&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=mrploch_ploch-commandline)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=mrploch_ploch-commandline&metric=coverage)](https://sonarcloud.io/summary/new_code?id=mrploch_ploch-commandline)
[![Codacy Grade](https://app.codacy.com/project/badge/Grade/96834038202e40868af2cb827f145301)](https://app.codacy.com/gh/mrploch/ploch-commandline/dashboard)
[![Codacy Coverage](https://app.codacy.com/project/badge/Coverage/96834038202e40868af2cb827f145301)](https://app.codacy.com/gh/mrploch/ploch-commandline/dashboard)

## Overview

**Ploch CommandLine Applications** is an opinionated library for building console applications in .NET Core.
It builds on top of the [Spectre.Console](https://spectreconsole.net/) library, providing a few additional features:

- app construction, including service registrations
- unified command interfaces
- command validation
- command execution
- exception handling
- logging
- configuration
- output formatting

## Documentation

- [Getting started](docs/GETTING_STARTED.md) — building your first application with `AppBuilder`.
- [Dependency updates](docs/dependency-updates.md) — how NuGet and GitHub Actions
  dependencies are kept current, and why NuGet is handled by a scheduled workflow rather
  than by Dependabot.
- [Release notes](RELEASE_NOTES.md) — what changed in each version.

## Building this repository

Clone `mrploch-development` as a sibling directory first — `Directory.Packages.props` imports the
shared package versions from it, so restore fails without it:

```bash
git clone https://github.com/mrploch/ploch-commandline.git
git clone https://github.com/mrploch/mrploch-development.git
cd ploch-commandline
dotnet build Ploch.CommandLine.Spectre.slnx
dotnet test  Ploch.CommandLine.Spectre.slnx
```

**The main solution needs no GitHub Packages token.** It only ever references stable `Ploch.*`
versions, and those are on nuget.org, so a build with `GH_PACKAGES_TOKEN` unset normally restores
successfully — the `401 (Unauthorized)` warnings from the unauthenticated GitHub Packages feed are
expected. That "stable versions only" rule is a policy, enforced by a check in CI.

Tokenless restore is best-effort rather than guaranteed: the GitHub Packages source stays eligible
while unauthenticated, and NuGet can surface its failure as `NU1301` before nuget.org answers. If
you hit that, retry, or set a token. The
[sample application](samples/SampleApp/README.md) is a separate case — it pins a prerelease build
of this repository's own packages and does need a token in its default mode.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the reasoning, the escape hatch for working against
unreleased `ploch-common` code, and the rest of the contributor setup.
