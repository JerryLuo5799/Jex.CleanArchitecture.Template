# ADR-0009 DTO 映射约定

- 状态：已采纳
- 日期：2026-10-12

## 决策

- 不引入 AutoMapper（v15 起商业许可）或其它映射库；`BannedSymbols.txt` 禁止 `AutoMapper` 命名空间。
- 每个 DTO 是 `sealed record`，旁边带一个 `static {Dto} From({Entity} entity)`；列表用 `.Select(Dto.From)`，分页用 `PagedData<T>.Map(Dto.From)`。
- 只读查询可以直接用 FreeSql 投影（`ToListAsync<Dto>()`）避免加载完整实体。
- DTO 不暴露密码哈希、安全戳、锁定状态等内部字段。

## 后果

映射代码显式、可搜索、可单元测试；新增字段时编译器会提示所有需要更新的 `From`。
