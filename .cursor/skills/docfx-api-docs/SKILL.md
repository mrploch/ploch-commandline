---
name: docfx-api-docs
description: |
  Build, deploy, and maintain DocFX-generated API documentation sites for MrPloch .NET libraries.
  Use when: (1) adding docs to a repo that has or needs a DocumentationSite/docfx_project folder,
  (2) updating an existing DocFX site (template, TOC, namespace overwrites, branding),
  (3) debugging DocFX build/deploy failures, (4) integrating existing docs/ folder markdown
  into the DocFX site, (5) bumping DocFX or aligning a repo with the mrploch DocFX conventions,
  (6) switching the deploy action or GitHub Pages target.
  Triggers on: DocFX, docfx.json, DocumentationSite, docfx_project, API docs, github.ploch.dev,
  github-pages for a .NET library, dotnet tool docfx, API reference site.
---

# DocFX API Docs for MrPloch

Canonical DocFX setup and conventions for MrPloch repositories (ploch-common, ploch-data, ploch-lists, etc.). This
skill captures "how we do DocFX" so it can be improved over time instead of re-derived each time.

## Context: the MrPloch baseline

| Aspect | Convention |
|---|---|
| Folder | `DocumentationSite/` at repo root (ploch-common style) or `docfx_project/` (ploch-data style). Prefer `DocumentationSite/`. |
| Carrier project | **None.** Do *not* add a placeholder `.csproj` — DocFX runs without one. The dummy `Program.cs`/Console app pattern in older repos is legacy and should be removed. |
| Config file | `DocumentationSite/docfx.json` |
| Output | `DocumentationSite/_site/` (always `.gitignore`'d) |
| Hosting | GitHub Pages, served at `https://github.ploch.dev/<repo-name>/` via DNS CNAME |
| Deploy action | `actions/deploy-pages@v4` (official) — preferred over `peaceiris/actions-gh-pages` |
| Trigger | Push to default branch OR `workflow_dispatch`. Deploy job needs `pages: write` + `id-token: write`. |
| Tool version | Pin in `.config/dotnet-tools.json` alongside `nbgv`. Never use `-g` global install in CI. |

## Baseline `docfx.json`

Use this as the starting point. Adapt `_appName` / paths per repo.

```json
{
  "metadata": [
    {
      "src": [
        {
          "src": "../src",
          "files": ["**/*.csproj"],
          "exclude": ["**/*Tests/**", "**/obj/**"]
        }
      ],
      "dest": "api",
      "memberLayout": "separatePages",
      "namespaceLayout": "nested",
      "includePrivateMembers": false,
      "disableGitFeatures": false,
      "properties": { "TargetFramework": "net9.0" }
    }
  ],
  "build": {
    "content": [
      { "files": ["api/**.yml", "api/index.md"] },
      { "files": ["articles/**.md", "articles/**/toc.yml", "toc.yml", "*.md"] },
      { "files": ["../docs/**/*.md", "../docs/**/toc.yml"] }
    ],
    "resource": [{ "files": ["images/**"] }],
    "overwrite": [
      {
        "files": ["apidoc/**.md", "namespaces/**.md"],
        "exclude": ["obj/**", "_site/**"]
      }
    ],
    "dest": "_site",
    "globalMetadata": {
      "_appName": "Ploch.Common",
      "_appTitle": "Ploch.Common — .NET Utility Libraries",
      "_appLogoPath": "images/logo.png",
      "_appFaviconPath": "images/favicon.ico",
      "_enableSearch": true,
      "_disableContribution": false,
      "_gitContribute": {
        "repo": "https://github.com/mrploch/<repo-name>",
        "branch": "master"
      }
    },
    "template": ["default", "modern"],
    "markdownEngineName": "markdig"
  }
}
```

Key decisions encoded above:

- **`template: ["default", "modern"]`** — modern template layers on top of default. Enables dark mode, `expanded: true` TOC nodes, and better nav.
- **`memberLayout: separatePages`** — each member gets its own URL (easier xref, better SEO, slower build/more HTML files).
- **`namespaceLayout: nested`** — namespaces nest in the sidebar.
- **`properties.TargetFramework`** — pick one TFM for metadata extraction. Multi-targeting generates one set per TFM, which is rarely what you want for docs and slows the build.
- **Do not glob `../**.md`** — that pulls in `CLAUDE.md`, `GEMINI.md`, `AGENTS.md`, `TODO.md`, etc. Be explicit.
- **Include `../docs/**` explicitly** so repo-root `docs/` folder content is part of the site.

## TOC conventions

Top-level `DocumentationSite/toc.yml`:

```yaml
- name: Home
  href: index.md
- name: Getting Started
  href: ../docs/GETTING_STARTED.md
- name: Libraries
  href: ../docs/
  homepage: ../docs/INDEX.md
- name: Articles
  href: articles/
- name: API
  href: api/
  homepage: api/index.md
- name: GitHub
  href: https://github.com/mrploch/<repo-name>
```

Per-subfolder `toc.yml` for `../docs/libraries/`:

```yaml
- name: Core
  items:
    - name: Ploch.Common
      href: common.md
    - name: Ploch.Common.Net9
      href: common-net9.md
- name: Serialization
  items:
    - name: Ploch.Common.Serialization
      href: common-serialization.md
    # ...
- name: Testing Support
  items:
    - name: Ploch.TestingSupport.XUnit3
      href: testing-support-xunit3.md
```

## Namespace & type overwrite pattern

Drop markdown files in `namespaces/` to enrich auto-generated API landing pages. Each file references the target
by UID and contributes the `summary` (and optionally `remarks`, `example`).

```markdown
---
uid: Ploch.Common.Collections
summary: *content
---

## Overview

`Ploch.Common.Collections` provides LINQ extensions and guard-clause helpers that fill common gaps in
`IEnumerable<T>`, `ICollection<T>`, and `IQueryable<T>`.

### Highlights

- `None(predicate)` — inverse of `Any(predicate)`
- `JoinWithFinalSeparator(sep, final)` — Oxford-comma-friendly joining
- `If(condition, action)` — chainable conditional filtering

See also: <xref:Ploch.Common.ArgumentChecking> for parameter validation.
```

Rules:
- One overwrite file per namespace or type you want to enrich.
- `summary: *content` means "use the H1/first-paragraph as summary on the API landing page."
- Prefer `<xref:UID>` over markdown links — validated at build time.

## GitHub Actions deploy workflow

Place at `.github/workflows/publish-docs.yml`. Replaces any older inline `peaceiris/actions-gh-pages` step in the
main build workflow.

```yaml
name: Publish Docs
on:
  push:
    branches: [master] # or main
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

jobs:
  publish-docs:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0 # NBGV needs full history

      - name: Clone mrploch-development (shared build config)
        run: git clone --depth 1 https://github.com/mrploch/mrploch-development.git ../mrploch-development

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      - run: dotnet tool restore # DocFX is pinned in .config/dotnet-tools.json
      - run: dotnet restore Ploch.<Name>.slnx
      - run: dotnet docfx DocumentationSite/docfx.json --warningsAsErrors

      - uses: actions/upload-pages-artifact@v3
        with:
          path: DocumentationSite/_site
      - id: deployment
        uses: actions/deploy-pages@v4
```

## `.config/dotnet-tools.json` entry

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "nbgv": { "version": "3.7.115", "commands": ["nbgv"] },
    "docfx": { "version": "2.78.3", "commands": ["docfx"] }
  }
}
```

Update the DocFX version via PR (treat it like any other dependency bump). Never install globally with `-g`.

## Local preview

```powershell
# From repo root
dotnet tool restore
dotnet docfx DocumentationSite/docfx.json --serve
# Navigate to http://localhost:8080
```

Or, for a rebuild-on-save loop, run `dotnet docfx DocumentationSite/docfx.json --serve -n http://localhost:8080`
and edit markdown in another terminal — DocFX watches content but **not** `docfx.json`; restart if you change config.

