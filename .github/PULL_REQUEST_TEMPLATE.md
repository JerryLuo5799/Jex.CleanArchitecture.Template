## 改动说明

<!-- 改了什么、为什么。 -->

## 检查清单

- [ ] `jex dev check --ci` 通过（模板源码）；改动 `src/Jex.Cli` 时 `dotnet test --project src/Jex.Cli.Tests/Jex.Cli.Tests.csproj` 通过
- [ ] 改动影响生成结果时，`jex dev matrix` 通过（或在 PR 的 template-tests 中通过）
- [ ] 新的模板占位符已加入 `scripts/assert-structure.cs` 的残留检查
- [ ] 模板内 C# 未使用原生 `#if DEBUG`，未写入仓库 URL，第三方命名空间放在 `GlobalUsings.cs`
- [ ] 生成项目的 `AGENTS.md` / ADR 已同步更新（若规范有变）
- [ ] `CHANGELOG.md` 的 `[Unreleased]` 已更新
