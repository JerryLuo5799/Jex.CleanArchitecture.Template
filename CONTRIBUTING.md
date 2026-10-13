# 参与开发

## 仓库结构

```
Jex.CleanArchitecture.Template.csproj   模板打包项目（只打包 templates/**，打包时给 jex.json 盖上版本）
Jex.CleanArchitecture.slnx              打包项目 + jex CLI + CLI 测试（所有命令都显式指定它或具体项目）
src/Jex.Cli/                            jex 命令行工具（NuGet 包 Jex.Cli，与模板包同版本）
src/Jex.Cli.Tests/                      jex 的单元测试（xunit.v3 + MTP）
scripts/assert-structure.cs             生成结果的结构断言（.NET 10 file-based app，由 jex dev new 调用）
scripts/test-template-matrix.cs         组合矩阵测试（由 jex dev matrix 调用）
templates/solution/                     `dotnet new jex` 的内容（本身是可构建、可测试的 jex 项目）
templates/items/{entity,usecase,endpoint}/   item templates
docs/                                   模板仓库文档与 ADR
```

`templates/solution` 是一个真实的解决方案：模板源码按 **SQLite、无 Aspire** 的组合编译和测试；其它组合由矩阵测试验证。

## 开发循环

第一次在本仓库工作时，先把源码里的 jex 装成全局工具（之后改了 `src/Jex.Cli` 再运行一次）：

```bash
dotnet run --project src/Jex.Cli -- dev install-cli
```

1. 修改 `templates/solution`、`templates/items` 或 `src/Jex.Cli`。
2. `jex dev check --ci`：模板源码格式、严格构建、测试。改了 CLI 时再运行 `dotnet build Jex.CleanArchitecture.slnx -c Release -p:ContinuousIntegrationBuild=true` 与 `dotnet test --project src/Jex.Cli.Tests/Jex.Cli.Tests.csproj`。
3. `jex dev install` → `jex dev new Acme --database postgresql --check`：从当前目录安装模板，生成到 `artifacts/sandbox/Acme`，结构断言后运行 `check --ci`。
4. 影响多种组合时运行 `jex dev matrix`（或 `jex dev matrix --cells sqlite,bare`）。
5. 更新 `CHANGELOG.md` 的 `[Unreleased]`。

### jex 的开发循环

- **迭代 CLI 源码**：`dotnet run --project src/Jex.Cli -- dev new Acme --check`。`--check` 在同一进程内运行 `check --ci`，验证的就是当前源码，不经过全局安装的 jex。
- **重装全局 jex**：`dotnet run --project src/Jex.Cli -- dev install-cli`。它打包 `0.0.0-dev`、卸载旧工具、删除 NuGet 缓存里的 `jex.cli/0.0.0-dev`（版本号不变，不删缓存就会装回旧包），再用 `--source artifacts` 安装。不要用已安装的 `jex dev install-cli` 更新自己（Windows 下运行中的工具被锁定，jex 会拒绝）。
- **矩阵测哪个 jex**：矩阵脚本从不在 PATH 上找 jex，而是由 `jex dev matrix` 把自己的启动方式传给它（`--jex <exe>`，用 `dotnet jex.dll` 启动时再加 `--jex-arg <dll>`）。全局 jex 跑 `jex dev matrix` 测的是全局 jex；`dotnet run --project src/Jex.Cli -- dev matrix --cells sqlite,bare` 测的是源码。
- **版本**：本地构建的 CLI 是 `0.0.0-dev`（开发模式：`jex new` 只使用 `jex dev install` 安装的开发模板，不联网安装）。

## 模板编写规则

