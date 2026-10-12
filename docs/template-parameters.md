# 模板参数（`dotnet new jex`）

| 参数 | 短名 | 类型 / 默认 | 影响 |
|---|---|---|---|
| `-n, --name` | | 文本 / 当前目录名 | 项目与命名空间名。`Acme.Shop` → `Acme.Shop.*`；含 `-` 的名字转为合法命名空间（`my-api` → `my_api`），目录与命名空间保持一致；数据库名、compose 项目名使用 kebab-case（`acme-shop`）。 |
| `--database` | `-db` | `sqlite` | 见下表 |
| `--use-aspire` | `-aspire` | `false` | 生成 `src/*.AppHost`，加入解决方案；`Taskfile.yml` 增加 `run:aspire`。`*.ServiceDefaults` 始终生成。 |
| `--include-ai-docs` | `-ai` | `true` | 为 `false` 时不生成 `AGENTS.md`、`CLAUDE.md`、`.claude/`、`.mcp.json`、`.github/copilot-instructions.md`、`.github/instructions/`。 |
| `--include-docker` | `-docker` | `true` | 为 `false` 时不生成 `Dockerfile`、`.dockerignore`、`docker-compose.yml`、`.env.example`、`docker/`；`Taskfile.yml` 不含 `infra:*`。 |
| `--http-port` | `-hp` | 随机 5000–5300 | launchSettings 与 `.http` 中的 HTTP 端口 |
| `--https-port` | `-hsp` | 随机 7000–7300 | launchSettings 中的 HTTPS 端口 |
| `--skip-restore` | `-sr` | `false` | 生成后不执行 `dotnet restore` |

## `--database`

| 值 | FreeSql 提供程序 | DbUp | 默认连接串 | compose profile | Testcontainers |
|---|---|---|---|---|---|
| `sqlite` | `FreeSql.Provider.Sqlite` | `dbup-sqlite` | `Data Source=<name>.db` | 无 | 不需要 |
| `postgresql` | `FreeSql.Provider.PostgreSQL` | `dbup-postgresql` | `Host=localhost;Port=5432;…` | `postgresql` | `Testcontainers.PostgreSql` |
| `sqlserver` | `FreeSql.Provider.SqlServer` | `dbup-sqlserver` | `Server=localhost,1433;…` | `sqlserver` | `Testcontainers.MsSql` |
| `mysql` | `FreeSql.Provider.MySqlConnector` | `dbup-mysql` | `Server=localhost;Port=3306;…` | `mysql` | `Testcontainers.MySql` |

集成测试默认使用 SQLite 临时库（不需要 Docker）。`TEST_DATABASE=provider` 时使用项目的真实提供程序（Testcontainers，Docker 不可用即失败）；`TEST_DATABASE=auto` 在本地无 Docker 时回退 SQLite。

## 每次生成都会唯一化的值

- 开发环境 JWT 密钥（`appsettings.Development.json`）与 `UserSecretsId`：由 GUID 生成器重新生成。
- HTTP/HTTPS 端口：随机（可用参数固定）。
