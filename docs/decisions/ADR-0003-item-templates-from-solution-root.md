# ADR-0003 Item templates run from the solution root

**English** | [简体中文](ADR-0003-item-templates-from-solution-root.zh-CN.md)

- Status: Accepted
- Date: 2026-10-12

## Context

A feature slice writes to `src/*.Domain`, `src/*.Application`, `src/*.Infrastructure`, `src/*.WebAPI` and `tests/*` at the same time. The `msbuild:RootNamespace` binding only works inside a single project directory.

## Decision

- Item templates run from the solution root, and `JexSolution` in the output paths is replaced by the required `--solution-name` parameter (`jex scaffold` derives the prefix from the `*.WebAPI` project in the solution and fills it in, and switches to the root when run from any subdirectory).
- The generated code must pass `jex check` straight away (including the format check and architecture tests): commands always have a Validator, handlers are compiling stubs, and tests are passing placeholder cases.
- When a target file already exists, `dotnet new` refuses to overwrite it by default (exit code 73); `--force` is not added.
- The type parameter of the use case template is called `--usecase-type`, to avoid the `--type` that `dotnet new` already has.

## Consequences

In every combination, the matrix test uses the jex under test to run a full `jex scaffold feature` (entity → usecase ×2 → endpoint) plus two more use cases, and verifies that a second run fails.
