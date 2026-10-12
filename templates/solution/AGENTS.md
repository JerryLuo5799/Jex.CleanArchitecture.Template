# AGENTS.md — Jex 开发规范（AI 与人类共用）

本文件是本仓库**唯一的规范来源**。Claude Code、GitHub Copilot、Cursor 等工具都会读取它。
修改规范请改这里，不要在其它文件里复制规则。

## 1. 项目概览

| 项 | 说明 |
|---|---|
| 运行时 | .NET 10，C# latest，`net10.0` |
| Web | ASP.NET Core Minimal API，端点以 `IEndpointGroup` 分组（`src/Jex.WebAPI/Endpoints/`） |
| CQRS | MediatR 12（锁定，v13 起商业许可）：`ICommand` / `IQuery<T>` + 管道行为 |
| 校验 | FluentValidation，每个 Command 必须有一个 Validator |
| 数据 | FreeSql Code First + UnitOfWork；数据库 provider 见 `appsettings.json` 的 `Database:Provider` |
| 认证 | ASP.NET Core Identity（FreeSql Store）+ JWT Bearer；角色/策略基于 claims |
| 日志 | NLog（`appsettings.json` 的 `NLog` 节），请求日志记录业务码 |
| 测试 | xunit.v3 + Microsoft.Testing.Platform、Shouldly、Moq、NetArchTest |
| 本地地址 | `http://localhost:5208`，Scalar UI：`/scalar`，OpenAPI：`/openapi/v1.json` |

**完成标准只有一条命令：`task check`**（格式化 + 构建 + 全部测试）。没有 go-task 时等价于：
`dotnet format Jex.slnx && dotnet build Jex.slnx && dotnet test --solution Jex.slnx`。

## 2. 架构与依赖规则

```
Jex.Domain  ←  Jex.Application  ←  Jex.Infrastructure
                      ↑                      ↑
                   Jex.WebAPI ───────────────┘
```

- 只允许向内依赖。`Jex.Domain` 不引用 FreeSql/ASP.NET Core（Identity 实体基类是唯一例外，见 ADR-0007）；`Jex.Application` 不引用 FreeSql 与 ASP.NET Core。
- 规则由 `tests/Jex.ArchitectureTests` 强制执行，失败信息会直接告诉你怎么改。
- 第三方命名空间一律放在各项目的 `GlobalUsings.cs`；源文件只写 `System.*` 与本项目命名空间的 `using`。

## 3. 目录 → 产物映射（照着示例文件写）

| 产物 | 路径公式 | 示例（照抄这个） |
|---|---|---|
| 实体 | `src/Jex.Domain/Entities/{Entity}.cs` | `src/Jex.Domain/Entities/TodoItem.cs` |
| 领域事件 | `src/Jex.Domain/Events/{Entity}Events.cs` | `src/Jex.Domain/Events/TodoItemEvents.cs` |
| 枚举 / 常量 | `src/Jex.Domain/Enums/`、`src/Jex.Domain/Constants/` | `PriorityLevel.cs`、`Roles.cs` |
| 仓储接口 | `src/Jex.Application/Common/Interfaces/I{Entity}Repository.cs` | `ITodoListRepository.cs` |
| Command（含 Handler） | `src/Jex.Application/Features/{Feature}/Commands/{Verb}{Entity}/{Verb}{Entity}Command.cs` | `Features/TodoItems/Commands/CreateTodoItem/CreateTodoItemCommand.cs` |
| Validator | 同目录 `{Verb}{Entity}CommandValidator.cs`（或与 Command 同文件） | `Features/TodoLists/Commands/CreateTodoList/CreateTodoListCommandValidator.cs` |
| Query（含 Handler、Dto） | `src/Jex.Application/Features/{Feature}/Queries/{Name}/{Name}Query.cs` | `Features/TodoItems/Queries/GetTodoItemsWithPagination/GetTodoItemsWithPaginationQuery.cs` |
| 事件处理器 | `src/Jex.Application/Features/{Feature}/EventHandlers/{Event}Handler.cs` | `Features/TodoItems/EventHandlers/TodoItemCompletedEventHandler.cs` |
| 实体配置 | `src/Jex.Infrastructure/Persistence/Configurations/{Entity}Configuration.cs` | `TodoConfigurations.cs` |
| 仓储实现 | `src/Jex.Infrastructure/Persistence/Repositories/{Entity}Repository.cs` | `TodoListRepository.cs` |
| 端点组 | `src/Jex.WebAPI/Endpoints/{Feature}.cs` | `Endpoints/TodoItems.cs` |
| 单元测试 | `tests/Jex.UnitTests/Features/{Feature}/{Name}Tests.cs` | `Features/TodoLists/CreateTodoListCommandTests.cs` |
| 集成测试 | `tests/Jex.IntegrationTests/Endpoints/{Feature}EndpointsTests.cs` | `Endpoints/TodoItemsEndpointsTests.cs` |
| 迁移脚本 | `db/migrations/NNNN_name.sql`（由 `task db:script` 生成） | — |
| 架构决策 | `docs/decisions/ADR-NNNN-*.md` | `ADR-0005-unit-of-work-and-domain-events.md` |

