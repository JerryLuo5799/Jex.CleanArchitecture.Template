# ADR-0005 UnitOfWork 行为与领域事件

- 状态：已采纳
- 日期：2026-10-12

## 决策

- 每个 `ICommand` 由最内层的 `UnitOfWorkBehaviour` 包裹：开启事务 → 执行 Handler → 循环派发领域事件直到队列为空 → 提交。任何异常回滚并丢弃事件。
- 嵌套的 `ISender.Send` 加入同一事务（`Propagation.Required`）；只有最外层（事务拥有者）派发事件和提交，内层 scope 在 Dispose 时不得触碰事务。
- 查询（`IQuery<T>`）和标记 `[NonTransactional]` 的命令不开事务。`LoginCommand` 是 `[NonTransactional]`，因为失败计数必须在抛异常后仍然落库。
- 仓储在 Add/Update/Delete 时把实体放入 `PendingDomainEvents`；事件由 `DomainEventDispatcher` 通过 MediatR 发布。
- Store、仓储、种子代码不得直接用 `IFreeSql` 写数据，否则会脱离事务。

## 限制

事件处理器运行在命令事务内，**只能做数据库操作**。需要可靠地通知外部系统时使用 Outbox（本模板尚未内置，列入路线图）；架构测试禁止事件处理器注入 `HttpClient`。

## 验收

`TransactionRollbackTests`：两表写入后抛异常全部回滚；内层成功、外层失败全部回滚且事件未执行；事件处理器写入后失败全部回滚；成功路径三处写入全部提交。
