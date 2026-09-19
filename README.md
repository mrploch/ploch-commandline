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