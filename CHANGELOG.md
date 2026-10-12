# Changelog

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [SemVer](https://semver.org/lang/zh-CN/)。

## [Unreleased]

## [2.0.0]

全面重写。升级前请卸载旧版：`dotnet new uninstall Jex.CleanArchitecture.Template`。

### 新增

- 模板参数：`--database sqlite|postgresql|sqlserver|mysql`、`--use-aspire`、`--include-ai-docs`、`--include-docker`、`--http-port`、`--https-port`、`--skip-restore`。
- Item templates：`jex-entity`、`jex-usecase`、`jex-endpoint`；生成即可通过 `task check`，重复运行拒绝覆盖。
- 生成项目的 `AGENTS.md`、`CLAUDE.md`、`.claude/settings.json`、`/new-feature` 与 `/review-architecture` skills、Copilot 指令、`.mcp.json`、9 条 ADR、`Taskfile.yml`。
- MediatR 管道：日志、未处理异常、授权（`[AuthorizeRequest]`）、校验、性能、工作单元；`ICommand` / `IQuery<T>`。
- FreeSql 工作单元、只写脏列的仓储、审计字段、软删除（`DeletedToken` 保证仅活跃记录唯一）、领域事件。
- DbUp 版本化迁移：`migrate`、`db-script` 命令（基于独立基准库生成脚本）。
- Minimal API `IEndpointGroup`、统一信封（含 401/403/404/405/415/429/绑定失败）、Scalar、构建时 `openapi.json`、CORS、限流、健康检查。
- ServiceDefaults（OpenTelemetry、服务发现、弹性）与可选 Aspire AppHost；Dockerfile 与 compose profiles。
- 示例功能切片 TodoLists / TodoItems；单元、集成（含 Testcontainers）、架构测试。
- 仓库：`templates/` 布局、`Taskfile.yml`、结构断言与矩阵脚本、可复用 CI 工作流、Trusted Publishing 发布。

### 变更

- 校验从 Sannr 换为 FluentValidation；Controllers 换为 Minimal API；成功业务码从 `2000` 改为 `0`。
- 测试迁移到 xunit.v3 + Microsoft.Testing.Platform 与 Shouldly。
- 许可证统一为 MIT。

### 修复

- 改邮箱后无法登录（`NormalizedEmail` 未同步）；登录未检查锁定与用户状态。
- 请求日志记录明文请求体（含密码）。
- 首次 `dotnet run` 因 JWT 占位符直接抛异常。
- 分页 `totalCount` 从未赋值；校验错误详情丢失。
- 仓储更新覆盖全部列。
