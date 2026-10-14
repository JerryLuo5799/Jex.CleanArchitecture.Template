# 脚手架（item templates）

[English](scaffolding.md) | **简体中文**

三个 item template 随解决方案模板一起安装，由 `jex scaffold` 调用：jex 在项目任意子目录都能运行，会切换到解决方案根目录，并从解决方案中的 `*.WebAPI` 项目推导出 `--solution-name`（项目前缀）。生成的代码立即可以通过 `jex check`；同名文件已存在时命令报错（退出码 73），不会覆盖。

## 一键生成功能切片

```bash
jex scaffold feature Order Orders
```

依次执行（任一步失败即停止）：

1. `jex-entity`：`Domain/Entities/Order.cs`（表 `orders`）、`IOrderRepository`、`OrderRepository`、`OrderConfiguration`、`Features/Orders/OrderDto.cs`、单测
2. `jex-usecase` command：`Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs`（+ Handler）、`CreateOrderCommandValidator.cs`、单测
3. `jex-usecase` query：`Features/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs`（+ Handler，`long Id`）、单测
4. `jex-endpoint`：`WebAPI/Endpoints/Orders.cs`（`/api/orders`，映射 `GetOrderById`、`CreateOrder`）、集成测试

之后填充实体属性、Handler 逻辑与校验规则，再 `jex db script add-orders`。

## 单独使用

| jex 命令 | 等价命令（在解决方案根目录） |
|---|---|
| `jex scaffold entity Order Orders` | `dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .` |
| `jex scaffold usecase CancelOrder Orders --with-id` | `dotnet new jex-usecase -n CancelOrder --feature Orders --usecase-type command --return-type Unit --with-id --solution-name Acme -o .` |
| `jex scaffold usecase GetOrdersWithPagination Orders --type query --return OrderDto --paged` | `dotnet new jex-usecase -n GetOrdersWithPagination --feature Orders --usecase-type query --return-type OrderDto --paged --solution-name Acme -o .` |
| `jex scaffold usecase GetOrders Orders --type query --return "List<OrderDto>"` | `dotnet new jex-usecase -n GetOrders --feature Orders --usecase-type query --return-type "List<OrderDto>" --solution-name Acme -o .` |
| `jex scaffold endpoint Reports` | `dotnet new jex-endpoint -n Reports --solution-name Acme -o .`（空端点组） |

### `jex scaffold` 的通用选项

| 选项 | 说明 |
|---|---|
| `--no-test` | 不生成测试（item template 的 `--include-test false`） |
| `--solution-name <Prefix>` | 指定项目前缀，跳过推导（例如 `*.WebAPI` 项目改过名） |
| `--api-project <path>` | 指定 Web API 项目（解决方案里有多个或没有 `*.WebAPI.csproj` 时） |
| `--template-version <版本>` | 按这个版本核对已安装的模板，代替 `jex.json` 的 `templateVersion` |
| `--force` | 跳过模板版本核对 |

`jex scaffold` 从不安装或升级模板包（`dotnet new install` 每台机器只保留一个版本，会影响所有项目）。`jex.json` 的版本与已安装模板主版本不同时报错并给出两条出路；同一主版本内的差异只警告。

### `jex-usecase` 参数

| 参数 | jex 选项 | 默认 | 说明 |
|---|---|---|---|
| `--feature` / `-f` | 第二个位置参数 | 必填 | 功能目录（复数），与实体的 `--plural` 一致时可直接使用 `{Entity}Dto` |
| `--usecase-type` / `-ut` | `--type` | `command` | `command` 生成 Handler + Validator；`query` 生成 Handler |
| `--return-type` / `-rt` | `--return` | `Unit` | 结果类型；`Unit` 表示无返回（仅命令） |
| `--with-id` / `-id` | `--with-id` | `false` | 请求带 `long Id` 参数 |
| `--paged` / `-pg` | `--paged` | `false` | 仅查询：继承 `PagedQuery`，返回 `PagedData<ReturnType>`，并生成 `PagedQueryValidator` |
| `--include-test` / `-it` | `--no-test` | `true` | 生成单元测试 |

### `jex-entity` 参数

| 参数 | jex 选项 | 默认 | 说明 |
|---|---|---|---|
| `--plural` / `-p` | 第二个位置参数 | 必填 | 功能目录与表名（`OrderLines` → `order_lines`） |
| `--include-test` / `-it` | `--no-test` | `true` | 生成单元测试 |

### `jex-endpoint` 参数

| 参数 | jex 选项 | 默认 | 说明 |
|---|---|---|---|
| `--entity` / `-e` | `--entity` | 空 | 指定后映射 `Create{Entity}` 与 `Get{Entity}ById`，并生成集成测试（需先生成这两个用例） |
| `--include-test` / `-it` | `--no-test` | `true` | 生成集成测试（需要 `--entity`） |

## 为什么在解决方案根目录运行

一次调用需要同时写入 `src/` 下多个项目和 `tests/`，因此不能依赖 `msbuild:RootNamespace` 绑定（只在单个项目目录内有效）。见 [ADR-0003](decisions/ADR-0003-item-templates-from-solution-root.zh-CN.md)。
