# Ploch.CommandLine.Spectre

## Overview

**Ploch CommandLine Applications** is an opinionated library for building console applications in .NET Core. It builds on top of
the [Spectre.Console](https://spectreconsole.net/) library, providing a few additional features and prescribed ways of doing things.

## Features

[Spectre.Console](https://spectreconsole.net/) provides a rich set of features for building console applications. It includes composing command line interfaces,
rich UI elements, and more.

**Ploch.CommandLine.Spectre** builds on top of that and provides a few additional features and prescribed ways of doing things.

It includes:

- App construction and configuration, including service registrations
- Unified command interfaces providing opinionated way of organizing your app
- Command validation
- Command execution
- Exception handling
- Logging
- Configuration
- Output formatting

## Getting Started

TODO, GitHub Issue: [Ploch.CommandLine.Spectre Getting Started documentation #4
](https://github.com/mrploch/ploch-commandline/issues/4)

## Dependency Injection

`Ploch.CommandLine.Spectre` uses the `Microsoft.Extensions.DependencyInjection` library for dependency injection. It also relies on
`Ploch.Common.DependencyInjection` package which defines a **Services Bundle** concept - a class that groups related service registrations.

### Services Bundles

This project provides following services bundles:

- `AppServicesBundle` - registers services required for the app to run
- `OutputServicesBundle` - registers services required for output formatting


