# DeepSeek 用量仪表盘 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 C# WinForms 桌面应用，查看 DeepSeek API 余额、Token 用量和费用估算。

**Architecture:** 单窗口 WinForms，TabControl 承载 4 个 TabPage（仪表盘/余额/用量/费用）。Services 层负责 HTTP 调用、Key 加密存储、费用计算。Models 层为 JSON 反序列化的数据结构。

**Tech Stack:** .NET 8.0, WinForms, HttpClient, System.Text.Json, DPAPI

**项目路径:** `C:\Users\YBY\Desktop\ds-balance-viewer`

---

## 文件结构

```
ds-balance-viewer/
├── DSBalanceViewer.sln
├── DSBalanceViewer/
│   ├── DSBalanceViewer.csproj
│   ├── Program.cs
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   ├── MainForm.resx
│   ├── Models/
│   │   ├── BalanceInfo.cs
│   │   └── UsageRecord.cs
│   └── Services/
│       ├── DeepSeekApiService.cs
│       ├── KeyVault.cs
│       └── PricingService.cs
```

**职责划分：**
- `Program.cs` — 入口，启动 MainForm
- `MainForm` — 主窗口，TabControl + 四个 TabPage，刷新按钮，状态栏
- `BalanceInfo.cs` — 余额 API 响应的 JSON 反序列化模型
- `UsageRecord.cs` — 用量 API 响应的 JSON 反序列化模型
- `DeepSeekApiService.cs` — HttpClient 封装，调 `/user/balance` 和 `/user/usage`
- `KeyVault.cs` — DPAPI 加解密 Key，文件读写
- `PricingService.cs` — 内置各模型单价，Token 量 × 单价 = 费用

---

### Task 1: 创建项目结构与 Models

**Files:**
- Create: `DSBalanceViewer/DSBalanceViewer.csproj`
- Create: `DSBalanceViewer/Program.cs`
- Create: `DSBalanceViewer/Models/BalanceInfo.cs`
- Create: `DSBalanceViewer/Models/UsageRecord.cs`

- [ ] **Step 1: 用 dotnet CLI 创建项目**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
dotnet new winforms -n DSBalanceViewer -f net8.0 --no-restore
```

- [ ] **Step 2: 创建 Models 目录**

```bash
mkdir "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Models"
mkdir "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Services"
```

- [ ] **Step 3: 编写 BalanceInfo.cs**

创建 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Models\BalanceInfo.cs`：

```csharp
using System.Text.Json.Serialization;

namespace DSBalanceViewer.Models;

public class BalanceResponse
{
    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("balance_infos")]
    public List<BalanceInfo> BalanceInfos { get; set; } = new();
}

public class BalanceInfo
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "CNY";

    [JsonPropertyName("total_balance")]
    public string TotalBalance { get; set; } = "0.00";

    [JsonPropertyName("granted_balance")]
    public string GrantedBalance { get; set; } = "0.00";

    [JsonPropertyName("topped_up_balance")]
    public string ToppedUpBalance { get; set; } = "0.00";
}
```

- [ ] **Step 4: 编写 UsageRecord.cs**

创建 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Models\UsageRecord.cs`：

```csharp
using System.Text.Json.Serialization;

namespace DSBalanceViewer.Models;

public class UsageResponse
{
    [JsonPropertyName("data")]
    public List<UsageItem> Data { get; set; } = new();
}

public class UsageItem
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("prompt_tokens")]
    public long PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public long CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public long TotalTokens { get; set; }
}
```

- [ ] **Step 5: 还原依赖并编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet restore
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git init
git add DSBalanceViewer.sln DSBalanceViewer/ .gitignore
git commit -m "feat: create project structure with models"
```

---

### Task 2: KeyVault 加密存储服务

**Files:**
- Create: `DSBalanceViewer/Services/KeyVault.cs`

- [ ] **Step 1: 编写 KeyVault.cs**

创建 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Services\KeyVault.cs`：

```csharp
using System.Security.Cryptography;
using System.Text;

namespace DSBalanceViewer.Services;

public class KeyVault
{
    private readonly string _filePath;