- **重命名**：解决方案模板不使用 `sourceName`，而是由 `NameSafe`（`--name` 的 safe_namespace 形式）同时替换文件名与内容中的 `Jex`，保证 `my-api` 这类名字的目录与命名空间一致（都变成 `my_api`）。
- **小写 `jex`** 只用于 jex 命令（`jex check`、`jex scaffold ...`）和 `jex.json`；引用 item template 短名时写 `TPLPREFIX-usecase`，生成后变成 `jex-usecase`。其它小写名称用 `TPLNAMELOWER`（kebab-case 项目名）。
- **jex 的 NuGet 包名写成 `TPLTOOL`**（`dotnet tool install -g TPLTOOL`），生成后变成 `Jex.Cli`；直接写 `Jex.Cli` 会被 `NameSafe` 改成 `<项目名>.Cli`。
- **占位符**：`TPLPREFIX`、`TPLTOOL`、`TPLNAMELOWER`、`DB_PROFILE`、`FREESQL_PROVIDER`、`TPLCONN_PREFIX`、`TPLCONN_SUFFIX`；新增占位符时同时加入 `scripts/assert-structure.cs` 的残留检查。
- **文档里的 jex 命令要能执行**：`Jex.Cli.Tests` 的 `DocsCommandTests` 用真实的命令解析器检查 `templates/solution` 与本仓库文档中每个以 jex 开头的行内代码与代码块命令，以及 `.claude/settings.json`、`.vscode/tasks.json` 里的 jex 命令（只写命令名的引用只要求命令存在；带参数的必须完整可解析）。
- **端口**：只写 `localhost:5208` / `localhost:7025`，模板会替换成随机端口；容器内固定 8080。
- **条件**：C# 用 `#if (UseSqlite)` 等模板条件；**不要使用原生 `#if DEBUG`**（会被模板引擎当作条件求值后删除），确需时用 `//-:cnd:noEmit` / `//+:cnd:noEmit` 包裹。JSON 用 `//#if`，XML/slnx/Markdown 用 `<!--#if -->`，YAML/Dockerfile/.env/SQL 用 `#if`（见 `SpecialCustomOperations`）。
- **模板源码专用内容**用 `TemplateSourceOnly` 条件包裹（生成时恒为假），例如让模板源码以 SQLite 方式编译的 `DefineConstants`。
- **第三方命名空间放在各项目的 `GlobalUsings.cs`**，源文件只 `using` `System.*` 和项目命名空间；否则 import 排序会随项目名变化（`Acme` 与 `my_api` 排序位置不同），生成项目的 `dotnet format --verify-no-changes` 会失败。
- **不要在模板内容里写本仓库的 GitHub URL**（包含 `Jex`，会被重命名）。
- 生成项目的规范只写在 `templates/solution/AGENTS.md`；其它 AI 入口文件只做指向。

## 新增一个模板参数

1. `templates/solution/.template.config/template.json` 增加 `parameter`（和需要的 `computed`/`generated` 符号、`modifiers`）。
2. `dotnetcli.host.json` 增加长短参数名（避开 `dotnet new` 自带的 `--type`、`--name`、`--output` 等）。
3. 在内容里用条件或占位符使用它。
4. `scripts/assert-structure.cs` 增加对应断言；`scripts/test-template-matrix.cs` 视需要增加矩阵格子，`.github/workflows/template-tests.yml` 同步。
5. 更新 `docs/template-parameters.md` 与 `README.md`、`README.zh-CN.md` 的参数表。

## 发布

模板包 `Jex.CleanArchitecture.Template` 与工具包 `Jex.Cli` 用同一个版本号、同一个 tag 一起发布（`jex new` 默认安装与自己同版本的模板）。

1. 更新 `CHANGELOG.md`，新增 `## [x.y.z]` 段落。
2. 打 tag `vx.y.z` 并推送。`release.yml` 会：校验版本号 → 严格检查并用 `dotnet pack Jex.CleanArchitecture.slnx -p:Version=x.y.z` 打出两个包 → 从这两个包安装 jex 与模板跑完整矩阵 → 校验 CHANGELOG → 通过 NuGet Trusted Publishing 发布这两个 nupkg 并创建 GitHub Release。
3. 首次发布前需在 nuget.org 为本仓库与 `release.yml` 配置 Trusted Publishing 策略（覆盖 `Jex.CleanArchitecture.Template` 与 `Jex.Cli` 两个包 id；首次发布 `Jex.Cli` 前确认策略允许创建新包），并设置仓库变量 `NUGET_USER`；`publish` job 使用名为 `nuget` 的 environment，可以在其上加人工审批。
