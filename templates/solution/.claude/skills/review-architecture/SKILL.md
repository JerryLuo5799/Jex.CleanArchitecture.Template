---
name: review-architecture
description: 审查当前改动是否符合 AGENTS.md 的架构与命名规则：运行架构测试、对照 Must-Not 清单、检查错误码与响应契约。用法：/review-architecture
---

# 架构审查

1. 运行 `task test:arch`，把每条失败原样列出（失败信息就是修复指引）。
2. `git diff --name-only` 列出改动文件，逐个对照 `AGENTS.md` §6 的 Must-Not：
   - Handler/端点是否注入了 `IFreeSql`、`IUnitOfWork`；是否在 Handler 里提交事务
   - 是否出现 `DateTime.Now/UtcNow`、AutoMapper、Controller、`AddValidation()`
   - 是否修改了 `ErrorCodes` 已有值；新异常是否继承 `KnownException`
   - 领域事件处理器是否调用了外部系统
   - 源文件里是否 `using` 了第三方命名空间（应在 `GlobalUsings.cs`）
3. 检查新端点：`RoutePrefix` 显式、处理方法具名静态、分页返回 `totalCount`。
4. 检查测试：每个新 Command 有 Validator 单测，每个新端点有集成测试。
5. 输出一份简短清单：通过项 / 需修改项（文件:行 + 修法）。不要自行修改代码，除非用户要求。
