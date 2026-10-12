---
applyTo: "src/**/Endpoints/**/*.cs"
---

# 端点组规则（详见 AGENTS.md §3/§5/§8）

- 每个文件一个 `public sealed class {Feature} : IEndpointGroup`，显式声明 `public static string RoutePrefix => "/{feature}";`（不含 `/api`）。
- 在 `Map(RouteGroupBuilder group)` 里用 `group.MapGet(Handler, "pattern")` 等重载：**处理方法在前**，必须是本类的私有静态方法，不能是 lambda（端点名自动为 `{Group}_{Method}`）。
- 需要登录的分组调用 `group.RequireAuthorization()`；匿名端点单独 `.AllowAnonymous()`；登录类端点加 `.RequireRateLimiting(RateLimitingSettings.AuthPolicyName)`。
- 处理方法只做两件事：`sender.Send(command/query)` 与 `ApiResponse.Success(...)`。不要在端点里写业务逻辑、不要注入仓储或 `IFreeSql`。
- 分页查询返回 `ApiResponse.Success(result.Items, result.TotalCount)`。
- 路由参数 + 请求体的组合用一个 `sealed record {Verb}{Entity}Request(...)` 作为 body，再在处理方法里拼成 Command。
- 参考实现：`src/Jex.WebAPI/Endpoints/TodoItems.cs`。
