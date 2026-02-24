# .NET 安装记录（Ubuntu 24.04）

已在当前环境安装 .NET SDK 8.0（用于后续 CLI 构建/工具链）。

## 安装命令

```bash
apt-get update
apt-get install -y wget gpg apt-transport-https
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
apt-get update
apt-get install -y dotnet-sdk-8.0
```

## 验证结果

```bash
dotnet --list-sdks
# 8.0.124 [/usr/lib/dotnet/sdk]
```

## 说明

- 当前仓库的 `Client.App` 为 WPF（`Microsoft.NET.Sdk.WindowsDesktop`），该 SDK 仅在 Windows 环境可用。
- 因此 Linux 环境下可使用 dotnet 完成 Domain/Infrastructure 项目构建，但无法完整构建 WPF 项目。
