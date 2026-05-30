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

            var key = GetApiKey();
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

    private string? GetApiKey()
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

    // ---- Build methods for each tab ----

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

        grid.Columns["PromptTokens"].DefaultCellStyle.Format = "N0";
        grid.Columns["CompletionTokens"].DefaultCellStyle.Format = "N0";
        grid.Columns["TotalTokens"].DefaultCellStyle.Format = "N0";

        panel.Controls.Add(grid);
        tabUsage.Controls.Add(panel);
    }

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
}