    public KeyVault()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DSBalanceViewer");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "key.bin");
    }

    public bool KeyExists() => File.Exists(_filePath);

    public void SaveKey(string apiKey)
    {
        var plain = Encoding.UTF8.GetBytes(apiKey);
        var cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, cipher);
    }

    public string? LoadKey()
    {
        if (!File.Exists(_filePath)) return null;
        try
        {
            var cipher = File.ReadAllBytes(_filePath);
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void DeleteKey()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/Services/KeyVault.cs
git commit -m "feat: add KeyVault service with DPAPI encryption"
```

---

### Task 3: DeepSeekApiService HTTP 调用服务

**Files:**
- Create: `DSBalanceViewer/Services/DeepSeekApiService.cs`

- [ ] **Step 1: 编写 DeepSeekApiService.cs**

创建 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Services\DeepSeekApiService.cs`：

```csharp
using System.Net.Http.Headers;
using System.Text.Json;
using DSBalanceViewer.Models;

namespace DSBalanceViewer.Services;

public class DeepSeekApiService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.deepseek.com";

    public DeepSeekApiService(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<BalanceResponse> GetBalanceAsync()
    {
        var response = await _http.GetAsync($"{BaseUrl}/user/balance");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BalanceResponse>(json)
               ?? throw new InvalidOperationException("Failed to parse balance response");
    }

    public async Task<UsageResponse> GetUsageAsync()
    {
        var response = await _http.GetAsync($"{BaseUrl}/user/usage");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UsageResponse>(json)
               ?? throw new InvalidOperationException("Failed to parse usage response");
    }

    public void Dispose() => _http.Dispose();
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/Services/DeepSeekApiService.cs
git commit -m "feat: add DeepSeekApiService for balance and usage endpoints"
```

---

### Task 4: PricingService 费用估算服务

**Files:**
- Create: `DSBalanceViewer/Services/PricingService.cs`

- [ ] **Step 1: 编写 PricingService.cs**

创建 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Services\PricingService.cs`：

```csharp
using DSBalanceViewer.Models;

namespace DSBalanceViewer.Services;

public class PricingService
{
    // 价格单位: 元/百万 token（DeepSeek 官方定价，2025 年）
    // 输入价格 / 输出价格
    private readonly Dictionary<string, (decimal Input, decimal Output)> _pricing = new()
    {
        ["deepseek-chat"] = (1.00m, 2.00m),
        ["deepseek-reasoner"] = (4.00m, 16.00m),
    };

    public decimal EstimateCost(IEnumerable<UsageItem> items)
    {
        decimal total = 0;
        foreach (var item in items)
        {
            var (inputPrice, outputPrice) = GetPrice(item.Model);
            total += (item.PromptTokens / 1_000_000m) * inputPrice;
            total += (item.CompletionTokens / 1_000_000m) * outputPrice;
        }
        return Math.Round(total, 2);
    }

    public (decimal InputPrice, decimal OutputPrice) GetPrice(string model)
    {
        if (_pricing.TryGetValue(model, out var price))
            return price;
        // 未知模型默认按 deepseek-chat 价格
        return _pricing["deepseek-chat"];
    }

    public Dictionary<string, decimal> EstimateCostByDate(IEnumerable<UsageItem> items)
    {
        return items
            .GroupBy(i => i.Date)
            .ToDictionary(g => g.Key, g => EstimateCost(g));
    }

    public Dictionary<string, decimal> EstimateCostByModel(IEnumerable<UsageItem> items)
    {
        return items
            .GroupBy(i => i.Model)
            .ToDictionary(g => g.Key, g => EstimateCost(g));
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/Services/PricingService.cs
git commit -m "feat: add PricingService with model pricing and cost estimation"
```

---

### Task 5: MainForm — 窗口布局与 TabControl

**Files:**
- Delete: `DSBalanceViewer/Form1.cs` (template, 不需要)
- Delete: `DSBalanceViewer/Form1.Designer.cs` (template, 不需要)
- Create: `DSBalanceViewer/MainForm.cs`
- Create: `DSBalanceViewer/MainForm.Designer.cs`
- Modify: `DSBalanceViewer/Program.cs`

- [ ] **Step 1: 删除模板生成的 Form1 文件**

```bash
rm "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Form1.cs"
rm "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Form1.Designer.cs"
```

- [ ] **Step 2: 更新 Program.cs**

替换 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\Program.cs` 为：

```csharp
namespace DSBalanceViewer;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
```

- [ ] **Step 3: 创建 MainForm.Designer.cs**

创建 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\MainForm.Designer.cs`：

```csharp
namespace DSBalanceViewer;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TabControl tabControl;
    private TabPage tabDashboard;
    private TabPage tabBalance;
    private TabPage tabUsage;
    private TabPage tabCost;
    private Button btnRefresh;
    private Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.tabControl = new TabControl();
        this.tabDashboard = new TabPage();
        this.tabBalance = new TabPage();
        this.tabUsage = new TabPage();
        this.tabCost = new TabPage();
        this.btnRefresh = new Button();
        this.lblStatus = new Label();

        this.tabControl.SuspendLayout();
        this.SuspendLayout();

        // tabControl
        this.tabControl.Controls.Add(this.tabDashboard);
        this.tabControl.Controls.Add(this.tabBalance);
        this.tabControl.Controls.Add(this.tabUsage);
        this.tabControl.Controls.Add(this.tabCost);
        this.tabControl.Dock = DockStyle.Fill;
        this.tabControl.Name = "tabControl";
        this.tabControl.SelectedIndex = 0;

        // tabDashboard
        this.tabDashboard.Text = "仪表盘";
        this.tabDashboard.AutoScroll = true;

        // tabBalance
        this.tabBalance.Text = "余额";
        this.tabBalance.AutoScroll = true;

        // tabUsage
        this.tabUsage.Text = "用量";
        this.tabUsage.AutoScroll = true;

        // tabCost
        this.tabCost.Text = "费用";
        this.tabCost.AutoScroll = true;

        // btnRefresh
        this.btnRefresh.Text = "刷新";
        this.btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        // lblStatus
        this.lblStatus.Text = "就绪";
        this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        // status panel
        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            Padding = new Padding(10, 5, 10, 5)
        };
        statusPanel.Controls.Add(btnRefresh);
        statusPanel.Controls.Add(lblStatus);
        btnRefresh.Location = new Point(statusPanel.Width - 110, 7);
        lblStatus.Location = new Point(10, 10);
        lblStatus.AutoSize = true;

        // MainForm
        this.ClientSize = new Size(800, 600);
        this.Text = "DeepSeek 用量仪表盘";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Controls.Add(tabControl);
        this.Controls.Add(statusPanel);

        this.tabControl.ResumeLayout(false);
        this.ResumeLayout(false);
    }
}
```

- [ ] **Step 4: 创建 MainForm.cs（骨架）**

```csharp
using DSBalanceViewer.Models;
using DSBalanceViewer.Services;

namespace DSBalanceViewer;

public partial class MainForm : Form
{
    private readonly KeyVault _vault = new();
    private DeepSeekApiService? _api;
    private readonly PricingService _pricing = new();
    private BalanceResponse? _balance;
    private UsageResponse? _usage;

    public MainForm()
    {
        InitializeComponent();
        btnRefresh.Click += async (_, _) => await RefreshData();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RefreshData();
    }

    private async Task RefreshData()
    {
        // placeholder — will be implemented in Task 6
    }
}
```

- [ ] **Step 5: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/MainForm.cs DSBalanceViewer/MainForm.Designer.cs DSBalanceViewer/Program.cs
git commit -m "feat: add MainForm with TabControl layout and refresh button"
```

---

### Task 6: 实现 RefreshData 核心逻辑

**Files:**
- Modify: `DSBalanceViewer/MainForm.cs`

- [ ] **Step 1: 实现完整的 RefreshData 方法**

替换 `C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer\MainForm.cs` 为：

```csharp
using DSBalanceViewer.Models;
using DSBalanceViewer.Services;

namespace DSBalanceViewer;

public partial class MainForm : Form
{
    private readonly KeyVault _vault = new();
    private DeepSeekApiService? _api;
    private readonly PricingService _pricing = new();
    private BalanceResponse? _balance;
    private UsageResponse? _usage;

    public MainForm()
    {
        InitializeComponent();
        btnRefresh.Click += async (_, _) => await RefreshData();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RefreshData();
    }

    private async Task RefreshData()
    {
        try
        {
            btnRefresh.Enabled = false;
            lblStatus.Text = "加载中...";
            lblStatus.ForeColor = Color.Black;

            var key = await GetApiKey();
            if (key == null) return;

            _api = new DeepSeekApiService(key);

            var balanceTask = _api.GetBalanceAsync();
            var usageTask = _api.GetUsageAsync();

            await Task.WhenAll(balanceTask, usageTask);

            _balance = balanceTask.Result;
            _usage = usageTask.Result;

            BuildDashboard();
            BuildBalanceTab();
            BuildUsageTab();
            BuildCostTab();

            lblStatus.Text = $"最后更新: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _vault.DeleteKey();
            lblStatus.Text = "Key 无效，请重新输入";
            lblStatus.ForeColor = Color.Red;
            MessageBox.Show("API Key 无效，请重新输入。", "认证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"错误: {ex.Message}";
            lblStatus.ForeColor = Color.Red;
            MessageBox.Show($"请求失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRefresh.Enabled = true;
            _api?.Dispose();
        }
    }

    private async Task<string?> GetApiKey()
    {
        if (_vault.KeyExists())
        {
            var key = _vault.LoadKey();
            if (!string.IsNullOrWhiteSpace(key)) return key;
        }

        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "请输入 DeepSeek API Key：", "API Key", "", -1, -1);
        if (string.IsNullOrWhiteSpace(input))
        {
            lblStatus.Text = "未提供 API Key";
            return null;
        }

        _vault.SaveKey(input.Trim());
        return input.Trim();
    }

    // BuildDashboard / BuildBalanceTab / BuildUsageTab / BuildCostTab
    // will be implemented in subsequent tasks
    private void BuildDashboard() { }
    private void BuildBalanceTab() { }
    private void BuildUsageTab() { }
    private void BuildCostTab() { }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/MainForm.cs
git commit -m "feat: implement RefreshData with API key input and error handling"
```

---

### Task 7: 仪表盘 Tab

**Files:**
- Modify: `DSBalanceViewer/MainForm.cs`

- [ ] **Step 1: 实现 BuildDashboard 方法**

在 `MainForm.cs` 中替换 `private void BuildDashboard() { }` 为：

```csharp
private void BuildDashboard()
{
    tabDashboard.Controls.Clear();

    var panel = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        Padding = new Padding(20),
        AutoScroll = true
    };

    // ---- 余额卡片 ----
    var balance = _balance?.BalanceInfos?.FirstOrDefault();
    var cardPanel = new Panel
    {
        Width = 700,
        Height = 120,
        BackColor = Color.FromArgb(240, 248, 255),
        Padding = new Padding(15)
    };

    var titleBalance = new Label
    {
        Text = "💰 余额",
        Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
        Location = new Point(15, 10),
        AutoSize = true
    };
    cardPanel.Controls.Add(titleBalance);

    var totalLabel = new Label
    {
        Text = $"总余额: ¥{balance?.TotalBalance ?? "—"}",
        Font = new Font("Microsoft YaHei", 20, FontStyle.Bold),
        ForeColor = Color.FromArgb(0, 120, 212),
        Location = new Point(15, 40),
        AutoSize = true
    };
    cardPanel.Controls.Add(totalLabel);

    var grantedLabel = new Label
    {
        Text = $"赠送余额: ¥{balance?.GrantedBalance ?? "—"}   充值余额: ¥{balance?.ToppedUpBalance ?? "—"}",
        Font = new Font("Microsoft YaHei", 10),
        ForeColor = Color.Gray,
        Location = new Point(15, 80),
        AutoSize = true
    };
    cardPanel.Controls.Add(grantedLabel);

    panel.Controls.Add(cardPanel);

    // ---- Token 消耗 ----
    if (_usage?.Data != null && _usage.Data.Count > 0)
    {
        var totalPrompt = _usage.Data.Sum(i => i.PromptTokens);
        var totalCompletion = _usage.Data.Sum(i => i.CompletionTokens);

        var usagePanel = new Panel
        {
            Width = 700,
            Height = 100,
            BackColor = Color.FromArgb(245, 255, 245),
            Padding = new Padding(15),
            Margin = new Padding(0, 15, 0, 0)
        };

        var titleUsage = new Label
        {
            Text = "📊 本月 Token 消耗",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            Location = new Point(15, 10),
            AutoSize = true
        };
        usagePanel.Controls.Add(titleUsage);

        var tokenText = new Label
        {
            Text = $"输入: {totalPrompt:N0} tokens   输出: {totalCompletion:N0} tokens   合计: {totalPrompt + totalCompletion:N0} tokens",
            Font = new Font("Microsoft YaHei", 11),
            Location = new Point(15, 45),
            AutoSize = true
        };
        usagePanel.Controls.Add(tokenText);

        panel.Controls.Add(usagePanel);

        // ---- 费用概览 ----
        var costThisMonth = _pricing.EstimateCost(_usage.Data);
        var costPanel = new Panel
        {
            Width = 700,
            Height = 80,
            BackColor = Color.FromArgb(255, 250, 240),
            Padding = new Padding(15),
            Margin = new Padding(0, 15, 0, 0)
        };

        var titleCost = new Label
        {
            Text = "💵 本月费用估算",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            Location = new Point(15, 10),
            AutoSize = true
        };
        costPanel.Controls.Add(titleCost);

        var costValue = new Label
        {
            Text = $"¥{costThisMonth:N2}（根据官方定价估算）",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.DarkOrange,
            Location = new Point(15, 42),
            AutoSize = true
        };
        costPanel.Controls.Add(costValue);

        panel.Controls.Add(costPanel);
    }

    tabDashboard.Controls.Add(panel);
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/MainForm.cs
git commit -m "feat: implement dashboard tab with balance cards and cost overview"
```

---

### Task 8: 余额 Tab

**Files:**
- Modify: `DSBalanceViewer/MainForm.cs`

- [ ] **Step 1: 实现 BuildBalanceTab 方法**

在 `MainForm.cs` 中替换 `private void BuildBalanceTab() { }` 为：

```csharp
private void BuildBalanceTab()
{
    tabBalance.Controls.Clear();

    var panel = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        Padding = new Padding(40, 30, 40, 30),
        AutoScroll = true
    };

    var title = new Label
    {
        Text = "💰 账户余额",
        Font = new Font("Microsoft YaHei", 18, FontStyle.Bold),
        AutoSize = true
    };
    panel.Controls.Add(title);

    var balance = _balance?.BalanceInfos?.FirstOrDefault();
    if (balance == null)
    {
        panel.Controls.Add(new Label { Text = "暂无数据", AutoSize = true });
        tabBalance.Controls.Add(panel);
        return;
    }

    var items = new (string Label, string Value, Color Color)[]
    {
        ("总余额", $"¥{balance.TotalBalance}", Color.FromArgb(0, 120, 212)),
        ("充值余额", $"¥{balance.ToppedUpBalance}", Color.FromArgb(0, 150, 100)),
        ("赠送余额", $"¥{balance.GrantedBalance}", Color.FromArgb(200, 120, 0)),
    };

    foreach (var (label, value, color) in items)
    {
        var card = new Panel
        {
            Width = 500,
            Height = 90,
            BackColor = Color.White,
            Margin = new Padding(0, 15, 0, 0),
            Padding = new Padding(20)
        };

        var lbl = new Label
        {
            Text = label,
            Font = new Font("Microsoft YaHei", 12),
            ForeColor = Color.Gray,
            Location = new Point(20, 12),
            AutoSize = true
        };
        card.Controls.Add(lbl);

        var val = new Label
        {
            Text = value,
            Font = new Font("Microsoft YaHei", 26, FontStyle.Bold),
            ForeColor = color,
            Location = new Point(20, 38),
            AutoSize = true
        };
        card.Controls.Add(val);

        panel.Controls.Add(card);
    }

    panel.Controls.Add(new Label
    {
        Text = $"币种: {balance.Currency}",
        Font = new Font("Microsoft YaHei", 10),
        ForeColor = Color.Gray,
        Margin = new Padding(0, 15, 0, 0),
        AutoSize = true
    });

    tabBalance.Controls.Add(panel);
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/MainForm.cs
git commit -m "feat: implement balance tab with detailed balance cards"
```

---

### Task 9: 用量 Tab

**Files:**
- Modify: `DSBalanceViewer/MainForm.cs`

- [ ] **Step 1: 实现 BuildUsageTab 方法**

在 `MainForm.cs` 中替换 `private void BuildUsageTab() { }` 为：

```csharp
private void BuildUsageTab()
{
    tabUsage.Controls.Clear();

    if (_usage?.Data == null || _usage.Data.Count == 0)
    {
        tabUsage.Controls.Add(new Label
        {
            Text = "暂无用量数据",
            Location = new Point(20, 20),
            AutoSize = true
        });
        return;
    }

    // 按模型分组汇总
    var grouped = _usage.Data
        .GroupBy(i => i.Model)
        .Select(g => new
        {
            Model = g.Key,
            PromptTokens = g.Sum(i => i.PromptTokens),
            CompletionTokens = g.Sum(i => i.CompletionTokens),
            TotalTokens = g.Sum(i => i.TotalTokens)
        })
        .ToList();

    var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

    var title = new Label
    {
        Text = "📈 Token 用量（按模型）",
        Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
        Location = new Point(20, 15),
        AutoSize = true
    };
    panel.Controls.Add(title);

    var grid = new DataGridView
    {
        Location = new Point(20, 50),
        Width = 740,
        Height = 480,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false
    };

    grid.Columns.Add("Model", "模型");
    grid.Columns.Add("PromptTokens", "输入 Token");
    grid.Columns.Add("CompletionTokens", "输出 Token");
    grid.Columns.Add("TotalTokens", "总 Token");

    foreach (var item in grouped)
    {
        grid.Rows.Add(
            item.Model,
            $"{item.PromptTokens:N0}",
            $"{item.CompletionTokens:N0}",
            $"{item.TotalTokens:N0}");
    }

    // 默认列格式
    grid.Columns["PromptTokens"].DefaultCellStyle.Format = "N0";
    grid.Columns["CompletionTokens"].DefaultCellStyle.Format = "N0";
    grid.Columns["TotalTokens"].DefaultCellStyle.Format = "N0";

    panel.Controls.Add(grid);
    tabUsage.Controls.Add(panel);
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/MainForm.cs
git commit -m "feat: implement usage tab with DataGridView grouped by model"
```

---

### Task 10: 费用 Tab

**Files:**
- Modify: `DSBalanceViewer/MainForm.cs`

- [ ] **Step 1: 实现 BuildCostTab 方法**

在 `MainForm.cs` 中替换 `private void BuildCostTab() { }` 为：

```csharp
private void BuildCostTab()
{
    tabCost.Controls.Clear();

    if (_usage?.Data == null || _usage.Data.Count == 0)
    {
        tabCost.Controls.Add(new Label
        {
            Text = "暂无数据",
            Location = new Point(20, 20),
            AutoSize = true
        });
        return;
    }

    var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

    var title = new Label
    {
        Text = "💵 费用明细（按日期）",
        Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
        Location = new Point(20, 15),
        AutoSize = true
    };
    panel.Controls.Add(title);

    var disclaimer = new Label
    {
        Text = "⚠️ 费用根据官方定价估算，仅供参考，实际扣费以账单为准",
        Font = new Font("Microsoft YaHei", 9),
        ForeColor = Color.DarkOrange,
        Location = new Point(20, 42),
        AutoSize = true
    };
    panel.Controls.Add(disclaimer);

    // 按日期汇总
    var byDate = _usage.Data
        .GroupBy(i => i.Date)
        .Select(g => new
        {
            Date = g.Key,
            TotalTokens = g.Sum(i => i.TotalTokens),
            EstimatedCost = _pricing.EstimateCost(g)
        })
        .OrderByDescending(x => x.Date)
        .ToList();

    var grid = new DataGridView
    {
        Location = new Point(20, 75),
        Width = 740,
        Height = 450,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false
    };

    grid.Columns.Add("Date", "日期");
    grid.Columns.Add("Tokens", "Token 消耗");
    grid.Columns.Add("Cost", "估算费用");

    foreach (var item in byDate)
    {
        grid.Rows.Add(item.Date, $"{item.TotalTokens:N0}", $"¥{item.EstimatedCost:N4}");
    }

    grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
    grid.Columns["Cost"].DefaultCellStyle.Format = "C";

    panel.Controls.Add(grid);
    tabCost.Controls.Add(panel);
}
```

- [ ] **Step 2: 编译验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add DSBalanceViewer/MainForm.cs
git commit -m "feat: implement cost tab with date-grouped estimated costs"
```

---

### Task 11: 最终集成验证

**Files:** (none, verification only)

- [ ] **Step 1: 完整编译**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet build --configuration Release
```

Expected: Build succeeded with 0 errors and 0 warnings.

- [ ] **Step 2: 运行应用验证**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer\DSBalanceViewer"
dotnet run
```

Expected: 窗口打开，显示 4 个 Tab。首次提示输入 API Key。输入后仪表盘显示数据。

- [ ] **Step 3: 验证清单**

手动验证：
- [ ] 启动弹出 Key 输入框
- [ ] 输入正确 Key 后仪表盘显示余额/Token/费用
- [ ] 切换到余额 Tab 显示三个余额数字
- [ ] 切换到用量 Tab 显示 DataGridView 表格
- [ ] 切换到费用 Tab 显示按日期费用
- [ ] 点击刷新按钮重新加载
- [ ] 输入无效 Key 提示错误
- [ ] 关闭后重开，不再要求输入 Key

- [ ] **Step 4: 最终 Commit**

```bash
cd "C:\Users\YBY\Desktop\ds-balance-viewer"
git add .
git commit -m "chore: final integration verification complete"
```
