# Dependency Management Standards

## Version Pinning

- **Always use fixed versions** (e.g., `"lodash": "4.17.21"`, not `"^4.17.21"`).
- Applies to all dependency files including overrides and resolutions.

## Upgrading Process

### Pre-Upgrade Investigation

- Read changelog/release notes between current and target versions.
- Identify breaking changes and their impact.
- Look for official migration guides and codemods.
- Check for CLI migration tools (e.g. `npx package-name migrate`).
- Note deprecated APIs and their replacements.

### Information Sources

- Official documentation site (highest priority).
- Repository `CHANGELOG.md`, GitHub releases.
- Migration guides at `/docs/migration`, `/MIGRATION.md`.
- Package README; community resources for complex migrations.

### Upgrade Execution

- Run automated tools first (codemods, CLI migrations).
- Update configuration files and type definitions.
- Update imports, API calls, deprecated methods.
- Run tests and fix breakages.
- Address linter/type errors.
- Manual verification of critical paths.

### Post-Upgrade

- Update README, spec, or mdc files that reference the package.
