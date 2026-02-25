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
2. NuGet 还原：`Newtonsoft.Json`、`System.Data.SQLite.Core`。
3. 启动项目。

默认测试账号：`operator01 / 123456`。
