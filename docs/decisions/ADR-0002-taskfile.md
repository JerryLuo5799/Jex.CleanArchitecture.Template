# ADR-0002 Taskfile as the task entry point

**English** | [简体中文](ADR-0002-taskfile.zh-CN.md)

- Status: Superseded by [ADR-0005](ADR-0005-jex-cli.md) (the repository and generated projects use the `jex` command-line tool instead and no longer need go-task)
- Date: 2026-10-12

## Decision

Both the repository and generated projects provide shortcut commands through a [go-task](https://taskfile.dev) `Taskfile.yml`.

- Cross-platform (built-in shell interpreter, no bash needed on Windows), a single file, `task --list` is the command list, and it is easy for AI to read.
- Each task is only a thin wrapper around `dotnet` / `docker` commands, and the generated project's `AGENTS.md` also lists the equivalent native commands, so everything works without go-task.
- The acceptance command of a generated project is `task check`; the CI equivalent is `task check:ci`.

## Alternatives

- Paired PowerShell + bash scripts: two copies of the logic drift apart easily.
- A `dotnet` local tool: awkward for non-.NET commands (docker).
- Makefile: poor experience on Windows.
