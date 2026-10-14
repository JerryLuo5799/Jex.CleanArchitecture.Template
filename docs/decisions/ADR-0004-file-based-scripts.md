# ADR-0004 Repository scripts are .NET 10 file-based apps

**English** | [简体中文](ADR-0004-file-based-scripts.zh-CN.md)

- Status: Accepted
- Date: 2026-10-12

## Decision

The structure assertions (`scripts/assert-structure.cs`) and the matrix test (`scripts/test-template-matrix.cs`) are single-file C# programs run with `dotnet run --file <script>.cs`.

- One piece of code behaves the same on Windows / Linux / macOS, instead of maintaining both `.ps1` and `.sh` copies.
- They depend only on the .NET SDK, so CI and local runs are the same.
- The repository root contains `Jex.CleanArchitecture.Template.csproj`, so `--file` must be given explicitly; otherwise `dotnet run` treats the script as an argument to that project.

## Consequences

Process calls in the scripts use `ProcessStartInfo.ArgumentList`, so paths with spaces need no manual escaping; the matrix output directories deliberately contain spaces.

Both scripts are wrapped by `jex dev new` / `jex dev matrix` ([ADR-0005](ADR-0005-jex-cli.md)) and stay as standalone scripts instead of being ported into the CLI.
