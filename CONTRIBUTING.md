# Contributing

**English** | [简体中文](CONTRIBUTING.zh-CN.md)

## Repository layout

```
Jex.CleanArchitecture.Template.csproj   Template packaging project (packs only templates/**, stamps the version into jex.json)
Jex.CleanArchitecture.slnx              Packaging project + jex CLI + CLI tests (every command names it or a specific project)
src/Jex.Cli/                            The jex command-line tool (NuGet package Jex.Cli, same version as the template package)
src/Jex.Cli.Tests/                      jex unit tests (xunit.v3 + MTP)
scripts/assert-structure.cs             Structure assertions on generated output (.NET 10 file-based app, called by jex dev new)
scripts/test-template-matrix.cs         Combination matrix test (called by jex dev matrix)
templates/solution/                     Content of `dotnet new jex` (itself a buildable, testable jex project)
templates/items/{entity,usecase,endpoint}/   Item templates
docs/                                   Documentation and ADRs for this repository
```

`templates/solution` is a real solution: the template source compiles and tests as the **SQLite, no Aspire** combination; the other combinations are verified by the matrix test.

## Dev loop

The first time you work in this repository, install the jex from source as a global tool (run it again whenever you change `src/Jex.Cli`):

```bash
dotnet run --project src/Jex.Cli -- dev install-cli
```

1. Change `templates/solution`, `templates/items` or `src/Jex.Cli`.
2. `jex dev check --ci`: formatting, strict build and tests of the template source. If you changed the CLI, also run `dotnet build Jex.CleanArchitecture.slnx -c Release -p:ContinuousIntegrationBuild=true` and `dotnet test --project src/Jex.Cli.Tests/Jex.Cli.Tests.csproj`.
3. `jex dev install` → `jex dev new Acme --database postgresql --check`: installs the templates from the current directory, generates into `artifacts/sandbox/Acme`, asserts the structure and runs `check --ci`.
4. When a change affects several combinations, run `jex dev matrix` (or `jex dev matrix --cells sqlite,bare`).
5. Update `[Unreleased]` in `CHANGELOG.md` and `CHANGELOG.zh-CN.md`.

### The jex dev loop

- **Iterating on the CLI source**: `dotnet run --project src/Jex.Cli -- dev new Acme --check`. `--check` runs `check --ci` in the same process, so it verifies the current source, not the globally installed jex.
- **Reinstalling the global jex**: `dotnet run --project src/Jex.Cli -- dev install-cli`. It packs `0.0.0-dev`, uninstalls the old tool, deletes `jex.cli/0.0.0-dev` from the NuGet cache (the version does not change, so without this the old package would be installed again), then installs with `--source artifacts`. Do not use an installed `jex dev install-cli` to update itself (on Windows a running tool is locked, and jex refuses).
- **Which jex the matrix tests**: the matrix script never looks for jex on PATH; `jex dev matrix` passes it how to start itself (`--jex <exe>`, plus `--jex-arg <dll>` when started as `dotnet jex.dll`). Running `jex dev matrix` with the global jex tests the global jex; `dotnet run --project src/Jex.Cli -- dev matrix --cells sqlite,bare` tests the source.
- **Version**: a locally built CLI is `0.0.0-dev` (dev mode: `jex new` only uses the dev templates installed by `jex dev install` and never installs from the network).

## Template authoring rules

