# 参与开发

## 仓库结构

```
Jex.CleanArchitecture.Template.csproj   打包项目（只打包 templates/**）
Taskfile.yml                            仓库任务
scripts/assert-structure.cs             生成结果的结构断言（.NET 10 file-based app）
scripts/test-template-matrix.cs         组合矩阵测试
templates/solution/                     `dotnet new jex` 的内容（本身是可构建、可测试的解决方案）
templates/items/{entity,usecase,endpoint}/   item templates
docs/                                   模板仓库文档与 ADR
```

`templates/solution` 是一个真实的解决方案：模板源码按 **SQLite、无 Aspire** 的组合编译和测试；其它组合由矩阵测试验证。

## 开发循环

1. 修改 `templates/solution` 或 `templates/items`。
2. `task check:ci`：模板源码格式、严格构建、测试。
3. `task init` → `task new NAME=Acme DB=...` → `task new:check`：验证生成结果。
4. 影响多种组合时运行 `task template:test-all`（或 `-- --cells sqlite,bare`）。
5. 更新 `CHANGELOG.md` 的 `[Unreleased]`。

## 模板编写规则

- **重命名**：解决方案模板不使用 `sourceName`，而是由 `NameSafe`（`--name` 的 safe_namespace 形式）同时替换文件名与内容中的 `Jex`，保证 `my-api` 这类名字的目录与命名空间一致（都变成 `my_api`）。
- **小写 `jex` 只能出现在 `TPLPREFIX` 里**：生成项目中需要引用 item template 短名时写 `TPLPREFIX-usecase`，生成后变成 `jex-usecase`。其它小写名称用 `TPLNAMELOWER`（kebab-case 项目名）。
- **占位符**：`TPLPREFIX`、`TPLNAMELOWER`、`DB_PROFILE`、`FREESQL_PROVIDER`、`TPLCONN_PREFIX`、`TPLCONN_SUFFIX`；新增占位符时同时加入 `scripts/assert-structure.cs` 的残留检查。
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
5. 更新 `docs/template-parameters.md` 与 `README.md` 的参数表。

## 发布

1. 更新 `CHANGELOG.md`，新增 `## [x.y.z]` 段落。
2. 打 tag `vx.y.z` 并推送。`release.yml` 会：校验版本号 → 严格检查并打包 → 用同一个 nupkg 跑完整矩阵 → 校验 CHANGELOG → 通过 NuGet Trusted Publishing 发布同一个 nupkg 并创建 GitHub Release。
3. 首次发布前需在 nuget.org 为本仓库与 `release.yml` 配置 Trusted Publishing 策略，并设置仓库变量 `NUGET_USER`；`publish` job 使用名为 `nuget` 的 environment，可以在其上加人工审批。
