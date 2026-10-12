# ADR-0003 Item templates 在解决方案根目录运行

- 状态：已采纳
- 日期：2026-10-12

## 背景

一个功能切片要同时写 `src/*.Domain`、`src/*.Application`、`src/*.Infrastructure`、`src/*.WebAPI` 和 `tests/*`。`msbuild:RootNamespace` 绑定只在单个项目目录中有效。

## 决策

- Item template 从解决方案根目录运行，输出路径中的 `JexSolution` 由必填参数 `--solution-name` 替换（生成项目的 Taskfile 自动填入自己的名字）。
- 生成的代码必须立即通过 `task check`（含格式检查与架构测试）：命令总有 Validator，Handler 是可编译的桩，测试是可通过的占位用例。
- 目标文件已存在时，`dotnet new` 默认拒绝覆盖（退出码 73），不加 `--force`。
- 用例模板的类型参数叫 `--usecase-type`，避开 `dotnet new` 自带的 `--type`。

## 后果

矩阵测试在每种组合中运行一次完整的 `entity → usecase ×2 → endpoint`，并验证第二次运行会失败。
