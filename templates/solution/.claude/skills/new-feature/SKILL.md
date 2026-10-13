---
name: new-feature
description: 按 AGENTS.md 的标准流程为本项目新增一个功能切片（实体 → 仓储 → 用例 → 端点 → 测试 → 迁移）。用法：/new-feature Order Orders [字段描述]
---

# 新增功能切片

参数：`$ARGUMENTS` = `{Entity} {Plural} [字段/规则描述]`，例如 `Order Orders 含 CustomerName(必填,≤100)、Total(decimal>0)、Status 枚举`。

## 步骤（严格按顺序，每步完成后再进行下一步）

1. 阅读 `AGENTS.md` §3–§7，并打开示例文件 `src/Jex.Domain/Entities/TodoItem.cs`、`src/Jex.Application/Features/TodoItems/Commands/CreateTodoItem/CreateTodoItemCommand.cs`、`src/Jex.WebAPI/Endpoints/TodoItems.cs`、`tests/Jex.IntegrationTests/Endpoints/TodoItemsEndpointsTests.cs` 作为模板。
2. 运行脚手架：`jex scaffold feature {Entity} {Plural}`（没有 jex 时用 AGENTS.md §4 每步给出的原生 `dotnet new` 命令；其余用例按 §4 第 2 步用 `jex scaffold usecase` 逐个生成）。如果目标文件已存在，命令会报错，停下来向用户确认，不要覆盖；如果提示模板版本不匹配，把提示原样告诉用户，不要自行加 `--force`。
3. 运行 `jex build`（原生：`dotnet build Jex.slnx`）确认骨架可编译。
4. 填充：
   - 实体属性与 `{Entity}Configuration`（长度、索引、导航）。
   - Command/Query 的字段、Handler 逻辑（只调用仓储接口）、Validator 规则。
   - 端点组的路由与请求 record。
5. 补测试：Handler 单测、Validator 单测、端点集成测试（成功 + 校验失败 + 未认证各一个）。
6. 运行 `jex check`（原生命令见 AGENTS.md §10）。失败时按架构测试提示修复，不得关闭规则。构建会更新 `openapi.json`，一并提交。
7. 运行 `jex db script add-{plural-lower}` 生成迁移脚本。若 `db/migrations` 原本为空，脚本会包含全部现有表（初始迁移，属预期，向用户说明）；否则检查 SQL 只包含新表/新列。
8. 汇报：列出新增文件、`jex check` 的结果、迁移脚本路径。不要创建额外的 Markdown 文档。
