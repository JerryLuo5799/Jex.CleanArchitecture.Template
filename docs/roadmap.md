# Roadmap and non-goals

**English** | [简体中文](roadmap.zh-CN.md)

## Planned

- Refresh tokens (rotation + revocation).
- Outbox: reliably notify external systems of domain events (event handlers can currently only do database work inside the transaction; see ADR-0005 in the generated project).
- An OpenTelemetry `ActivitySource` for FreeSql (SQL tracing).
- Snapshot tests of generated output with `Microsoft.TemplateEngine.Authoring.TemplateVerifier`, in addition to the existing structure assertions.
- A database-level lock for migrations (PostgreSQL advisory lock / SQL Server `sp_getapplock`); today this relies on the deployment pipeline running them one at a time.

## Not planned for now (and why)

| Item | Reason |
|---|---|
| API versioning | Most projects do not need it; when needed, add a version segment to the endpoint group's `RoutePrefix` |
| Idempotency keys | Too closely tied to the business to fit a general template |
| Kubernetes manifests | Deployment setups vary widely; a Dockerfile and health check endpoints are provided |
| devcontainer | Costs more to maintain than it gives; `global.json` + `jex` are enough |
| Husky / pre-commit hooks | Local hooks add friction; CI enforces the rules |
| OpenAPI client generation | `openapi.json` is committed, so the frontend can pick any generator |
| An MCP server built into the template | Referencing an existing docs server in `.mcp.json` is enough |
| AutoMapper / MediatR 13+ / FluentAssertions 8+ | Commercial licenses; see ADR-0009 in the generated project and the dependabot ignore rules |
