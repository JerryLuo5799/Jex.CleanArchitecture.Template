# ADR-0004 仓库脚本使用 .NET 10 file-based app

[English](ADR-0004-file-based-scripts.md) | **简体中文**

- 状态：已采纳
- 日期：2026-10-12

## 决策

结构断言（`scripts/assert-structure.cs`）与矩阵测试（`scripts/test-template-matrix.cs`）写成单文件 C# 程序，用 `dotnet run --file <script>.cs` 运行。

- 一份代码在 Windows / Linux / macOS 上行为一致，不再维护 `.ps1` + `.sh` 两份。
- 只依赖 .NET SDK，CI 与本地相同。
- 仓库根目录有 `Jex.CleanArchitecture.Template.csproj`，因此必须显式写 `--file`，否则 `dotnet run` 会把脚本当作该项目的参数。

## 后果

脚本里的进程调用用 `ProcessStartInfo.ArgumentList`，路径含空格时无需手工转义；矩阵用例的输出目录故意包含空格。

两个脚本由 `jex dev new` / `jex dev matrix` 包装调用（[ADR-0005](ADR-0005-jex-cli.zh-CN.md)），保留为独立脚本而不移植进 CLI。