## 4. 新增功能的标准流程（必须按顺序）

以新增 `Order` 聚合为例。脚手架命令可一步生成可编译的骨架，再填充业务逻辑。

1. **实体**：`task scaffold:entity ENTITY=Order PLURAL=Orders`
   → `Domain/Entities/Order.cs`（继承 `BaseAuditableEntity`；需要软删时实现 `ISoftDelete`）
   → `Application/Common/Interfaces/IOrderRepository.cs`、`Infrastructure/Persistence/Repositories/OrderRepository.cs`
   → `Infrastructure/Persistence/Configurations/OrderConfiguration.cs`（主键、长度、索引、导航）
2. **用例**：`task scaffold:usecase USECASE=CreateOrder FEATURE=Orders TYPE=command RETURN=long`，
   `task scaffold:usecase USECASE=GetOrderById FEATURE=Orders TYPE=query RETURN=OrderDto WITH_ID=true`
   → 填充 Handler 业务逻辑与 Validator 规则。需要登录的请求加 `[AuthorizeRequest]`。
3. **端点**：`task scaffold:endpoint FEATURE=Orders ENTITY=Order` → `WebAPI/Endpoints/Orders.cs`（映射 Create/GetById），
   显式声明 `RoutePrefix => "/orders"`，处理方法必须是**具名静态方法**（不能用 lambda）。
4. **测试**：Handler/Validator 单元测试；端点集成测试（用 `CustomWebApplicationFactory.CreateAuthenticatedClient()`）。
5. **迁移**：`task db:script NAME=add-orders` 生成 `db/migrations/000N_add-orders.sql`，审阅后提交。
6. **验收**：`task check` 必须通过。

一键完成 1–3：`task scaffold:feature ENTITY=Order PLURAL=Orders`。
等价的原生命令：`dotnet new TPLPREFIX-entity|TPLPREFIX-usecase|TPLPREFIX-endpoint`（在解决方案根目录运行，加 `--solution-name Jex`）。

## 5. 命名公式

| 类型 | 公式 | 示例 |
|---|---|---|
| Command | `{Verb}{Entity}Command`，实现 `ICommand` / `ICommand<T>` | `CreateTodoItemCommand : ICommand<long>` |
| Query | `Get{Entity}ByIdQuery`、`Get{Entities}Query`、`Get{Entities}WithPaginationQuery`，实现 `IQuery<T>` | `GetTodoItemsWithPaginationQuery` |
| Handler | `{Request}Handler`，`sealed` | `CreateTodoItemCommandHandler` |
| Validator | `{Request}Validator`，`sealed` | `CreateTodoItemCommandValidator` |
| Dto | `{Entity}Dto` / `{Entity}BriefDto`，带 `static From(entity)` | `TodoItemBriefDto.From` |
| 领域事件 | `{Entity}{PastTenseVerb}Event : BaseEvent` | `TodoItemCompletedEvent` |
| 端点组 | `{Feature}`（复数）: `IEndpointGroup`，端点名自动为 `{Group}_{Method}` | `TodoItems_CreateTodoItem` |
| 表名 | `[Table("snake_case_plural")]` | `todo_items` |
| 路由 | `/api/{feature-lower}`，由 `RoutePrefix` 显式声明 | `/api/todoitems` |
| 测试方法 | `{Method}_{Scenario}_{Expected}` | `Create_DuplicateTitle_Returns4000` |

## 6. Must / Must-Not

**Must**
- 写操作用 `ICommand`，读操作用 `IQuery<T>`；Handler 只依赖 `Application/Common/Interfaces` 里的接口。
- 每个 Command 恰好一个 `AbstractValidator`（可以为空）；校验规则只写在 Validator，不写在 Handler。
- 更新实体前必须**通过同一个仓储实例**加载（`GetByIdAsync`），这样只写脏列。
- 时间用注入的 `TimeProvider`；审计字段由仓储自动填充，Handler 不碰 `CreatedAt/LastModifiedAt`。
- 业务失败抛 `KnownException` 子类（`NotFoundException`、`ValidationException`、`ForbiddenAccessException`、`ConflictException`），API 层会转成信封。
- 新实体加 `{Entity}Configuration`（自动注册）；新仓储实现 `I{Entity}Repository`（自动注册）。
- 代码注释、日志、异常消息用英文；文档用中文。

