# 与 AI 助手协作

生成的项目（`--include-ai-docs true`，默认）为主流 AI 编码工具准备好了上下文，规则只维护在一处。

| 文件 | 读者 | 内容 |
|---|---|---|
| `AGENTS.md` | 所有工具（Codex、Cursor、Copilot、Claude 等） | **唯一规范来源**：架构、目录→产物映射（附真实示例路径）、标准流程、命名、Must/Must-Not、自动注册清单、响应契约、测试约定、命令速查、常见失败与修复、ADR 索引 |
| `CLAUDE.md` | Claude Code | `@AGENTS.md` 引入 + Claude 专属说明 |
| `.claude/settings.json` | Claude Code | 权限白名单：只放行 `task check/build/test*/format/scaffold:*`、`dotnet build/test/format`、`dotnet new jex-*` 等；拒绝 `git push`、`rm -rf`、`task db:reset`、读取 `.env` 与生产配置 |
| `.claude/skills/new-feature` | Claude Code | `/new-feature Order Orders [字段描述]`：脚手架 → 填充 → 测试 → 迁移 → `task check` |
| `.claude/skills/review-architecture` | Claude Code | `/review-architecture`：架构测试 + Must-Not 对照 |
| `.github/copilot-instructions.md` | GitHub Copilot | 一行指向 `AGENTS.md` |
| `.github/instructions/*.instructions.md` | GitHub Copilot | 按路径生效的端点与测试规则 |
| `.mcp.json` | 支持 MCP 的工具 | Microsoft Learn 文档服务器，减少 API 臆造 |

## 让 AI 自己闭环的设计

- **一个验收命令**：`task check`。AI 改完就跑，失败就修。
- **架构测试的失败信息就是修复指引**，例如"CreateOrderCommand 缺少 Validator：同目录新建 …"。
- **编译期护栏**：`BannedSymbols.txt` 禁止 `DateTime.Now`、`Task.Result`、`Console.WriteLine`、AutoMapper；`.editorconfig` + `dotnet format` 统一风格。
- **照抄示例**：TodoLists / TodoItems 是完整功能切片，`AGENTS.md` 的映射表每行都指向真实文件。
- **自动注册**：端点组、验证器、Handler、仓储、实体配置全部扫描注册，AI 不需要（也不应该）修改 `DependencyInjection.cs`。
- **脚手架先行**：先生成可编译骨架，再填充业务逻辑，比从零写出错少。
- **契约可见**：`openapi.json` 在构建时生成并提交，接口变化在 diff 中可见。

## 建议的提示方式

- "按 AGENTS.md 新增 Order 功能：字段 …，规则 …，完成后跑 task check。"
- "运行 /review-architecture 并修复问题。"
- "给 CreateOrderCommand 增加校验：Total > 0，CustomerName 必填 ≤ 100，补单元测试。"
