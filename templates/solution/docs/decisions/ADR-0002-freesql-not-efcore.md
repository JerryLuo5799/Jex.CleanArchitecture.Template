# ADR-0002 选择 FreeSql 而非 EF Core

- 状态：已采纳
- 日期：2026-10-12

## 背景

团队熟悉 FreeSql，需要轻量的 Code First、多数据库支持（SQLite / PostgreSQL / SQL Server / MySQL）与简单直观的查询 API。

## 决策

数据访问使用 FreeSql（`FreeSql` + `FreeSql.DbContext`）：

- 单例 `IFreeSql`，事务通过 scoped `UnitOfWorkManager`（ADR-0005）。
- 仓储基于 `DefaultRepository<T, long>`：加载即跟踪，更新只写脏列。
- 实体配置放在 `Infrastructure/Persistence/Configurations`，通过 `IEntityConfiguration` 自动发现。
- 开发环境 `AutoSyncStructure` 同步 schema；生产环境只执行版本化迁移（ADR-0008）。

## 取舍

- FreeSql 没有 EF Core 的迁移工具链，因此自建 `db-script` / `migrate` 命令（DbUp）。
- `DateTimeOffset` 在 SQLite 下不映射，Identity 的 `LockoutEnd` 以 `LockoutEndUtc` 列持久化。
- 不使用 EF Core 的生态包（如 Aspire 的 EF 集成）。
- 时间：所有时间字段是 UTC `DateTime`，在每种数据库中都存为不带时区的类型；读取时统一恢复为 `DateTimeKind.Utc`。PostgreSQL 为此开启 `Npgsql.EnableLegacyTimestampBehavior`（见 `FreeSqlFactory`），列类型为 `timestamp`。