## Cleanup script

`DocumentationSite/Clean-DocFx-Common.ps1` (or equivalent):

```powershell
cd $PSScriptRoot
Remove-Item _site, obj -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item api/*.yml -Exclude toc.yml -Force -ErrorAction SilentlyContinue
Remove-Item api/.manifest -Force -ErrorAction SilentlyContinue
```

## Known gotchas

1. **DocFX on .NET 10 (late-2025 / 2026):** ploch-data workflow contains the note *"DocumentationSite is currently
   excluded from the solution (docfx incompatible with .NET 10)"*. Re-verify against the DocFX release you pin
   before adopting net10 targets in a DocumentationSite.
2. **`properties.TargetFramework` is required** when the source projects multi-target — otherwise DocFX runs the
   metadata extractor once per TFM and you get duplicated/inconsistent YAML.
3. **`../**.md` is dangerous** as a content glob. The repo root now contains `CLAUDE.md`, `GEMINI.md`, `AGENTS.md`,
   `TODO.md`, etc. List explicit includes or use a narrow pattern like `../README.md`.
4. **`"exclude": ["../**Tests/**"]`** — DocFX is case-sensitive on Linux (CI). Use `**/*Tests/**` with a wildcard
   between directory separators.
5. **`cp ./README.md ./DocumentationSite/index.md` in CI is a smell.** It means the site's homepage is a
   build-time side effect. Prefer a committed `index.md` that imports README content via `[!INCLUDE]` or a
   DocFX `globalMetadata._appTitle` + short intro.
6. **Orphan deploy workflows:** repos often have both `build-dotnet.yml` (which includes an inline deploy step)
   and `publish-docs.yml`. Decide which one deploys and remove the redundancy — two deploy paths diverge over
   time.
7. **Empty `overwrite` globs waste inspection time.** If `apidoc/` is empty, drop the glob from `docfx.json`.
8. **Generated `api/*.yml` should be in `.gitignore`.** Do not commit them — they churn on every source change.

## Improvement backlog (what we learn as we go)

- [ ] Evaluate DocFX PDF output (`docfx pdf`) for offline-docs use cases.
- [ ] Try the `statictoc` template variant for very large APIs.
- [ ] Add a repo-local filter YAML to hide `JetBrainsAnnotations.cs`-style internals.
- [ ] Add `xrefmap.yml` publishing so other MrPloch repos can cross-reference into this one's API.
- [ ] Consider Algolia DocSearch integration for hosted search.
- [ ] Standardise a shared `mrploch-development/docfx-shared/` with common templates and globalMetadata.
- [ ] Track DocFX .NET 10 compatibility and document the fix once available.

## Anti-patterns to avoid

- Installing DocFX globally in CI (`dotnet tool update -g docfx`).
- A placeholder Console `.csproj` in `DocumentationSite/` — adds build time and confusion.
- Two deploy jobs in two workflows both targeting GitHub Pages.
- Copy-pasting `docfx.json` from another repo without adjusting `_appName`, `_gitContribute.repo`, and the content globs.
- Referencing markdown pages in `toc.yml` that don't exist (DocFX logs warnings but the site still builds).
- Committing `_site/`, `api/*.yml`, or `obj/` from the DocumentationSite folder.
