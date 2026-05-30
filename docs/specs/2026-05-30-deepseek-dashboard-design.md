# DeepSeek 用量仪表盘 — 设计规格

## 概述

一个 C# WinForms 桌面应用，查看 DeepSeek API 用量详情（余额、Token 消耗、费用估算），替代网页查看。

---

## 技术栈

| 项 | 选择 |
|---|---|
| 框架 | .NET 8.0 WinForms |
| UI 库 | 系统自带控件（简洁实用，不引入第三方 UI 库） |
| HTTP | `HttpClient` + `System.Text.Json` |
| Key 加密 | DPAPI (`System.Security.Cryptography.ProtectedData`) |
| 图表 | 无第三方图表库，用进度条/简单绘制替代 |

---

## 架构

```
┌──────────────────────────────────────────────────┐
│                   WinForms UI                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────┐ │
│  │  仪表盘   │ │   余额   │ │   用量   │ │ 费用 │ │
│  │ TabPage1 │ │ TabPage2 │ │ TabPage3 │ │TabPg4│ │
│  └──────────┘ └──────────┘ └──────────┘ └──────┘ │
├──────────────────────────────────────────────────┤
│                    Services                       │
│  DeepSeekApiService — HTTP 调用 API               │
│  KeyVault          — DPAPI 加解密 Key             │
│  PricingService    — 费用估算                      │
├──────────────────────────────────────────────────┤
│                    Models                         │
│  BalanceInfo / UsageRecord / CostSummary           │
└──────────────────────────────────────────────────┘
```

---

## Tab 页设计

### Tab 1 — 仪表盘（首页）

打开即见，汇总关键信息：
- **余额卡片**：总余额（大字）、赠送余额、充值余额
- **Token 总消耗**：本月输入/输出 token 合计
- **本月费用概览**：一个估算数字
- 最后刷新时间戳

### Tab 2 — 余额

- 总余额、赠送余额、充值余额，清晰展示
- 余额数值大字体显示

### Tab 3 — 用量

- `DataGridView` 表格，按模型分组
- 列：模型名称、输入 Token、输出 Token、总 Token
- 显示日期范围（默认本月，可切换）

### Tab 4 — 费用

- 按日期汇总表格
- 列：日期、模型、Token 量、估算费用
- 顶部标注"费用根据官方定价估算，仅供参考"
- 各模型单价硬编码（查自 DeepSeek 官方定价页）

---

## 数据流

```
启动 → KeyVault 读取加密 Key → DeepSeekApiService 调用 API
  → 解析 JSON → Model → 绑定 UI
  → 失败则显示错误提示，不会崩溃
```

- 启动时自动刷新一次
- 点击"刷新"按钮手动刷新
- API 调用失败显示 MessageBox 错误提示

---

## DeepSeek API

### 接口

| 端点 | 用途 |
|---|---|
| `GET https://api.deepseek.com/user/balance` | 余额 |
| `GET https://api.deepseek.com/user/usage` | Token 用量 |

### 请求头

```
Authorization: Bearer <api_key>
Content-Type: application/json
```

---

## Key 存储

- 第一次启动弹出输入框让用户输入 API Key
- Key 用 DPAPI 加密后存到 `%LOCALAPPDATA%\DSBalanceViewer\key.bin`
- 之后启动自动读取解密

---

## 错误处理

| 场景 | 处理 |
|---|---|
| 无 Key | 弹出输入框 |
| Key 无效 (401) | 提示 Key 无效，重新输入 |
| 网络错误 | 提示网络错误，可重试 |
| API 返回异常 | 显示错误信息，不崩溃 |

---

## 项目结构

```
ds-balance-viewer/
├── DSBalanceViewer.sln
├── DSBalanceViewer/
│   ├── DSBalanceViewer.csproj
│   ├── Program.cs
│   ├── MainForm.cs              # 主窗体，包含 TabControl + 四个 Tab
│   ├── MainForm.Designer.cs
│   ├── Models/
│   │   ├── BalanceInfo.cs
│   │   └── UsageRecord.cs
│   ├── Services/
│   │   ├── DeepSeekApiService.cs
│   │   ├── KeyVault.cs
│   │   └── PricingService.cs
│   └── appsettings.json         # 可选：模型定价配置
└── docs/
    └── specs/
        └── 2026-05-30-deepseek-dashboard-design.md
```

---

## 不做

- 不做数据持久化（不存历史记录，每次刷新拿最新）
- 不做通知/提醒
- 不做多 Key 管理
- 不做导出功能
- 不引入第三方 UI/图表库
