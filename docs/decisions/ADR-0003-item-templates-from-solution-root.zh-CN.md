# ADR-0003 Item templates 在解决方案根目录运行

[English](ADR-0003-item-templates-from-solution-root.md) | **简体中文**

- 状态：已采纳
- 日期：2026-10-12

## 背景

一个功能切片要同时写 `src/*.Domain`、`src/*.Application`、`src/*.Infrastructure`、`src/*.WebAPI` 和 `tests/*`。`msbuild:RootNamespace` 绑定只在单个项目目录中有效。

## 决策

- Item template 从解决方案根目录运行，输出路径中的 `JexSolution` 由必填参数 `--solution-name` 替换（`jex scaffold` 从解决方案里的 `*.WebAPI` 项目推导出前缀并自动填入，在任意子目录运行都会切换到根目录）。
- 生成的代码必须立即通过 `jex check`（含格式检查与架构测试）：命令总有 Validator，Handler 是可编译的桩，测试是可通过的占位用例。
- 目标文件已存在时，`dotnet new` 默认拒绝覆盖（退出码 73），不加 `--force`。
- 用例模板的类型参数叫 `--usecase-type`，避开 `dotnet new` 自带的 `--type`。

## 后果

矩阵测试在每种组合中用被测的 jex 运行一次完整的 `jex scaffold feature`（entity → usecase ×2 → endpoint）再加两个用例，并验证第二次运行会失败。
