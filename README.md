# Jex.CleanArchitecture.Template

一个面向 **AI 辅助开发** 的 .NET 10 Web API 项目模板：一条命令在本地生成项目，生成的项目自带清晰的分层架构、统一的开发规范、脚手架命令和一份 `AGENTS.md`，Claude Code、GitHub Copilot、Cursor 打开就能按规范写代码。

参考了 [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)（管道行为、端点组、item template）与 [netcorepal/netcorepal-cloud-template](https://github.com/netcorepal/netcorepal-cloud-template)（AI 指令、错误码、分页、compose profiles）。

## 快速开始

```bash
dotnet new install Jex.CleanArchitecture.Template
dotnet new jex -n Acme.Shop                       # SQLite，开箱即跑
cd Acme.Shop
task check                                        # 格式化 + 构建 + 全部测试（需要 go-task；等价 dotnet 命令见生成项目的 AGENTS.md）
task run                                          # http://localhost:<随机端口>/scalar
```

开发环境首次启动会自动建表并创建管理员 `admin@example.com / Admin123!`。

其它组合：

```bash
dotnet new jex -n Acme --database postgresql --use-aspire     # PostgreSQL + .NET Aspire 编排
dotnet new jex -n Acme --include-ai-docs false --include-docker false
```

| 参数 | 短名 | 默认 | 说明 |
|---|---|---|---|
| `--database` | `-db` | `sqlite` | `sqlite` / `postgresql` / `sqlserver` / `mysql`，决定 FreeSql 与 DbUp 提供程序、连接串、compose 服务 |
| `--use-aspire` | `-aspire` | `false` | 生成 `*.AppHost`（ServiceDefaults 始终包含） |
| `--include-ai-docs` | `-ai` | `true` | `AGENTS.md`、`CLAUDE.md`、`.claude/`、Copilot 指令、`.mcp.json` |
| `--include-docker` | `-docker` | `true` | `Dockerfile`、`docker-compose.yml`、`.env.example` |
| `--http-port` / `--https-port` | `-hp` / `-hsp` | 随机 | launchSettings 端口 |
| `--skip-restore` | `-sr` | `false` | 跳过生成后的 restore |

完整说明见 [docs/template-parameters.md](docs/template-parameters.md)。

## 生成的项目包含什么

- **架构**：Domain / Application / Infrastructure / WebAPI 四层，依赖规则由架构测试强制。
- **CQRS**：MediatR 12（`ICommand` / `IQuery<T>`），管道行为：日志 → 未处理异常 → 授权（`[AuthorizeRequest]`）→ FluentValidation → 性能 → 工作单元。
- **数据**：FreeSql Code First，仓储只写脏列，审计字段自动填充，软删除（仅活跃记录唯一），领域事件在事务内派发；生产用 DbUp 执行已审阅的 SQL 迁移。
- **API**：Minimal API `IEndpointGroup`，统一 HTTP 200 + `{ requestId, code, msg, data, totalCount, errors }` 信封，Scalar UI，构建时生成并提交 `openapi.json`。
- **安全与运维**：ASP.NET Core Identity + JWT、锁定与状态校验、CORS、限流、健康检查、NLog、OpenTelemetry（可选 Aspire）、Docker。
- **测试**：xunit.v3 + MTP、Shouldly；单元、集成（SQLite 或 Testcontainers 真实数据库）、架构测试；`TransactionRollbackTests` 覆盖嵌套事务与事件回滚。
- **AI 开发**：`AGENTS.md`（唯一规范来源）、`CLAUDE.md`、`/new-feature` 与 `/review-architecture` skills、按路径生效的 Copilot 指令、收窄的权限白名单。
- **脚手架**：`task scaffold:feature ENTITY=Order PLURAL=Orders` 一次生成实体、仓储、配置、DTO、命令、查询、端点和测试，生成即可通过 `task check`。

## Item templates

在生成项目的根目录运行（Taskfile 已封装）：

| 模板 | 作用 | 示例 |
|---|---|---|
| `jex-entity` | 实体 + 仓储接口/实现 + 实体配置 + DTO + 单测 | `dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .` |
| `jex-usecase` | Command（+Handler+Validator）或 Query（+Handler）+ 单测 | `dotnet new jex-usecase -n CreateOrder --feature Orders --usecase-type command --return-type long --solution-name Acme -o .` |
| `jex-endpoint` | `IEndpointGroup`（可选 `--entity` 映射 Create/GetById + 集成测试） | `dotnet new jex-endpoint -n Orders --entity Order --solution-name Acme -o .` |

详见 [docs/scaffolding.md](docs/scaffolding.md)。

## 开发本模板

```bash
task check:ci            # 严格检查模板源码（templates/solution）
task init                # 从当前目录安装全部 4 个模板（开发循环，无需打包）
task new NAME=Acme DB=postgresql ASPIRE=true
task new:check           # 严格检查刚生成的项目
task template:test-all   # 6 种组合：生成 → 结构断言 → 脚手架 → 格式/构建/测试
task init:pkg            # 打包后从 nupkg 安装（发布验收路径）
```

仓库结构、占位符规则与发布流程见 [CONTRIBUTING.md](CONTRIBUTING.md)。

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
