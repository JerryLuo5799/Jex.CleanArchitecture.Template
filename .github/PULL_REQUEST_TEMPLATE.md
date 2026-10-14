## What changed / 改动说明

<!-- What changed and why. / 改了什么、为什么。 -->

## Checklist / 检查清单

- [ ] `jex dev check --ci` passes (template source); when `src/Jex.Cli` changed, `dotnet test --project src/Jex.Cli.Tests/Jex.Cli.Tests.csproj` passes / `jex dev check --ci` 通过（模板源码）；改动 `src/Jex.Cli` 时 `dotnet test --project src/Jex.Cli.Tests/Jex.Cli.Tests.csproj` 通过
- [ ] When the change affects generated output, `jex dev matrix` passes (or passes in the PR's template-tests) / 改动影响生成结果时，`jex dev matrix` 通过（或在 PR 的 template-tests 中通过）
- [ ] New template placeholders are added to the leftover check in `scripts/assert-structure.cs` / 新的模板占位符已加入 `scripts/assert-structure.cs` 的残留检查
- [ ] Template C# uses no native `#if DEBUG` and no repository URL; third-party namespaces are in `GlobalUsings.cs` / 模板内 C# 未使用原生 `#if DEBUG`，未写入仓库 URL，第三方命名空间放在 `GlobalUsings.cs`
- [ ] The generated project's `AGENTS.md` / ADRs are updated (if conventions changed) / 生成项目的 `AGENTS.md` / ADR 已同步更新（若规范有变）
- [ ] `[Unreleased]` in `CHANGELOG.md` and `CHANGELOG.zh-CN.md` is updated / `CHANGELOG.md` 与 `CHANGELOG.zh-CN.md` 的 `[Unreleased]` 已更新
- [ ] Changed docs are updated in both English and Chinese / 改动的文档已同时更新英文与中文版本
