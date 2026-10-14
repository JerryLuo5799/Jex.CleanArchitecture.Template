# ADR-0005 Replace Taskfile with the jex command-line tool

**English** | [简体中文](ADR-0005-jex-cli.zh-CN.md)

- Status: Accepted (supersedes [ADR-0002](ADR-0002-taskfile.md))
- Date: 2026-10-12

## Context

Both the repository and generated projects depended on go-task: users had to install an extra third-party tool, and the generated project's Taskfile hard-coded the solution name, API path and database profile through template placeholders, so it no longer matched the project after a rename or a database change.

## Decision

- Add a .NET global tool **`Jex.Cli`** (command name `jex`, `src/Jex.Cli`), released with the same version and the same tag as the template package. The generated project's commands (`jex check`, `jex scaffold …`, `jex db …`, `jex infra …`) and the repository's dev tasks (`jex dev …`) all go through it; `Taskfile.yml` is removed from both the repository and generated projects.
- **Projects are identified by a manifest** (as in Angular, the Aspire CLI and ABP Studio): the root of a generated project has a `jex.json`, and jex searches upward from the current directory. The manifest only records facts that cannot drift: the marker and the **template version at generation time** (`StampJexManifest` writes the package version at pack time; the source file stays at `0.0.0-dev`). The solution and the API / AppHost / test projects are derived from the project references in `*.slnx`, and the database profile is derived from `appsettings.json` only when `jex infra` needs it. After a rename, jex follows the slnx project references; when it cannot derive something it fails and suggests `--api-project` / `--solution-name`.
- **Two version policies**: `dotnet new install` keeps only one version per machine, so only `jex new` installs or switches the template package (target: `--template-version`, defaulting to the CLI version). `jex scaffold` never installs; it only compares the major version in `jex.json` with the installed templates, and when they differ it fails with the command to switch or `--force`.
- Commands only build arguments and hand them to a process runner that bypasses the shell (`ProcessStartInfo.ArgumentList`); file system work is done in C#. A multi-step command stops at the first failing step and returns that step's exit code.
- `jex dev check` / `jex dev new <Name> --check` reuse the `check` implementation in the current process; the matrix script is a separate process that receives the CLI under test explicitly through `--jex <exe>` and `--jex-arg <arg>` and never looks it up on PATH.
- The structure assertion and matrix scripts stay file-based apps ([ADR-0004](ADR-0004-file-based-scripts.md)), wrapped by `jex dev`.

## Alternatives

- Keep go-task: an extra dependency, and the Taskfile drifts away from the project.
- A `dotnet` local tool manifest (`.config/dotnet-tools.json`): a generated project cannot restore it before it has a package source, and it cannot reference an unpublished version; a global tool matches how users already work.
- Record the solution name and project paths in jex.json: that information goes stale with refactoring.

## Consequences

- Users only need `dotnet tool install -g Jex.Cli`; environments without jex can still work with the native commands in section 10 of `AGENTS.md`.
- The tool package name is written as the placeholder `TPLTOOL` in the template source (restored to `Jex.Cli` on generation); otherwise `NameSafe` would rename `Jex.Cli` to `<project name>.Cli`.
- The publish job pushes two packages; before the first release, make sure the Trusted Publishing policy on nuget.org allows creating `Jex.Cli`.
- Projects generated before jex existed can run `jex init` at the root to add a `jex.json` (the version is unknown, so scaffolding needs `--template-version` or `--force`).
