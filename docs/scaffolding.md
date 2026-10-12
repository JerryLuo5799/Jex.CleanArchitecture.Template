# 脚手架（item templates）

三个 item template 随解决方案模板一起安装，**在生成项目的根目录运行**，并通过 `--solution-name` 指定解决方案名（Taskfile 自动填写）。生成的代码立即可以通过 `task check`；同名文件已存在时命令报错（退出码 73），不会覆盖。

## 一键生成功能切片

```bash
task scaffold:feature ENTITY=Order PLURAL=Orders
```

依次执行：

1. `jex-entity`：`Domain/Entities/Order.cs`（表 `orders`）、`IOrderRepository`、`OrderRepository`、`OrderConfiguration`、`Features/Orders/OrderDto.cs`、单测
2. `jex-usecase` command：`Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs`（+ Handler）、`CreateOrderCommandValidator.cs`、单测
3. `jex-usecase` query：`Features/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs`（+ Handler，`long Id`）、单测
4. `jex-endpoint`：`WebAPI/Endpoints/Orders.cs`（`/api/orders`，映射 `GetOrderById`、`CreateOrder`）、集成测试

之后填充实体属性、Handler 逻辑与校验规则，再 `task db:script NAME=add-orders`。

## 单独使用

| 任务 | 等价命令 |
|---|---|
| `task scaffold:entity ENTITY=Order PLURAL=Orders` | `dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .` |
| `task scaffold:usecase USECASE=CancelOrder FEATURE=Orders TYPE=command WITH_ID=true` | `dotnet new jex-usecase -n CancelOrder --feature Orders --usecase-type command --with-id --solution-name Acme -o .` |
| `task scaffold:usecase USECASE=GetOrders FEATURE=Orders TYPE=query RETURN=List<OrderDto>` | `dotnet new jex-usecase -n GetOrders --feature Orders --usecase-type query --return-type "List<OrderDto>" --solution-name Acme -o .` |
| `task scaffold:endpoint FEATURE=Reports` | `dotnet new jex-endpoint -n Reports --solution-name Acme -o .`（空端点组） |

### `jex-usecase` 参数

| 参数 | 默认 | 说明 |
|---|---|---|
| `--feature` / `-f` | 必填 | 功能目录（复数），与实体的 `--plural` 一致时可直接使用 `{Entity}Dto` |
| `--usecase-type` / `-ut` | `command` | `command` 生成 Handler + Validator；`query` 生成 Handler |
| `--return-type` / `-rt` | `Unit` | 结果类型；`Unit` 表示无返回（仅命令） |
| `--with-id` / `-id` | `false` | 请求带 `long Id` 参数 |
| `--include-test` / `-it` | `true` | 生成单元测试 |

### `jex-entity` 参数

| 参数 | 默认 | 说明 |
|---|---|---|
| `--plural` / `-p` | 必填 | 功能目录与表名（`OrderLines` → `order_lines`） |
| `--include-test` / `-it` | `true` | 生成单元测试 |

### `jex-endpoint` 参数

| 参数 | 默认 | 说明 |
|---|---|---|
| `--entity` / `-e` | 空 | 指定后映射 `Create{Entity}` 与 `Get{Entity}ById`，并生成集成测试（需先生成这两个用例） |
| `--include-test` / `-it` | `true` | 生成集成测试（需要 `--entity`） |

## 为什么在解决方案根目录运行

一次调用需要同时写入 `src/` 下多个项目和 `tests/`，因此不能依赖 `msbuild:RootNamespace` 绑定（只在单个项目目录内有效）。见 [ADR-0003](decisions/ADR-0003-item-templates-from-solution-root.md)。
