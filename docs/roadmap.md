# 路线图与明确不做的事

## 计划中

- Refresh token（轮换 + 吊销）。
- Outbox：让领域事件可靠地通知外部系统（目前事件处理器只能做事务内的数据库操作，见生成项目的 ADR-0005）。
- FreeSql 的 OpenTelemetry `ActivitySource`（SQL 追踪）。
- 用 `Microsoft.TemplateEngine.Authoring.TemplateVerifier` 做生成结果快照测试，补充现有结构断言。
- 迁移的数据库级互斥锁（PostgreSQL advisory lock / SQL Server `sp_getapplock`），目前依赖部署流水线串行执行。

## 暂不做（及原因）

| 项 | 原因 |
|---|---|
| API 版本化 | 多数项目用不到；需要时在端点组的 `RoutePrefix` 上加版本段即可 |
| 幂等键 | 与业务强相关，不适合放进通用模板 |
| Kubernetes 清单 | 部署方式差异大；提供了 Dockerfile 与健康检查端点 |
| devcontainer | 维护成本高于收益；`global.json` + `jex` 已足够 |
| Husky / pre-commit 钩子 | 本地钩子增加摩擦，规则由 CI 强制 |
| OpenAPI 客户端生成 | 提交了 `openapi.json`，前端可按需选择生成器 |
| 模板内置 MCP server | 用 `.mcp.json` 引用现成的文档服务器即可 |
| AutoMapper / MediatR 13+ / FluentAssertions 8+ | 商业许可；见生成项目的 ADR-0009 与 dependabot 忽略规则 |
