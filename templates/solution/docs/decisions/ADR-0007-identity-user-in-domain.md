# ADR-0007 User 实体位于 Domain

- 状态：已采纳
- 日期：2026-10-12

## 背景

ASP.NET Core Identity 需要用户/角色类型继承 `IdentityUser<TKey>` / `IdentityRole<TKey>`。把它们放在 Domain 可以让领域代码直接引用用户，但引入了对 `Microsoft.AspNetCore.Identity` 命名空间的依赖。

## 决策

- `User`、`ApplicationRole`、`UserRole` 位于 `Jex.Domain.Entities`，仅引用 `Microsoft.Extensions.Identity.Stores` 包（不是 ASP.NET Core 框架引用）。
- 架构测试只禁止 Domain/Application 依赖 Http、Mvc、Builder、Authorization 等命名空间，`Microsoft.AspNetCore.Identity` 的实体基类是明确的例外。
- 身份字段（Email/UserName/Status/密码）的修改必须经由 `IIdentityService`（内部走 `UserManager`），以保证规范化列、安全戳与锁定状态一致。

## 后果

Domain 不再是"零依赖"，但依赖范围被架构测试锁定在实体基类上。
