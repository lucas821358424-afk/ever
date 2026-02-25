# EverInspection Client

WPF + .NET Framework 4.6 的制造点检客户端示例实现，支持：

- 账号登录（首次在线 + 离线登录）
- 模板驱动动态表单
- 本地 SQLite 草稿/提交/上传状态管理
- 点检值实时校验与超规格标红
- 历史查询

## 目录

- `EverInspection.sln`
- `src/EverInspection.Client`
  - `Models` 领域对象
  - `Services` 认证、模板、权限、表单、同步服务
  - `Data` SQLite 初始化
  - `ViewModels` MVVM 逻辑
  - `Views` WPF 页面

## 本地运行

1. 在 Windows + Visual Studio 2019/2022 环境打开 `EverInspection.sln`。
2. NuGet 还原：`Newtonsoft.Json`、`Stub.System.Data.SQLite.Core.NetFramework`。
3. 启动项目。
4. 若提示找不到 `System.Data.SQLite`，请在解决方案根目录执行 `nuget restore EverInspection.sln` 后重新生成。
5. 若报错 `无法加载 DLL sqlite.interop.dll`，请先清理 `bin/obj` 后重新生成，并确认输出目录下存在 `x86\SQLite.Interop.dll` 与 `x64\SQLite.Interop.dll`。

默认测试账号：`operator01 / 123456`。

## 本地数据库位置

- 默认路径：`%LOCALAPPDATA%\EverInspection\ever-inspection.db`
- 可通过环境变量 `EVER_INSPECTION_DB_PATH` 指定完整数据库文件路径（例如 `D:\Ever\ever-inspection.db`）。

## 模板配置与联机更新

- 本地 SQLite 新增模板配置表：`template_header_configs`、`template_row_configs`，用于存储每个表单版本的表头字段与点检行配置。
- `Rows` 配置支持按制程自动分组显示（同制程连续行自动空白显示，形成合并单元格视觉效果）。
- 支持按模板开关控制 `Panel ID` 是否显示/必填；点检日期和时间自动带入当前值。
- 有网场景可通过 `SyncService.SyncTemplateConfigs(true)` 触发模板配置更新（当前示例从 `Resources/oracle-template-sync.json` 读取，模拟 Oracle 服务端下发）。
