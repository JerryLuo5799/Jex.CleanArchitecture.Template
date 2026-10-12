# ADR-0006 FluentValidation

- 状态：已采纳
- 日期：2026-10-12

## 决策

- 输入校验统一用 FluentValidation：每个 Command 恰有一个 `AbstractValidator`（可为空），由程序集扫描注册，`ValidationBehaviour` 在 Handler 之前执行。
- 需要数据库的规则（如标题唯一）在 Validator 中注入仓储接口并用 `MustAsync`。
- 校验消息固定英文（`InvariantCulture`）；本地化放在前端或网关。
- 不启用 Minimal API 内置的 `AddValidation()`，避免两套校验。
- 校验失败以 `ValidationException`（4000）返回，`errors` 字典按成员分组。

## 后果

架构测试 `EveryCommand_HasExactlyOneValidator` 强制执行；脚手架在生成 Command 时总会生成 Validator。
