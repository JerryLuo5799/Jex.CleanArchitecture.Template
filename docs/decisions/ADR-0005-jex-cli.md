# ADR-0005 用 jex 命令行工具取代 Taskfile

- 状态：已采纳（取代 [ADR-0002](ADR-0002-taskfile.md)）
- 日期：2026-10-12

## 背景

仓库与生成项目都依赖 go-task：用户要额外安装第三方工具，生成项目的 Taskfile 用模板占位符写死了解决方案名、API 路径和数据库 profile，改名或换数据库后就与项目不一致。

## 决策

- 新增 .NET 全局工具 **`Jex.Cli`**（命令名 `jex`，`src/Jex.Cli`），与模板包同版本、同一个 tag 发布。生成项目的命令（`jex check`、`jex scaffold …`、`jex db …`、`jex infra …`）与仓库开发任务（`jex dev …`）都走它；仓库与生成项目都删除 `Taskfile.yml`。
- **项目识别用清单**（与 Angular、Aspire CLI、ABP Studio 相同）：生成项目根目录有 `jex.json`，jex 从当前目录向上查找。清单只记录不会漂移的事实：标记与**生成时的模板版本**（打包时由 `StampJexManifest` 写入包版本，源文件固定为 `0.0.0-dev`）。解决方案、API / AppHost / 测试项目从 `*.slnx` 的项目引用推导，数据库 profile 只在 `jex infra` 需要时从 `appsettings.json` 推导；改名后跟随 slnx 的项目引用，推导不出时报错并提示 `--api-project` / `--solution-name`。
- **两套版本策略**：`dotnet new install` 每台机器只保留一个版本，所以只有 `jex new` 会安装或切换模板包（目标为 `--template-version`，缺省为 CLI 版本）；`jex scaffold` 从不安装，只核对 `jex.json` 的版本与已安装模板的主版本，不一致时报错并给出切换命令或 `--force`。
- 命令只拼参数、交给不经 shell 的进程执行器（`ProcessStartInfo.ArgumentList`）；文件系统操作用 C#。多步命令任一步失败即停止并返回该步的退出码。
- `jex dev check` / `jex dev new <Name> --check` 在当前进程内复用 `check` 的实现；矩阵脚本是独立进程，通过 `--jex <exe>` 与 `--jex-arg <arg>` 显式接收被测 CLI，不从 PATH 查找。
- 结构断言与矩阵脚本保留为 file-based app（[ADR-0004](ADR-0004-file-based-scripts.md)），由 `jex dev` 包装。

## 备选

- 继续用 go-task：额外依赖，Taskfile 与项目会漂移。
- `dotnet` 本地工具清单（`.config/dotnet-tools.json`）：生成项目还没有包源时无法 restore，也无法引用未发布的版本；全局工具与用户的使用方式一致。
- 在 jex.json 中记录解决方案名、项目路径：信息会随重构失效。

## 后果

- 用户只需 `dotnet tool install -g Jex.Cli`；没装 jex 的环境仍可按 `AGENTS.md` §10 用原生命令工作。
- 模板源码里的工具包名写成占位符 `TPLTOOL`（生成时还原为 `Jex.Cli`），否则 `NameSafe` 会把 `Jex.Cli` 改名为 `<项目名>.Cli`。
- 发布 job 推送两个包；首次发布前需确认 nuget.org 的 Trusted Publishing 策略允许创建 `Jex.Cli`。
- jex 之前生成的项目可在根目录运行 `jex init` 补 `jex.json`（版本未知，脚手架需要 `--template-version` 或 `--force`）。
