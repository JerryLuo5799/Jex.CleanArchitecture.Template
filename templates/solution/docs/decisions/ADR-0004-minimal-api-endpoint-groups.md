# ADR-0004 Minimal API 端点组

- 状态：已采纳
- 日期：2026-10-12

## 决策

不使用 MVC Controller。每个功能一个 `IEndpointGroup` 类（`src/Jex.WebAPI/Endpoints/{Feature}.cs`）：

- `RoutePrefix` 显式声明（重命名类不会改变 URL），统一挂在 `/api` 之下。
- `Map(RouteGroupBuilder)` 使用 `MapGet(handler, pattern)` 等重载；处理方法必须是具名静态方法，端点名固定为 `{Group}_{Method}`，作为 OpenAPI 的 operationId。
- `MapEndpoints()` 反射发现所有实现，无需注册。
- 端点只负责把请求交给 MediatR 并包装 `ApiResponse`。

## 后果

- 架构测试保证端点组的位置、密封、前缀唯一与无 Controller。
- `openapi.json` 在构建时生成并提交，接口变更在代码评审中可见。
