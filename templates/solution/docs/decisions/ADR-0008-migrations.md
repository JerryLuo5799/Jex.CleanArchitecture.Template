# ADR-0008 生产环境迁移策略

- 状态：已采纳
- 日期：2026-10-12

## 决策

- **生产禁止运行时自动同步 schema**（`Database:AutoSyncStructure` 必须为 false）。只允许执行已评审、已提交的版本化 SQL 脚本。
- 脚本位于 `db/migrations/NNNN_name.sql`，嵌入到 `Jex.WebAPI` 程序集，由 DbUp 按名称顺序执行，并记录到 `SchemaVersions` 表；重复执行是无操作。
- 执行方式：部署流水线中的独立步骤 `dotnet run --project src/Jex.WebAPI -- migrate`（`task db:migrate`）。`Migration:RunOnStartup=true` 仅适合单实例部署。
- 生成方式：`task db:script NAME=xxx` 创建**独立基准库**（SQLite 为临时文件，其它 provider 用 `Migration:BaselineConnection` 指向的空库），只应用已提交的脚本，再用 FreeSql 比较实体模型与基准库生成差异 DDL。不以开发库为基准，因为开发库已被自动同步，比较结果总是空。
- 事务：SQLite / PostgreSQL / SQL Server 每个脚本一个事务；MySQL 的 DDL 不可回滚，脚本必须可重入（`IF NOT EXISTS` 等）并在失败后手工处理。
- 多实例互斥：由部署流水线串行执行迁移；`SchemaVersions` 不是分布式锁。

## 验收

`MigrationTests`：空库生成初始脚本 → 应用到新库 → 再执行无操作 → 模型与库一致（不再生成脚本）。
