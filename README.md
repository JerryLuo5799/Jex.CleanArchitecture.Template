# Jex.CleanArchitecture.Template

**English** | [简体中文](https://github.com/JerryLuo5799/Jex.CleanArchitecture.Template/blob/main/README.zh-CN.md)

A .NET 10 Web API template built for **AI-assisted development**. One command generates a project locally. The generated project has a clear layered architecture, consistent conventions, scaffolding commands and an `AGENTS.md`, so Claude Code, GitHub Copilot or Cursor can follow the rules as soon as they open it.

## Quick start

```bash
dotnet tool install -g Jex.Cli                    # install the jex CLI once
jex new Acme.Shop                                 # installs the matching template package; SQLite, runs out of the box
cd Acme.Shop
jex check                                         # format + build + all tests
jex run                                           # http://localhost:<random port>/scalar
```

On first run in Development the tables are created automatically and an administrator `admin@example.com / Admin123!` is seeded.

`jex new` first makes sure the template package `Jex.CleanArchitecture.Template` installed on this machine has **the same version** as jex (it runs `dotnet new install` when it is missing and switches it when the version differs), then calls `dotnet new jex`. Without jex you can run `dotnet new install Jex.CleanArchitecture.Template` + `dotnet new jex -n Acme.Shop` directly. To upgrade, run `dotnet tool update -g Jex.Cli`; the next `jex new` switches to the new templates.

Other combinations:

```bash
jex new Acme --database postgresql --use-aspire     # PostgreSQL + .NET Aspire orchestration
jex new Acme --include-ai-docs false --include-docker false
```

| Option | Short | Default | Description |
|---|---|---|---|
| `--database` | `-db` | `sqlite` | `sqlite` / `postgresql` / `sqlserver` / `mysql`: picks the FreeSql and DbUp providers, connection string and compose service |
| `--use-aspire` | `-aspire` | `false` | Generates `*.AppHost` (ServiceDefaults is always included) |
| `--include-ai-docs` | `-ai` | `true` | `AGENTS.md`, `CLAUDE.md`, `.claude/`, Copilot instructions, `.mcp.json` |
| `--include-docker` | `-docker` | `true` | `Dockerfile`, `docker-compose.yml`, `.env.example` |
| `--http-port` / `--https-port` | `-hp` / `-hsp` | random | Ports in launchSettings |
| `--skip-restore` | `-sr` | `false` | Skip the restore after generating |

`jex new` takes the same options as `dotnet new jex`, plus `--template-version <version>` (use that template version) and `--no-install` (never install or switch the template package; fail when it is missing). See [docs/template-parameters.md](docs/template-parameters.md) for details.

## What the generated project contains

- **Architecture**: Domain / Application / Infrastructure / WebAPI layers; the dependency rules are enforced by architecture tests.
- **CQRS**: MediatR 12 (`ICommand` / `IQuery<T>`) with pipeline behaviours: logging → unhandled exceptions → authorization (`[AuthorizeRequest]`) → FluentValidation → performance → unit of work.
- **Data**: FreeSql Code First; repositories write only dirty columns, audit fields are filled automatically, soft delete (unique among active rows only), domain events dispatched inside the transaction; production applies reviewed SQL migrations with DbUp.
- **API**: Minimal API `IEndpointGroup`, a uniform HTTP 200 + `{ requestId, code, msg, data, totalCount, errors }` envelope, Scalar UI, and an `openapi.json` generated at build time and committed.
- **Security and operations**: ASP.NET Core Identity + JWT, lockout and account status checks, CORS, rate limiting, health checks, NLog, OpenTelemetry (optional Aspire), Docker.
- **Tests**: xunit.v3 + MTP, Shouldly; unit, integration (SQLite or real databases via Testcontainers) and architecture tests; `TransactionRollbackTests` covers nested transactions and event rollback.
- **AI development**: `AGENTS.md` (the single source of conventions), `CLAUDE.md`, `/new-feature` and `/review-architecture` skills, path-scoped Copilot instructions, a narrow permission allowlist.
- **Scaffolding**: `jex scaffold feature Order Orders` generates the entity, repository, configuration, DTO, command, query, endpoints and tests in one go, and the result passes `jex check` straight away.

## jex commands

Run them from any subdirectory of a generated project (jex walks up to the `jex.json` at the root, which records the template version the project was generated with):

| Command | Purpose |
|---|---|
| `jex check` / `jex check --ci` | Acceptance gate: format + build + test; `--ci` is as strict as CI (format verify only, warnings as errors) |
| `jex build`, `jex test`, `jex test unit`, `jex format --check` | Build, test (optionally only `unit` / `integration` / `arch`), formatting |
| `jex run`, `jex run --aspire`, `jex watch` | Run the API, the Aspire AppHost, or with hot reload |
| `jex scaffold feature Order Orders` | Entity + `CreateOrder` + `GetOrderById` + endpoint group; stops at the first failing step |
| `jex scaffold entity Order Orders`, `jex scaffold usecase CancelOrder Orders --with-id`, `jex scaffold endpoint Reports` | Generate one piece at a time |
| `jex db script add-orders`, `jex db migrate`, `jex db reset` | Generate a migration script, apply migrations, delete the local SQLite database |
| `jex infra up`, `jex infra down` | Start/stop the database container for `Database:Provider` (SQLite needs none) |
| `jex init` | Add a `jex.json` to a project generated before jex existed |

Every command is a thin wrapper around `dotnet` / `docker`; section 10 of the generated project's `AGENTS.md` lists the equivalent native commands.

## Item templates

`jex scaffold` fills in `--solution-name` for you; you can also call the templates directly from the root of a generated project:

| Template | Generates | Example |
|---|---|---|
| `jex-entity` | Entity + repository interface/implementation + entity configuration + DTO + unit test | `dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .` |
| `jex-usecase` | Command (+Handler+Validator) or Query (+Handler) + unit test | `dotnet new jex-usecase -n CreateOrder --feature Orders --usecase-type command --return-type long --solution-name Acme -o .` |
| `jex-endpoint` | `IEndpointGroup` (with `--entity`: Create/GetById endpoints + integration test) | `dotnet new jex-endpoint -n Orders --entity Order --solution-name Acme -o .` |

See [docs/scaffolding.md](docs/scaffolding.md).

## Developing this template

```bash
dotnet run --project src/Jex.Cli -- dev install-cli   # pack this repo's jex and install it as a global tool (0.0.0-dev)
jex dev check --ci       # strict check of the template source (templates/solution)
jex dev install          # install all 4 templates from this directory (dev loop, no packing)
jex dev new Acme --database postgresql --aspire true --check   # generate into artifacts/sandbox/Acme, assert structure + strict check
jex dev matrix           # 6 combinations: generate → assert structure → jex scaffold → jex check --ci
jex dev install --from-package   # pack, then install from the nupkg (release acceptance path)
```

Repository layout, placeholder rules, the jex dev loop and the release process are described in [CONTRIBUTING.md](CONTRIBUTING.md).

## Documentation

The documents below are written in Chinese.

- [Getting started](docs/getting-started.md)
- [Template parameters](docs/template-parameters.md)
- [Scaffolding](docs/scaffolding.md)
- [Working with AI assistants](docs/ai-agents.md)
- [Roadmap and non-goals](docs/roadmap.md)
- [Architecture decisions for this repository](docs/decisions/)
- [Changelog](CHANGELOG.md)

## License

[MIT](LICENSE)
