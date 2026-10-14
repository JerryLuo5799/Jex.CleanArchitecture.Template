# 快速上手

[English](getting-started.md) | **简体中文**

## 前置条件

- .NET 10 SDK（`dotnet --version` ≥ 10.0.100）
- 可选：Docker（非 SQLite 数据库的本地容器、`TEST_DATABASE=provider` 集成测试、镜像构建）

## 1. 安装 jex

```bash
dotnet tool install -g Jex.Cli
jex --version
```

`jex` 是本模板的命令行工具。所有 `jex` 命令都有等价的 `dotnet` 命令（生成项目的 `AGENTS.md` §10），不装 jex 也能开发。

## 2. 生成项目

```bash
jex new Acme.Shop
cd Acme.Shop
```

`jex new` 会先确保本机装有与 jex 同版本的模板包（`dotnet new install Jex.CleanArchitecture.Template@<版本>`），再运行 `dotnet new jex -n Acme.Shop`。不用 jex 时：

```bash
dotnet new install Jex.CleanArchitecture.Template
dotnet new list jex      # jex / jex-entity / jex-usecase / jex-endpoint
dotnet new jex -n Acme.Shop
```

生成的项目根目录有 `jex.json`（jex 的项目标记，记录生成时的模板版本），请一起提交。

## 3. 运行

```bash
jex check      # 验收：格式化 + 构建 + 测试
jex run        # 启动；浏览器打开 /scalar
```

Development 环境会自动建表并种子化 `admin@example.com / Admin123!`。用 `src/Acme.Shop.WebAPI/Acme.Shop.WebAPI.http` 登录后调用接口。

非 SQLite 数据库先启动容器：`jex infra up`（或 `--use-aspire` 生成后用 `jex run --aspire`）。

## 4. 开始用 AI 开发

用 Claude Code / Copilot / Cursor 打开项目目录，它们会读取 `AGENTS.md`。例如在 Claude Code 里：

```
/new-feature Order Orders 含 CustomerName(必填,≤100)、Total(decimal>0)
```

它会按 `AGENTS.md` 的流程运行脚手架、填充代码、补测试、生成迁移脚本并跑 `jex check`。详见 [ai-agents.zh-CN.md](ai-agents.zh-CN.md)。

## 5. 上线前

- 设置 `Jwt:Secret`（≥ 32 字符）等密钥：环境变量或 `dotnet user-secrets`，不要写进 `appsettings.json`。
- 生产 `Database:AutoSyncStructure=false`；用 `jex db script initial` 生成初始迁移并提交，部署流水线运行 `dotnet <App>.WebAPI.dll migrate`。
- 配置 `Cors:AllowedOrigins`、限流阈值；需要初始管理员时临时开启 `Seed:Admin:Enabled` 并通过环境变量提供凭据。

## 升级 jex 与模板

```bash
dotnet tool update -g Jex.Cli
```

之后的 `jex new` 会自动切换到同版本的模板包。已有项目里的 `jex scaffold` 不会安装或升级模板：它核对 `jex.json` 的模板版本与本机已安装的模板，主版本不同时拒绝执行，并提示 `dotnet new install Jex.CleanArchitecture.Template@<版本> --force`（切换本机模板）或 `--force`（用已安装的模板继续）。
