# Changelog

**English** | [简体中文](CHANGELOG.zh-CN.md)

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [SemVer](https://semver.org/).

## [Unreleased]

### Changed

- This repository's docs (CONTRIBUTING, CHANGELOG, `docs/`, ADRs) are bilingual: English `xxx.md` plus Chinese `xxx.zh-CN.md`, linked to each other at the top; issue and PR templates contain both languages.
- Releases are started by hand (Actions → release → Run workflow with the version) instead of by pushing a `v*` tag; the workflow creates the tag on the commit it built.

## [3.0.1]

### Changed

- The READMEs default to English, with the Chinese version moved to `README.zh-CN.md`; each links to the other at the top. This applies to both the repository and generated projects.
- The `jex` assembly is renamed to `jex.dll` (the package id is still `Jex.Cli`), so help shows usage as `jex [command]` instead of `Jex.Cli [command]`.

### Fixed

- When `jex scaffold` hits an existing file it explains that jex never overwrites and what to do, instead of leaving only the `--force` hint from `dotnet new` (`jex scaffold --force` only skips the template version check, which the help now says too).
- The help for `jex scaffold endpoint` says that the Create/GetById endpoints and integration test are only generated with `--entity`.
- Generated project: when `jex db migrate` fails on a database whose tables are created automatically at development run time (`Database:AutoSyncStructure`), the error explains why and how to fix it (SQLite: run `jex db reset` first); README and AGENTS.md cover this too.

## [3.0.0]

Stable release containing all changes from [3.0.0-rc.1]. **Breaking change**: go-task is no longer used; install jex first: `dotnet tool install -g Jex.Cli`. If an older template version is installed on the machine, `jex new` switches to the matching template version automatically.

### Fixed

- GitHub Releases for pre-release versions (such as `3.0.0-rc.1`) are marked as Pre-release and no longer become the repository's Latest release.
- `jex new` installs the template package, and all hints refer to it, as `Jex.CleanArchitecture.Template@<version>` (the `::` separator is deprecated by the .NET SDK).

## [3.0.0-rc.1]

**Breaking change**: go-task is no longer used; install jex first: `dotnet tool install -g Jex.Cli`.

### Added

- The `Jex.Cli` global tool (command `jex`), released with the same version as the template package: `jex new` (installs or switches to the template package of the same version), `jex check [--ci]`, `jex build` / `test` / `format` / `run` / `watch`, `jex scaffold entity|usecase|endpoint|feature`, `jex db script|migrate|reset`, `jex infra up|down`, `jex init`; repository dev commands `jex dev install|uninstall|pack|new|matrix|install-cli|check|clean`.
- `jex.json` at the root of a generated project: the project marker for jex, recording the template version it was generated with (the package version is written in at pack time); scaffolding uses it to refuse templates from a different major version.

### Changed

- The generated project's `AGENTS.md`, README, `CLAUDE.md`, skills, Copilot instructions, `.claude/settings.json` permissions, `.vscode/tasks.json` and item template hints use `jex` commands.
- The matrix test scaffolds with the jex under test and runs `jex check --ci`; CI installs jex from the nupkg built in the same run and checks `jex --version`.
- The packaging project's version property is now `Version`; `dotnet pack Jex.CleanArchitecture.slnx -p:Version=x` produces both packages.

### Removed

- `Taskfile.yml` in the repository and in generated projects; `DocumentationTests` in generated projects (the jex commands in docs are now checked by `Jex.Cli.Tests` with the real parser).
- The upstream reference note in the README.

## [2.0.0]

Complete rewrite. Uninstall the old version before upgrading: `dotnet new uninstall Jex.CleanArchitecture.Template`.

### Added

- Template parameters: `--database sqlite|postgresql|sqlserver|mysql`, `--use-aspire`, `--include-ai-docs`, `--include-docker`, `--http-port`, `--https-port`, `--skip-restore`.
- Item templates: `jex-entity`, `jex-usecase`, `jex-endpoint`; their output passes `task check` straight away, and running them again refuses to overwrite.
- In generated projects: `AGENTS.md`, `CLAUDE.md`, `.claude/settings.json`, the `/new-feature` and `/review-architecture` skills, Copilot instructions, `.mcp.json`, 9 ADRs, `Taskfile.yml`.
- MediatR pipeline: logging, unhandled exceptions, authorization (`[AuthorizeRequest]`), validation, performance, unit of work; `ICommand` / `IQuery<T>`.
- FreeSql unit of work, repositories that write only dirty columns, audit fields, soft delete (`DeletedToken` keeps uniqueness among active rows only), domain events.
- DbUp versioned migrations: `migrate` and `db-script` commands (scripts generated against a separate baseline database).
- Minimal API `IEndpointGroup`, a uniform envelope (including 401/403/404/405/415/429/binding failures), Scalar, `openapi.json` at build time, CORS, rate limiting, health checks.
- ServiceDefaults (OpenTelemetry, service discovery, resilience) and an optional Aspire AppHost; Dockerfile and compose profiles.
- Sample feature slices TodoLists / TodoItems; unit, integration (including Testcontainers) and architecture tests.
- Repository: `templates/` layout, `Taskfile.yml`, structure assertion and matrix scripts, reusable CI workflows, Trusted Publishing releases.

### Changed

- Validation moved from Sannr to FluentValidation; Controllers replaced by Minimal API; the success business code changed from `2000` to `0`.
- Tests moved to xunit.v3 + Microsoft.Testing.Platform and Shouldly.
- The license is MIT throughout.

### Fixed

- Users could not log in after changing their email (`NormalizedEmail` was not updated); login did not check lockout or user status.
- Request logging recorded request bodies in plain text (including passwords).
- The first `dotnet run` threw because of the JWT placeholder.
- Paged `totalCount` was never set; validation error details were lost.
- Repository updates overwrote every column.
