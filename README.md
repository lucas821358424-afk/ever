# Windows Pad 离线电子表单客户端（WPF）

本项目现已提供**可运行代码骨架**（.NET Framework 4.8 + WPF + SQLite），用于工业电子点检表单离线客户端开发。

> 运行目标已调整为 **Windows 7 SP1 及以上**（建议 Win10/Win11 作为开发环境，Win7 作为现场终端运行环境）。

## 当前已实现

- WPF 客户端基础框架（`Client.App`）
- 动态字段渲染（Canvas 绝对定位）
- 字段类型示例：Text / Number / Date / Select / Checkbox / Label
- 基础校验（必填、数值最小/最大、正则）
- 校验报警（输入框标红 + 弹窗）
- SQLite 初始化与模板保存/读取（`Client.Infrastructure`）
- SQL 初始化脚本（`sql/init.sql`）

## 项目结构

- `src/Client.App`：WPF UI 与动态渲染
- `src/Client.Domain`：领域模型与校验规则
- `src/Client.Infrastructure`：SQLite 持久化
- `sql/init.sql`：数据库脚本
- `docs/solution-architecture.md`：完整架构设计说明

## 使用说明

1. 在 Windows 环境使用 Visual Studio 2022 或 `dotnet` 打开 `Ever.Client.sln`。
2. 运行 `Client.App`。
3. 点击“加载示例模板”后进行填写，再点击“校验”查看报警效果。


## Windows 构建（推荐）

由于 `Client.App` 使用 WPF（`Microsoft.NET.Sdk.WindowsDesktop`），请在 Windows 环境构建。

- 运行环境：Windows 7 SP1 / Windows 10 / Windows 11
- 目标框架：.NET Framework 4.8（Win7 端请预装 .NET Framework 4.8 Runtime）

- 本地构建脚本：`scripts/windows/build.ps1`
- Windows CI：`.github/workflows/windows-build.yml`
- 详细步骤：`docs/windows-build.md`


## Excel模板导入（两步法）

- 入口按钮：`导入Excel模板`
- 流程：Excel -> TemplateMeta解析 -> FormTemplate入库 -> Canvas动态渲染
- 规范说明：`docs/excel-template-two-step.md`
