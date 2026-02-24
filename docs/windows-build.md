# Windows 环境构建指南

本项目的 UI 使用 WPF（`Microsoft.NET.Sdk.WindowsDesktop`），**必须在 Windows 环境构建/运行**。

当前发布目标支持 **Windows 7 SP1**（部署运行）到 Windows 11。

## 1. 环境要求

- 构建机：Windows 10 / Windows 11
- 运行机：Windows 7 SP1 / Windows 10 / Windows 11
- .NET Framework 4.8 Developer Pack（构建）
- .NET Framework 4.8 Runtime（运行，Win7 需预装）
- Visual Studio 2019/2022（推荐安装“桌面开发 - .NET”工作负载）

## 2. 命令行构建

在 PowerShell 中执行：

```powershell
cd <repo-root>
./scripts/windows/build.ps1 -Configuration Release
```

## 3. Visual Studio 构建

1. 打开 `Ever.Client.sln`
2. 选择 `Client.App` 作为启动项目
3. 选择 `Debug` 或 `Release`
4. 运行（F5）

## 4. 持续集成（Windows）

仓库提供了 GitHub Actions 工作流：

- `.github/workflows/windows-build.yml`

该工作流会在 `windows-latest` 上执行 restore + build，确保 WPF 项目持续可构建；产物可部署到 Win7 SP1（安装 .NET Framework 4.8 Runtime 后）。
