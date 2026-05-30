# DeepSeek 用量仪表盘

一个 Windows 桌面小工具，在通知栏即可查看 DeepSeek API 的余额、用量和费用，无需打开网页。

## 功能

- **余额查看** — 总余额、充值余额、赠送余额、币种
- **用量统计** — 按模型显示 Token 消耗与调用次数
- **活动记录** — 按日查看 API 调用量和费用
- **多 Key 管理** — 存储多组 API Key，随时切换
- **系统托盘** — 关闭可最小化到托盘，后台刷新
- **单文件免安装** — 无需 .NET 运行时，双击即运行

## 快速开始

1. 从 [Releases](https://github.com/YBY-9900/ds-balance-viewer/releases) 下载最新的 `DSBalanceViewer.exe`
2. 双击运行
3. 首次启动会提示输入 DeepSeek API Key（格式：`sk-xxx...`）
4. 支持同时存储多组 Key，在「更换 Key」中管理

## 截图

| 余额仪表盘 | 用量统计 |
|-----------|---------|
| 显示账户总余额、充值余额、赠送余额 | 按模型和按天统计的 Token 与费用 |
| 支持多 Key 管理和快速切换 | 数据自动刷新，支持最小化到托盘 |

## 从源码构建

```bash
git clone https://github.com/YBY-9900/ds-balance-viewer.git
cd ds-balance-viewer/DSBalanceViewer

# 需要 .NET 8.0 SDK
dotnet publish -c Release -o publish
dotnet publish -c Release -o publish-self   # 自包含版本（免 .NET 运行时）
```

### 运行环境

- **免运行时版（推荐）** — 任何 Windows x64 系统均可直接运行
- **需运行时版** — 需要 .NET 8.0 运行时（[下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)）

## 技术栈

- **语言**: C# 12
- **框架**: .NET 8.0 Windows Forms
- **加密**: DPAPI（`System.Security.Cryptography.ProtectedData`）
- **API**: DeepSeek API（balance + billing/usage）
- **发布**: 单文件自包含（免运行时）
