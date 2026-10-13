@AGENTS.md

## Claude Code 专属说明

- 规范全部在 `AGENTS.md`，上面已自动引入；不要在这里复制规则。
- 可用 skill：`/new-feature Order Orders`（脚手架 + 填充 + 测试）、`/review-architecture`（跑架构测试并对照 Must-Not 清单）。
- 每次改动结束前运行 `jex check`；失败就按架构测试的提示修，不要用 `#pragma` 或 `.editorconfig` 关掉规则。
- 不要创建总结性 Markdown 文件；改动说明写在提交信息里。
- 提交前不要 `git push`；推送由人来做。
