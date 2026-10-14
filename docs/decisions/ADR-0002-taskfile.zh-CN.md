# ADR-0002 用 Taskfile 作为任务入口

[English](ADR-0002-taskfile.md) | **简体中文**

- 状态：已被 [ADR-0005](ADR-0005-jex-cli.zh-CN.md) 取代（仓库与生成项目都改用 `jex` 命令行工具，不再需要 go-task）
- 日期：2026-10-12

## 决策

仓库与生成项目都用 [go-task](https://taskfile.dev) 的 `Taskfile.yml` 提供快捷命令。

- 跨平台（内置 shell 解释器，Windows 无需 bash），单文件，`task --list` 即命令清单，AI 容易读懂。
- 每个任务只是 `dotnet` / `docker` 命令的薄封装，生成项目的 `AGENTS.md` 同时列出等价原生命令，没装 go-task 也能工作。
- 生成项目的验收命令统一为 `task check`；CI 等价为 `task check:ci`。

## 备选

- PowerShell + bash 脚本成对维护：两份逻辑容易漂移。
- `dotnet` 本地工具：对非 .NET 命令（docker）不自然。
- Makefile：Windows 体验差。
