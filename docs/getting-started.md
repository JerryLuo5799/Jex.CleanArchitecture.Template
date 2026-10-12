# 快速上手

## 前置条件

- .NET 10 SDK（`dotnet --version` ≥ 10.0.100）
- 可选：[go-task](https://taskfile.dev)（`winget install Task.Task` / `brew install go-task` / `scoop install task`），所有 `task` 命令都有等价的 `dotnet` 命令
- 可选：Docker（非 SQLite 数据库的本地容器、`TEST_DATABASE=provider` 集成测试、镜像构建）

## 1. 安装模板

```bash
dotnet new install Jex.CleanArchitecture.Template
dotnet new list jex      # jex / jex-entity / jex-usecase / jex-endpoint
```

## 2. 生成项目

```bash
dotnet new jex -n Acme.Shop
cd Acme.Shop
```

## 3. 运行

```bash
task check     # 验收：格式化 + 构建 + 测试
task run       # 启动；浏览器打开 /scalar
```

Development 环境会自动建表并种子化 `admin@example.com / Admin123!`。用 `src/Acme.Shop.WebAPI/Acme.Shop.WebAPI.http` 登录后调用接口。

非 SQLite 数据库先启动容器：`task infra:up`（或 `--use-aspire` 后用 `task run:aspire`）。

## 4. 开始用 AI 开发

用 Claude Code / Copilot / Cursor 打开项目目录，它们会读取 `AGENTS.md`。例如在 Claude Code 里：

```
/new-feature Order Orders 含 CustomerName(必填,≤100)、Total(decimal>0)
```

它会按 `AGENTS.md` 的流程运行脚手架、填充代码、补测试、生成迁移脚本并跑 `task check`。详见 [ai-agents.md](ai-agents.md)。

## 5. 上线前

- 设置 `Jwt:Secret`（≥ 32 字符）等密钥：环境变量或 `dotnet user-secrets`，不要写进 `appsettings.json`。
- 生产 `Database:AutoSyncStructure=false`；用 `task db:script NAME=initial` 生成初始迁移并提交，部署流水线运行 `dotnet <App>.WebAPI.dll migrate`。
- 配置 `Cors:AllowedOrigins`、限流阈值；需要初始管理员时临时开启 `Seed:Admin:Enabled` 并通过环境变量提供凭据。
