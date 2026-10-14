# ADR-0001 The templates/ folder layout

**English** | [简体中文](ADR-0001-templates-folder-layout.zh-CN.md)

- Status: Accepted
- Date: 2026-10-12

## Context

The old layout kept the template content (src, tests, slnx) and the packaging project at the repository root: the template's own README was packed into generated projects, dot files such as `.github` and `.gitignore` were not packed, and `sourceName` also rewrote repository-level file names.

## Decision

- The repository root holds only tooling: the packaging project, the `jex` command-line tool (`src/`), scripts, CI and repository docs.
- `templates/solution/` is the solution template and is itself a buildable, testable solution; its `README.md` is the README of the generated project.
- `templates/items/{entity,usecase,endpoint}/` are the item templates.
- The packaging project uses `templates/**` as content, with `NoDefaultExcludes=true` to keep dot files; one nupkg contains the 4 templates.
- During development `dotnet new install ./templates` installs all templates from the folder (`jex dev install`); release acceptance goes through the nupkg (`jex dev install --from-package`, the CI matrix).

## Consequences

- Changes to the template source can be checked directly with `jex dev check --ci`; the generated output is verified by the matrix.
- Uninstalling: a folder install is removed by path (`dotnet new uninstall ./templates`), a package install by PackageId.
