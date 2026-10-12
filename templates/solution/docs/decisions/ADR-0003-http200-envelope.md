# ADR-0003 HTTP 200 + 信封的响应契约

- 状态：已采纳
- 日期：2026-10-12

## 背景

前端与网关按统一结构处理所有业务响应，用业务码而非 HTTP 状态码区分结果。

## 决策

`/api/**` 下的所有响应均为 HTTP 200，主体为 `ApiResponse`：`{ requestId, code, msg, data, totalCount?, errors? }`。

- 成功 `code = 0`；失败码见 `ErrorCodes`（4000 校验、4010 未认证、4030 禁止、4040 未找到、4050 方法不允许、4090 冲突、4290 限流、5000 未知）。
- 所有失败入口都走 `ApiResponseWriter`：异常处理器、授权结果处理器、状态码页（404/405/415）、限流拒绝。
- 正常端点直接返回 `ApiResponse<T>`，由框架序列化；端点过滤器只填充 `requestId` 并记录业务码供请求日志使用。
- `requestId` = W3C trace id，同时写入 `X-Request-Id` 响应头与日志。

## 例外

- `/health*` 保留真实状态码（探针语义）。
- `/openapi`、`/scalar`、重定向、文件下载不套信封。
- 非 `/api` 路由保持框架默认行为。

## 后果

- 监控必须按业务 `code` 统计失败率，HTTP 成功率不再有意义（请求日志已包含 `code`）。
- 契约表在 `AGENTS.md` §8，集成测试 `ErrorEnvelopeTests` 逐项覆盖。
