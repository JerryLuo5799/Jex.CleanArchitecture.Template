# Getting started

**English** | [简体中文](getting-started.zh-CN.md)

## Prerequisites

- .NET 10 SDK (`dotnet --version` ≥ 10.0.100)
- Optional: Docker (local containers for databases other than SQLite, `TEST_DATABASE=provider` integration tests, image builds)

## 1. Install jex

```bash
dotnet tool install -g Jex.Cli
jex --version
```

`jex` is this template's command-line tool. Every `jex` command has an equivalent `dotnet` command (section 10 of the generated project's `AGENTS.md`), so you can develop without jex.

## 2. Generate a project

```bash
jex new Acme.Shop
cd Acme.Shop
```

`jex new` first makes sure the template package with the same version as jex is installed (`dotnet new install Jex.CleanArchitecture.Template@<version>`), then runs `dotnet new jex -n Acme.Shop`. Without jex:

```bash
dotnet new install Jex.CleanArchitecture.Template
dotnet new list jex      # jex / jex-entity / jex-usecase / jex-endpoint
dotnet new jex -n Acme.Shop
```

The root of the generated project contains `jex.json` (the jex project marker, recording the template version it was generated with). Commit it along with the rest.

## 3. Run

```bash
jex check      # acceptance: format + build + test
jex run        # start; open /scalar in the browser
```

In Development the tables are created automatically and `admin@example.com / Admin123!` is seeded. Log in with `src/Acme.Shop.WebAPI/Acme.Shop.WebAPI.http` and then call the endpoints.

For databases other than SQLite, start the container first: `jex infra up` (or, when generated with `--use-aspire`, use `jex run --aspire`).

## 4. Start developing with AI

Open the project directory in Claude Code / Copilot / Cursor; they read `AGENTS.md`. For example, in Claude Code:

```
/new-feature Order Orders with CustomerName (required, ≤100), Total (decimal > 0)
```

Following the workflow in `AGENTS.md`, it runs the scaffolding, fills in the code, adds tests, generates a migration script and runs `jex check`. See [ai-agents.md](ai-agents.md).

## 5. Before going to production

- Set secrets such as `Jwt:Secret` (≥ 32 characters) through environment variables or `dotnet user-secrets`; never put them in `appsettings.json`.
- In production set `Database:AutoSyncStructure=false`; generate the initial migration with `jex db script initial` and commit it, and have the deployment pipeline run `dotnet <App>.WebAPI.dll migrate`.
- Configure `Cors:AllowedOrigins` and the rate limits. If you need an initial administrator, temporarily enable `Seed:Admin:Enabled` and supply the credentials through environment variables.

## Upgrading jex and the templates

```bash
dotnet tool update -g Jex.Cli
```

The next `jex new` switches to the template package of the same version automatically. `jex scaffold` in an existing project never installs or upgrades templates: it compares the template version in `jex.json` with the templates installed on the machine, refuses to run when the major versions differ, and suggests `dotnet new install Jex.CleanArchitecture.Template@<version> --force` (switch the machine's templates) or `--force` (continue with the installed templates).
