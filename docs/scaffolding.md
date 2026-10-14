# Scaffolding (item templates)

**English** | [简体中文](scaffolding.zh-CN.md)

Three item templates are installed together with the solution template and are called by `jex scaffold`: jex runs from any subdirectory of the project, switches to the solution root and derives `--solution-name` (the project prefix) from the `*.WebAPI` project in the solution. The generated code passes `jex check` straight away; when a file with the same name already exists the command fails (exit code 73) and never overwrites it.

## Generate a feature slice in one go

```bash
jex scaffold feature Order Orders
```

Runs, in order (stopping at the first failing step):

1. `jex-entity`: `Domain/Entities/Order.cs` (table `orders`), `IOrderRepository`, `OrderRepository`, `OrderConfiguration`, `Features/Orders/OrderDto.cs`, unit test
2. `jex-usecase` command: `Features/Orders/Commands/CreateOrder/CreateOrderCommand.cs` (+ Handler), `CreateOrderCommandValidator.cs`, unit test
3. `jex-usecase` query: `Features/Orders/Queries/GetOrderById/GetOrderByIdQuery.cs` (+ Handler, `long Id`), unit test
4. `jex-endpoint`: `WebAPI/Endpoints/Orders.cs` (`/api/orders`, mapping `GetOrderById` and `CreateOrder`), integration test

Then fill in the entity properties, handler logic and validation rules, and run `jex db script add-orders`.

## One piece at a time

| jex command | Equivalent command (at the solution root) |
|---|---|
| `jex scaffold entity Order Orders` | `dotnet new jex-entity -n Order --plural Orders --solution-name Acme -o .` |
| `jex scaffold usecase CancelOrder Orders --with-id` | `dotnet new jex-usecase -n CancelOrder --feature Orders --usecase-type command --return-type Unit --with-id --solution-name Acme -o .` |
| `jex scaffold usecase GetOrdersWithPagination Orders --type query --return OrderDto --paged` | `dotnet new jex-usecase -n GetOrdersWithPagination --feature Orders --usecase-type query --return-type OrderDto --paged --solution-name Acme -o .` |
| `jex scaffold usecase GetOrders Orders --type query --return "List<OrderDto>"` | `dotnet new jex-usecase -n GetOrders --feature Orders --usecase-type query --return-type "List<OrderDto>" --solution-name Acme -o .` |
| `jex scaffold endpoint Reports` | `dotnet new jex-endpoint -n Reports --solution-name Acme -o .` (empty endpoint group) |

### Common options of `jex scaffold`

| Option | Description |
|---|---|
| `--no-test` | Do not generate tests (`--include-test false` of the item template) |
| `--solution-name <Prefix>` | Set the project prefix and skip the derivation (for example when the `*.WebAPI` project was renamed) |
| `--api-project <path>` | Set the Web API project (when the solution has several or no `*.WebAPI.csproj`) |
| `--template-version <version>` | Check the installed templates against this version instead of `templateVersion` in `jex.json` |
| `--force` | Skip the template version check |

`jex scaffold` never installs or upgrades the template package (`dotnet new install` keeps only one version per machine, which affects every project). When the version in `jex.json` and the installed templates differ in major version it fails and offers two ways out; differences within the same major version only produce a warning.

### `jex-usecase` options

| Option | jex option | Default | Description |
|---|---|---|---|
| `--feature` / `-f` | second positional argument | required | Feature folder (plural); when it matches the entity's `--plural`, `{Entity}Dto` can be used directly |
| `--usecase-type` / `-ut` | `--type` | `command` | `command` generates Handler + Validator; `query` generates a Handler |
| `--return-type` / `-rt` | `--return` | `Unit` | Result type; `Unit` means no result (commands only) |
| `--with-id` / `-id` | `--with-id` | `false` | The request has a `long Id` parameter |
| `--paged` / `-pg` | `--paged` | `false` | Queries only: inherits `PagedQuery`, returns `PagedData<ReturnType>` and generates a `PagedQueryValidator` |
| `--include-test` / `-it` | `--no-test` | `true` | Generate a unit test |

### `jex-entity` options

| Option | jex option | Default | Description |
|---|---|---|---|
| `--plural` / `-p` | second positional argument | required | Feature folder and table name (`OrderLines` → `order_lines`) |
| `--include-test` / `-it` | `--no-test` | `true` | Generate a unit test |

### `jex-endpoint` options

| Option | jex option | Default | Description |
|---|---|---|---|
| `--entity` / `-e` | `--entity` | empty | When set, maps `Create{Entity}` and `Get{Entity}ById` and generates an integration test (generate those two use cases first) |
| `--include-test` / `-it` | `--no-test` | `true` | Generate an integration test (requires `--entity`) |

## Why run from the solution root

One call writes into several projects under `src/` and into `tests/`, so it cannot rely on the `msbuild:RootNamespace` binding (which only works inside a single project directory). See [ADR-0003](decisions/ADR-0003-item-templates-from-solution-root.md).
