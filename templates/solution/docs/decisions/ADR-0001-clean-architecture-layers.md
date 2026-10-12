# ADR-0001 分层与依赖规则

- 状态：已采纳
- 日期：2026-10-12

## 背景

需要一个团队与 AI 都能稳定遵循的结构：业务规则与框架、数据库解耦，变更影响范围可预期。

## 决策

采用四层 Clean Architecture：

```
Jex.Domain ← Jex.Application ← Jex.Infrastructure
                   ↑                   ↑
                Jex.WebAPI ────────────┘
```

- Domain：实体、枚举、常量、领域事件基类。不依赖任何框架（Identity 实体基类例外，见 ADR-0007）。
- Application：用例（Command/Query + Handler）、校验、管道行为、接口（仓储、身份、当前用户、工作单元）。不依赖 FreeSql、ASP.NET Core。
- Infrastructure：FreeSql 实现、Identity Store、迁移、健康检查。
- WebAPI：端点、信封、认证/授权、启动配置。

规则由 `tests/Jex.ArchitectureTests/LayerDependencyTests.cs` 强制执行。

## 后果

- 新功能按 `AGENTS.md` §4 的顺序自内向外实现。
- 跨层需求通过 Application 的接口表达，由 Infrastructure 实现并按约定自动注册。