**Must-Not**
- 不要新增 MVC Controller；不要在 Handler/端点里注入 `IFreeSql` 或 `IUnitOfWork`；不要在 Handler 里提交事务。
- 不要用 `DateTime.Now` / `DateTime.UtcNow`（分析器会报 RS0030）；不要引入 AutoMapper、MediatR ≥ 13、FluentAssertions ≥ 8。
- 不要启用 Minimal API 的 `AddValidation()`（会与 FluentValidation 形成两套校验）。
- 不要修改 `ErrorCodes` 中已有的值；新增错误码追加在对应区间末尾。
- 不要在领域事件处理器里调用外部系统（HTTP、邮件）；它们运行在命令事务内，只能做数据库操作（ADR-0005）。
- 不要在生产环境开启 `Database:AutoSyncStructure`；生产只跑已审阅的迁移脚本（ADR-0008）。
- 不要在源文件里 `using` 第三方命名空间（放 `GlobalUsings.cs`），否则 import 排序会随项目名变化。

## 7. 不需要手动注册的东西

以下全部通过程序集扫描自动注册，**不要**再去 `DependencyInjection.cs` 里加代码：

- MediatR Handler、`INotificationHandler`、FluentValidation Validator（`Jex.Application`）
- `IEndpointGroup` 实现（`MapEndpoints` 反射发现）
- `I{Entity}Repository` 实现（Infrastructure 中以 `Repository` 结尾的类按接口注册）
- `IEntityConfiguration` 实现（schema 同步、迁移、测试都从它推导实体列表）

需要手动注册的只有：新的 Options 类（`AddOptions<T>().BindConfiguration().ValidateOnStart()`）和新的基础设施服务。

## 8. 响应契约与错误码

所有 `/api/**` 响应都是 **HTTP 200 + 信封**：

```json
{ "requestId": "trace-id", "code": 0, "msg": "", "data": {}, "totalCount": 42, "errors": { "Email": ["..."] } }
```

| 情况 | code | 处理入口 |
|---|---|---|
| 成功 | `0` | 端点返回 `ApiResponse.Success(data[, totalCount])` |
| 校验失败 / 参数绑定失败 / JSON 格式错误 / Content-Type 错误 | `4000` | `ValidationBehaviour`、`ApiExceptionHandler`、`StatusCodePages` |
| 未认证（无/无效 token，`[AuthorizeRequest]` 匿名调用） | `4010` | `EnvelopeAuthorizationResultHandler`、`AuthorizationBehaviour` |
| 已认证但无权限（角色/策略不满足） | `4030` | 同上 |
| 资源或路由不存在 | `4040` | `NotFoundException`、`StatusCodePages` |
| HTTP 方法不支持 | `4050` | `StatusCodePages` |
| 冲突（并发戳过期、重复键） | `4090` | `ConflictException` |
| 请求过于频繁（带 `Retry-After` 头） | `4290` | 限流 `OnRejected` |
| 未预期异常（非 Development 隐藏详情） | `5000` | `ApiExceptionHandler` |

- 信封只用于业务 API。`/health*` 返回真实 HTTP 状态码（探针语义），`/openapi`、`/scalar` 不套信封。
- 分页：端点把 `PagedData<T>` 拆成 `data`（本页）+ `totalCount`，对外只有这一种分页形状。
- 校验消息固定英文（`CultureInfo.InvariantCulture`），需要本地化时在前端或网关处理。
- 错误码定义在 `src/Jex.Application/Common/ErrorCodes.cs`。

## 9. 测试约定

| 项目 | 职责 | 依赖 |
|---|---|---|
| `Jex.UnitTests` | Handler、Validator、Behaviour、实体行为；Moq 模拟接口 | 无 I/O |
| `Jex.IntegrationTests` | 通过 HTTP 调端点、仓储/事务/迁移行为 | 每个工厂一个独立 SQLite 临时库，无需 Docker |
| `Jex.ArchitectureTests` | 分层与命名规则、端点约定、文档与 Taskfile 一致性 | 反射 |

- 集成测试用 `CustomWebApplicationFactory`：`CreateClient()` 匿名，`CreateAuthenticatedClient(roles...)` 自签 JWT；`WithSettings(...)` 覆盖配置，`WithServices(...)` 注册测试专用服务。
- 读响应用 `ReadEnvelopeAsync()` / `.Code()` / `.Data()`（`ApiClientExtensions`）。
- 新功能至少：1 个 Handler 单测、1 个 Validator 单测、1 个端点集成测试。
- 架构测试必须通过；它的失败信息就是修复指引。

## 10. 命令速查

