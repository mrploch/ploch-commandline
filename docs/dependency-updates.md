# Dependency updates

How NuGet and GitHub Actions dependencies of this repository are kept current, and
why the two ecosystems are handled by two different mechanisms.

## Summary

| Ecosystem | Handled by | Why |
|---|---|---|
| GitHub Actions | Dependabot (`.github/dependabot.yml`) | Dependabot parses the workflow YAML directly; nothing outside this repository is needed. |
| NuGet | The [`Dependency report`](../.github/workflows/dependency-report.yml) workflow | Dependabot cannot evaluate this repository's MSBuild graph, because that graph spans two repositories. |

## Why Dependabot cannot update NuGet packages here

`Directory.Packages.props` imports six shared version files from the **sibling
repository** `mrploch/mrploch-development`:

```xml
<Import Project="../mrploch-development/dependencies/MicrosoftExtensions.Net9.Packages.props" />
```

That relative path resolves only when both repositories are cloned next to each other,
which is the workspace layout every `mrploch` repository assumes and which
`build-dotnet.yml` reproduces by cloning the sibling before it restores.

Dependabot checks out **one** repository and offers no mechanism to fetch another
alongside it. MSBuild evaluation therefore fails before a single package is examined:

```text
Directory.Packages.props(5,3): error MSB4019: The imported project
"/home/dependabot/dependabot-updater/mrploch-development/dependencies/MicrosoftExtensions.Net9.Packages.props"
was not found.
updater | ERROR Error type: dependency_file_not_found
```

Every scheduled `nuget` run has failed this way since the shared imports were
introduced — verified across runs on 2026-08-28, 2026-09-01, 2026-09-08, 2026-09-15 and
2026-09-19, with no successful run in between. Consequently no NuGet update pull
request — including a security update — has ever been raised, and the only signal that a
dependency carries a known vulnerability was an `NU1902` restore warning that nothing
was watching for.

The `github-actions` ecosystem is unaffected: it reads the workflow files as YAML and
never evaluates MSBuild. It stays enabled.

## Options considered

### 1. Make the imports conditional, with local fallback versions

`Condition="Exists(...)"` on each `Import`, plus a locally pinned `PackageVersion` for
every id the shared files provide.

**Rejected.** Dependabot would evaluate successfully and then update the *fallback*
pins — values that no real build ever uses, because in the workspace and in CI the
shared file is present and wins. The repository would accumulate a second, silently
diverging set of versions, and a green Dependabot pull request would assert an upgrade
that had not actually happened. Trading a loud failure for a quiet lie is the wrong
trade.

### 2. Move the `nuget` ecosystem to `mrploch-development`

The shared versions are owned centrally, so updating them centrally is the natural
shape. This is what [PLO-565](https://linear.app/ploch/issue/PLO-565) proposed.

**Rejected as a complete answer, kept as a follow-up.** Two things block it today:

- `mrploch/mrploch-development` has **no** `.github/dependabot.yml`, and its
  `dependencies/*.Packages.props` files are not in a layout Dependabot's `nuget`
  ecosystem can discover. Dependabot anchors NuGet discovery on solution and project
  files and on `Directory.Packages.props` / `Directory.Build.props`; a file named
  `dependencies/Ploch.Packages.props`, imported by nothing inside its own repository,
  is none of those. Several of its versions are also indirected through MSBuild
  properties (`$(PlochCommonPackagesVersion)`), which Dependabot does not rewrite.
  Enabling the ecosystem there is real work, not a configuration line.
- Even once that is done, it would not cover this repository. Two package versions are
  pinned **locally** in `Directory.Packages.props` — `FluentValidation.DependencyInjectionExtensions`
  and `Nerdbank.GitVersioning` — and belong to this repository alone. Deleting the
  `nuget` ecosystem and pointing at `mrploch-development` would leave those permanently
  unwatched.

Centralising the *shared* versions remains worth doing, and is tracked as
[PLO-582](https://linear.app/ploch/issue/PLO-582) against `mrploch-development`. It is
complementary to, not a replacement for, the mechanism below.

### 3. A scheduled workflow that reproduces the workspace layout first

**Chosen.** The root cause is a missing sibling checkout, and a GitHub Actions workflow
can do the one thing Dependabot cannot: clone `mrploch-development` next to the
checkout, exactly as `build-dotnet.yml` already does, and then let the .NET SDK evaluate
the real, complete MSBuild graph.

[`.github/workflows/dependency-report.yml`](../.github/workflows/dependency-report.yml)
runs weekly (and on demand) and:

- clones the sibling, restores, and runs `dotnet list package --outdated` and
  `dotnet list package --vulnerable --include-transitive`;
- covers **both** solutions — the main one and `samples/SampleApp`, which is standalone
  by design, keeps its own `Directory.Packages.props`, and would otherwise be left
  entirely unwatched;
- writes both lists, deduplicated and labelled with where each version has to be changed,
  to the run's job summary **and** to the log — the summary is what a person reads, but
  only the log can be retrieved through the API afterwards;
- **fails the job** when a vulnerable package is found, and only then.

Three details are load-bearing and easy to undo by accident:

- **The restore downgrades `NU1903`/`NU1904` for this run only.** `Directory.Build.props`
  sets `TreatWarningsAsErrors` with `WarningsNotAsErrors` limited to `NU1901;NU1902`, so a
  high or critical audit advisory is fatal. That is correct for a build — such a finding
  should stop a release — but here it would abort restore before the report could name the
  package, turning the one case this workflow exists for into a bare "restore failed". The
  script still fails the run, with the actionable table attached.
- **`--include-transitive` is on the vulnerability query only.** Combined with `--outdated`
  it aborts the SDK on the sample solution (`error: Sequence contains no matching element`,
  while either flag alone succeeds). A transitive package cannot be bumped directly anyway,
  so nothing actionable is lost; a transitive *vulnerability* very much is, and is covered.
- **There is no `pull_request` trigger.** This is a health check, not a merge gate. Running
  it on pull requests would hand `GH_PACKAGES_TOKEN` to code the pull request itself
  controls, and would fail outright for forks, which receive no secret. Validate a change
  to the report by dispatching the workflow on the branch.

The severity split is deliberate. A merely outdated package is information, and failing
a scheduled job weekly for it trains everyone to ignore the notification — the precise
failure mode this change exists to fix, since four months of red Dependabot runs went
unnoticed. A *vulnerable* package is an action item, so it turns the run red and sends
the notification.

This reports rather than raises pull requests. That is the accepted cost: a bump has to
be applied by hand (in this repository for the local pins, in `mrploch-development` for
the shared ones). To make that concrete, every row carries a **"Bump in"** column,
resolved from the `Directory.Packages.props` MSBuild itself would apply to that solution
— the nearest one at or above it, so the sample's standalone pins are never confused with
the main solution's. A version pinned here but resolved through an MSBuild property names
that property rather than claiming a location: the chain can cross into a shared file
(`Ploch.Common.Apps.Shared` is pinned here as `$(PlochAppsSharedVersion)`, which defaults
to the shared `$(PlochCommonPackagesVersion)`), and the property name is the pointer
someone can actually act on.

## When the shared versions move to `mrploch-development`

If [PLO-582](https://linear.app/ploch/issue/PLO-582) makes automated updates work there,
this workflow stays useful and
should not be removed: it still covers the local pins and transitive dependencies, and
it is the only check that exercises the composed two-repository graph the build
actually uses.