- **Renaming**: the solution template does not use `sourceName`. `NameSafe` (the safe_namespace form of `--name`) replaces `Jex` in both file names and content, so a name like `my-api` gives matching directories and namespaces (both become `my_api`).
- **Lowercase `jex`** is only for jex commands (`jex check`, `jex scaffold ...`) and `jex.json`. Refer to item template short names as `TPLPREFIX-usecase`, which becomes `jex-usecase` after generation. Other lowercase names use `TPLNAMELOWER` (the kebab-case project name).
- **Write the jex NuGet package name as `TPLTOOL`** (`dotnet tool install -g TPLTOOL`); it becomes `Jex.Cli` after generation. Writing `Jex.Cli` directly would be renamed by `NameSafe` to `<project name>.Cli`.
- **Placeholders**: `TPLPREFIX`, `TPLTOOL`, `TPLNAMELOWER`, `DB_PROFILE`, `FREESQL_PROVIDER`, `TPLCONN_PREFIX`, `TPLCONN_SUFFIX`. When you add a placeholder, also add it to the leftover check in `scripts/assert-structure.cs`.
- **jex commands in docs must run**: `DocsCommandTests` in `Jex.Cli.Tests` uses the real command parser to check every inline code span and code block command starting with jex in `templates/solution` and in this repository's docs, plus the jex commands in `.claude/settings.json` and `.vscode/tasks.json` (a reference by command name only has to exist; one with arguments must parse completely).
- **Ports**: only write `localhost:5208` / `localhost:7025`; the template replaces them with random ports. Inside containers the port is always 8080.
- **Conditions**: C# uses template conditions such as `#if (UseSqlite)`. **Do not use a native `#if DEBUG`** (the template engine evaluates it as a condition and removes it); if you really need one, wrap it in `//-:cnd:noEmit` / `//+:cnd:noEmit`. JSON uses `//#if`, XML/slnx/Markdown use `<!--#if -->`, YAML/Dockerfile/.env/SQL use `#if` (see `SpecialCustomOperations`).
- **Content only for the template source** goes inside a `TemplateSourceOnly` condition (always false on generation), for example the `DefineConstants` that make the template source compile as SQLite.
- **Third-party namespaces go in each project's `GlobalUsings.cs`**; source files only `using` `System.*` and project namespaces. Otherwise the import order changes with the project name (`Acme` and `my_api` sort differently) and `dotnet format --verify-no-changes` fails in the generated project.
- **Do not put this repository's GitHub URL in template content** (it contains `Jex` and would be renamed).
- The conventions of the generated project live only in `templates/solution/AGENTS.md`; the other AI entry files only point to it.

## Adding a template parameter

1. Add the `parameter` to `templates/solution/.template.config/template.json` (plus any `computed`/`generated` symbols and `modifiers` it needs).
2. Add the long and short option names to `dotnetcli.host.json` (avoid the ones `dotnet new` already uses, such as `--type`, `--name`, `--output`).
3. Use it in the content through conditions or placeholders.
4. Add the matching assertions to `scripts/assert-structure.cs`; add matrix cells to `scripts/test-template-matrix.cs` if needed and keep `.github/workflows/template-tests.yml` in sync.
5. Update `docs/template-parameters.md` / `docs/template-parameters.zh-CN.md` and the option tables in `README.md` and `README.zh-CN.md`.

## Releasing

The template package `Jex.CleanArchitecture.Template` and the tool package `Jex.Cli` are released together with the same version and the same tag (`jex new` installs the templates with its own version by default). Releases are started by hand; merging to main only runs CI.

1. Add a `## [x.y.z]` section to `CHANGELOG.md` and `CHANGELOG.zh-CN.md` and merge it to main.
2. On GitHub, open Actions → **release** → **Run workflow**, keep the branch on `main` and enter the version `x.y.z` (no `v`). `release.yml` then validates the version (semver, run from main, tag `vx.y.z` does not exist yet) → runs the strict check and packs both packages with `dotnet pack Jex.CleanArchitecture.slnx -p:Version=x.y.z` → installs jex and the templates from those packages and runs the full matrix → checks the CHANGELOG → publishes both nupkgs through NuGet Trusted Publishing → creates the tag `vx.y.z` on the built commit and the GitHub Release. A pre-release version (`x.y.z-rc.1`) is marked as a pre-release and does not become Latest.
3. Before the first release, configure a Trusted Publishing policy on nuget.org for this repository and `release.yml` (covering both package ids, `Jex.CleanArchitecture.Template` and `Jex.Cli`; before `Jex.Cli` is first published, make sure the policy allows creating a new package), and set the repository variable `NUGET_USER`. The `publish` job uses an environment named `nuget`, where you can add a manual approval.
