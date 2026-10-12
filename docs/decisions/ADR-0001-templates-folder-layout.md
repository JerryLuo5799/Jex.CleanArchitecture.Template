# ADR-0001 templates/ 目录布局

- 状态：已采纳
- 日期：2026-10-12

## 背景

旧布局把模板内容（src、tests、slnx）和打包项目放在仓库根目录：模板自己的 README 被打进生成项目，`.github`、`.gitignore` 等点文件没有打包，`sourceName` 还会改写仓库级文件名。

## 决策

- 仓库根目录只放工具链：打包项目、Taskfile、脚本、CI、仓库文档。
- `templates/solution/` 是解决方案模板，本身是可构建、可测试的解决方案；它的 `README.md` 就是生成项目的 README。
- `templates/items/{entity,usecase,endpoint}/` 是 item templates。
- 打包项目以 `templates/**` 为内容，`NoDefaultExcludes=true` 保留点文件；一个 nupkg 包含 4 个模板。
- 开发时 `dotnet new install ./templates` 按目录安装全部模板；发布验收走 nupkg（`task init:pkg`、CI 矩阵）。

## 后果

- 模板源码的改动可以直接 `task check:ci`，生成结果由矩阵验证。
- 卸载：目录安装按路径（`dotnet new uninstall ./templates`），包安装按 PackageId。
