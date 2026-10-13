# Jex.CleanArchitecture.Template

[English](README.md) | **简体中文**

一个面向 **AI 辅助开发** 的 .NET 10 Web API 项目模板：一条命令在本地生成项目，生成的项目自带清晰的分层架构、统一的开发规范、脚手架命令和一份 `AGENTS.md`，Claude Code、GitHub Copilot、Cursor 打开就能按规范写代码。

## 快速开始

```bash
dotnet tool install -g Jex.Cli                    # 一次性安装 jex 命令行工具
jex new Acme.Shop                                 # 自动安装同版本的模板包；SQLite，开箱即跑
cd Acme.Shop
jex check                                         # 格式化 + 构建 + 全部测试
jex run                                           # http://localhost:<随机端口>/scalar
```

开发环境首次启动会自动建表并创建管理员 `admin@example.com / Admin123!`。

`jex new` 会先确保本机装有与 jex **同版本**的模板包 `Jex.CleanArchitecture.Template`（没有就 `dotnet new install`，版本不同就切换），再调用 `dotnet new jex`。不想用 jex 时可以直接 `dotnet new install Jex.CleanArchitecture.Template` + `dotnet new jex -n Acme.Shop`。升级：`dotnet tool update -g Jex.Cli`，下次 `jex new` 自动切换到新模板。

其它组合：

```bash
jex new Acme --database postgresql --use-aspire     # PostgreSQL + .NET Aspire 编排
jex new Acme --include-ai-docs false --include-docker false
```

| 参数 | 短名 | 默认 | 说明 |
|---|---|---|---|
| `--database` | `-db` | `sqlite` | `sqlite` / `postgresql` / `sqlserver` / `mysql`，决定 FreeSql 与 DbUp 提供程序、连接串、compose 服务 |
| `--use-aspire` | `-aspire` | `false` | 生成 `*.AppHost`（ServiceDefaults 始终包含） |
| `--include-ai-docs` | `-ai` | `true` | `AGENTS.md`、`CLAUDE.md`、`.claude/`、Copilot 指令、`.mcp.json` |
| `--include-docker` | `-docker` | `true` | `Dockerfile`、`docker-compose.yml`、`.env.example` |
| `--http-port` / `--https-port` | `-hp` / `-hsp` | 随机 | launchSettings 端口 |
| `--skip-restore` | `-sr` | `false` | 跳过生成后的 restore |

`jex new` 与 `dotnet new jex` 参数相同，另外支持 `--template-version <版本>`（使用指定的模板版本）和 `--no-install`（不安装、不切换模板包，缺少时报错）。完整说明见 [docs/template-parameters.md](docs/template-parameters.md)。

## 生成的项目包含什么

- **架构**：Domain / Application / Infrastructure / WebAPI 四层，依赖规则由架构测试强制。
- **CQRS**：MediatR 12（`ICommand` / `IQuery<T>`），管道行为：日志 → 未处理异常 → 授权（`[AuthorizeRequest]`）→ FluentValidation → 性能 → 工作单元。
- **数据**：FreeSql Code First，仓储只写脏列，审计字段自动填充，软删除（仅活跃记录唯一），领域事件在事务内派发；生产用 DbUp 执行已审阅的 SQL 迁移。
- **API**：Minimal API `IEndpointGroup`，统一 HTTP 200 + `{ requestId, code, msg, data, totalCount, errors }` 信封，Scalar UI，构建时生成并提交 `openapi.json`。
- **安全与运维**：ASP.NET Core Identity + JWT、锁定与状态校验、CORS、限流、健康检查、NLog、OpenTelemetry（可选 Aspire）、Docker。
- **测试**：xunit.v3 + MTP、Shouldly；单元、集成（SQLite 或 Testcontainers 真实数据库）、架构测试；`TransactionRollbackTests` 覆盖嵌套事务与事件回滚。
- **AI 开发**：`AGENTS.md`（唯一规范来源）、`CLAUDE.md`、`/new-feature` 与 `/review-architecture` skills、按路径生效的 Copilot 指令、收窄的权限白名单。
- **脚手架**：`jex scaffold feature Order Orders` 一次生成实体、仓储、配置、DTO、命令、查询、端点和测试，生成即可通过 `jex check`。

## jex 命令

在生成项目的任意子目录运行（jex 向上查找根目录的 `jex.json`，该文件记录生成时的模板版本）：

| 命令 | 作用 |
|---|---|
| `jex check` / `jex check --ci` | 验收：格式化 + 构建 + 测试；`--ci` 与 CI 同样严格（格式只校验、警告即错误） |
| `jex build`、`jex test`、`jex test unit`、`jex format --check` | 构建、测试（可只跑 `unit` / `integration` / `arch`）、格式 |
| `jex run`、`jex run --aspire`、`jex watch` | 运行 API、Aspire AppHost、热重载 |
| `jex scaffold feature Order Orders` | 实体 + `CreateOrder` + `GetOrderById` + 端点组，任一步失败即停止 |
| `jex scaffold entity Order Orders`、`jex scaffold usecase CancelOrder Orders --with-id`、`jex scaffold endpoint Reports` | 单独生成 |
| `jex db script add-orders`、`jex db migrate`、`jex db reset` | 生成迁移脚本、执行迁移、删除本地 SQLite 库 |
| `jex infra up`、`jex infra down` | 按 `Database:Provider` 启停数据库容器（SQLite 无需容器） |
| `jex init` | 给 jex 之前生成的旧项目补一个 `jex.json` |

每个命令都只是 `dotnet` / `docker` 命令的封装，生成项目的 `AGENTS.md` §10 列出了等价原生命令。

## Item templates

`jex scaffold` 会自动填写 `--solution-name`；也可以在生成项目的根目录直接调用：

| 模板 | 作用 | 示例 |
|---|---|---|
| `jex-entity` | 实体 + 仓储接口/实现 + 实体配置 + DTO + 单测 | `dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .` |
| `jex-usecase` | Command（+Handler+Validator）或 Query（+Handler）+ 单测 | `dotnet new jex-usecase -n CreateOrder --feature Orders --usecase-type command --return-type long --solution-name Acme -o .` |
| `jex-endpoint` | `IEndpointGroup`（可选 `--entity` 映射 Create/GetById + 集成测试） | `dotnet new jex-endpoint -n Orders --entity Order --solution-name Acme -o .` |

详见 [docs/scaffolding.md](docs/scaffolding.md)。

## 开发本模板

```bash
dotnet run --project src/Jex.Cli -- dev install-cli   # 把本仓库的 jex 打包并安装为全局工具（0.0.0-dev）
jex dev check --ci       # 严格检查模板源码（templates/solution）
jex dev install          # 从当前目录安装全部 4 个模板（开发循环，无需打包）
jex dev new Acme --database postgresql --aspire true --check   # 生成到 artifacts/sandbox/Acme，结构断言 + 严格检查
jex dev matrix           # 6 种组合：生成 → 结构断言 → jex 脚手架 → jex check --ci
jex dev install --from-package   # 打包后从 nupkg 安装（发布验收路径）
```

仓库结构、占位符规则、jex 的开发循环与发布流程见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 文档

- [快速上手](docs/getting-started.md)
- [模板参数](docs/template-parameters.md)
- [脚手架](docs/scaffolding.md)
- [与 AI 助手协作](docs/ai-agents.md)
- [路线图与不做的事](docs/roadmap.md)
- [模板仓库的架构决策](docs/decisions/)
- [变更记录](CHANGELOG.md)

## 许可证

[MIT](LICENSE)
