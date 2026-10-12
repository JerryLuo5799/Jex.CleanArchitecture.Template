# Jex

基于 Clean Architecture 的 .NET 10 Web API。开发规范、目录约定、命令速查都在 [AGENTS.md](AGENTS.md)，AI 助手与新成员请先读它。

## 快速开始

```bash
# 可选：安装 go-task 以使用 task 命令（winget install Task.Task / brew install go-task）
task check          # 格式化 + 构建 + 测试（验收命令）
task run            # 启动 API：http://localhost:5208 ，Scalar UI：/scalar
```

开发环境首次启动会自动建表，并种子化管理员 `admin@example.com / Admin123!`（见 `src/Jex.WebAPI/appsettings.Development.json`）。
用 `src/Jex.WebAPI/Jex.WebAPI.http` 可以直接登录并调用示例接口。

没有 go-task 时，所有命令都有等价的 `dotnet` 命令（AGENTS.md §10）。

## 结构

```
src/
  Jex.Domain/           实体、枚举、常量、领域事件
  Jex.Application/      Command/Query + Handler、Validator、管道行为、接口
  Jex.Infrastructure/   FreeSql 仓储与配置、Identity Store、迁移、健康检查
  Jex.WebAPI/           端点组、信封、认证/授权、启动
<!--#if (UseAspire) -->
  Jex.AppHost/          .NET Aspire 编排
<!--#endif -->
  Jex.ServiceDefaults/  OpenTelemetry、服务发现、弹性策略
tests/
  Jex.UnitTests/  Jex.IntegrationTests/  Jex.ArchitectureTests/
db/migrations/          版本化 SQL 迁移（task db:script 生成，task db:migrate 执行）
docs/decisions/         架构决策记录（ADR）
openapi.json            构建时生成的 API 契约
```

示例功能切片：`TodoLists` / `TodoItems`（实体 → 仓储 → 用例 → 端点 → 测试），新功能照着它写。

## 常用命令

| 目的 | 命令 |
|---|---|
| 验收 | `task check` |
| 新功能脚手架 | `task scaffold:feature ENTITY=Order PLURAL=Orders` |
| 生成迁移脚本 | `task db:script NAME=add-orders` |
| 执行迁移 | `task db:migrate` |
<!--#if (IncludeDocker) -->
| 本地数据库容器 | `task infra:up` / `task infra:down` |
| 构建镜像 | `docker build -f src/Jex.WebAPI/Dockerfile -t TPLNAMELOWER .` |
<!--#endif -->
<!--#if (UseAspire) -->
| Aspire 编排运行 | `task run:aspire` |
<!--#endif -->

## 配置

- 密钥不进仓库：`Jwt:Secret`、`Seed:Admin:Password` 等通过环境变量或 `dotnet user-secrets` 提供；`appsettings.Development.json` 里的开发密钥由模板生成，仅供本机使用。
- 数据库：`ConnectionStrings:DefaultConnection` 与 `Database:Provider`；生产环境 `Database:AutoSyncStructure` 必须为 `false`（ADR-0008）。
- CORS、限流、种子、迁移：见 `appsettings.json` 对应节，启动时校验。

## 删除示例功能

删除 `src/*/**/Todo*`、`src/Jex.WebAPI/Endpoints/Todo*.cs`、`tests/**/Todo*`、`src/Jex.Infrastructure/Persistence/Configurations/TodoConfigurations.cs`，然后运行 `task check`。
