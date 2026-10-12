---
applyTo: "tests/**/*.cs"
---

# 测试规则（详见 AGENTS.md §9）

- 命名 `{Method}_{Scenario}_{Expected}`；断言用 Shouldly（`ShouldBe`、`ShouldThrowAsync`），不要用 `Assert.*`。
- 单元测试只测 Handler / Validator / Behaviour / 实体行为，用 Moq 模拟 `I*Repository`、`IIdentityService`、`ICurrentUser`；Validator 用 `TestValidate`/`TestValidateAsync`。
- 集成测试用 `CustomWebApplicationFactory`（`IClassFixture`）：`CreateAuthenticatedClient()` 得到带 JWT 的客户端，`CreateAuthenticatedClient(Roles.Administrator)` 带角色；用 `ReadEnvelopeAsync()`、`.Code()`、`.Data()` 读信封；断言业务码用 `ErrorCodes.*` 常量。
- 需要改配置用 `new CustomWebApplicationFactory().WithSettings(("Key","Value"))`；需要测试专用服务用 `.WithServices(services => ...)`；这类工厂自己 `Dispose`。
- 第三方命名空间（Moq、Shouldly、MediatR…）已在 `GlobalUsings.cs`，测试文件里不要再 `using`。
- 参考实现：`tests/Jex.UnitTests/Features/TodoLists/CreateTodoListCommandTests.cs`、`tests/Jex.IntegrationTests/Endpoints/TodoItemsEndpointsTests.cs`。