| 目的 | task | 原生命令 |
|---|---|---|
| 验收（格式+构建+测试） | `task check` | `dotnet format Jex.slnx && dotnet build Jex.slnx && dotnet test --solution Jex.slnx` |
| CI 同等严格 | `task check:ci` | `dotnet build Jex.slnx -c Release -p:ContinuousIntegrationBuild=true` |
| 只跑单元测试 | `task test:unit` | `dotnet test --project tests/Jex.UnitTests/Jex.UnitTests.csproj` |
| 运行 API | `task run` | `dotnet run --project src/Jex.WebAPI` |
| 热重载 | `task watch` | `dotnet watch --project src/Jex.WebAPI` |
| 重置本地库 | `task db:reset` | 删除 `src/Jex.WebAPI/*.db` |
| 生成迁移脚本 | `task db:script NAME=add-orders` | `dotnet run --project src/Jex.WebAPI -- db-script add-orders db/migrations` |
| 执行迁移 | `task db:migrate` | `dotnet run --project src/Jex.WebAPI -- migrate` |
| 脚手架 | `task scaffold:feature ENTITY=Order PLURAL=Orders` | `dotnet new TPLPREFIX-entity ...`（见 §4） |
<!--#if (IncludeDocker) -->
| 本地基础设施 | `task infra:up` / `task infra:down` | `docker compose --profile DB_PROFILE up -d` |
<!--#endif -->
<!--#if (UseAspire) -->
| Aspire 编排运行 | `task run:aspire` | `dotnet run --project src/Jex.AppHost` |
<!--#endif -->

开发环境首次运行会自动建表并种子化 `admin@example.com / Admin123!`（`appsettings.Development.json` 的 `Seed:Admin`）。

## 11. 常见失败与修复

| 现象 | 原因 | 修复 |
|---|---|---|
| 架构测试 `EveryCommand_HasExactlyOneValidator` 失败 | 新 Command 没有 Validator | 同目录新增 `sealed class XxxCommandValidator : AbstractValidator<XxxCommand>` |
| `Requests_ImplementCommandOrQuery_NotRawIRequest` 失败 | 直接实现了 `IRequest` | 改为 `ICommand` / `ICommand<T>` / `IQuery<T>` |
| `EndpointGroups_DeclareRoutePrefixUnderApi` 失败 | 端点组缺少 `RoutePrefix` 或带了 `/api` | `public static string RoutePrefix => "/orders";` |
| 启动抛 `ArgumentException: Endpoint handlers must be named static methods` | 用了 lambda | 改成端点组里的私有静态方法 |
| `EveryEntityWithATable_HasAnEntityConfiguration` 失败 | 新实体没有配置类 | 新增 `OrderConfiguration : EntityConfiguration<Order>` |
| 编译报 RS0030 | 用了 `DateTime.Now` 等被禁 API | 注入 `TimeProvider` |
| `dotnet format --verify-no-changes` 报 IMPORTS | 源文件里 `using` 了第三方命名空间 | 移到项目的 `GlobalUsings.cs` |
| 更新实体后所有列都被写了一遍 | 实体不是从同一个仓储加载的 | 先 `GetByIdAsync` 再改字段再 `UpdateAsync` |
| 登录返回 4010 但密码正确 | 账户被锁定（5 次失败锁 15 分钟）或 `Status != Active` | 等待或在开发库里重置 |
| `docker compose` / `dotnet run` 报端口 "access permissions" 或 10013 | Windows 把该端口划入了 Hyper-V/WSL 保留范围（`netsh interface ipv4 show excludedportrange protocol=tcp`） | 在 `.env` 改 `POSTGRES_PORT` 等端口并同步连接串；API 端口改 `launchSettings.json` |

## 12. 架构决策记录（ADR）

改变以下决策前，先在 `docs/decisions/` 新增一条 ADR 并在 PR 中引用：

- [ADR-0001 分层与依赖规则](docs/decisions/ADR-0001-clean-architecture-layers.md)
- [ADR-0002 选择 FreeSql 而非 EF Core](docs/decisions/ADR-0002-freesql-not-efcore.md)
- [ADR-0003 HTTP 200 + 信封的响应契约](docs/decisions/ADR-0003-http200-envelope.md)
- [ADR-0004 Minimal API 端点组](docs/decisions/ADR-0004-minimal-api-endpoint-groups.md)
- [ADR-0005 UnitOfWork 行为与领域事件](docs/decisions/ADR-0005-unit-of-work-and-domain-events.md)
- [ADR-0006 FluentValidation](docs/decisions/ADR-0006-fluentvalidation.md)
- [ADR-0007 User 实体位于 Domain](docs/decisions/ADR-0007-identity-user-in-domain.md)
- [ADR-0008 生产环境迁移策略](docs/decisions/ADR-0008-migrations.md)
- [ADR-0009 DTO 映射约定](docs/decisions/ADR-0009-dto-mapping.md)
