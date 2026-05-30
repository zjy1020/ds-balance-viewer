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
        btnChangeKey.Click += (_, _) => ChangeKey();
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
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                                              || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _vault.DeleteKey();
            lblStatus.Text = "Key 无效，请重新输入";
            lblStatus.ForeColor = Color.Red;
            MessageBox.Show("API Key 无效，请重新输入。", "认证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ChangeKey();
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

    private void ChangeKey()
    {
        _vault.DeleteKey();
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "请输入新的 DeepSeek API Key：", "更换 API Key", "", -1, -1);
        if (!string.IsNullOrWhiteSpace(input))
        {
            _vault.SaveKey(input.Trim());
            lblStatus.Text = "Key 已更新，点击刷新";
        }
    }

    // ---- Build methods for each tab ----

    private void BuildDashboard()
    {
        tabDashboard.Controls.Clear();

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(16),
            AutoScroll = true
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // ---- 余额卡片 ----
        var balance = _balance?.BalanceInfos?.FirstOrDefault();
        var card = MakeCard("💰 余额", Color.FromArgb(240, 248, 255), new (string, FontStyle, Color)[] {
            ($"总余额: ¥{balance?.TotalBalance ?? "—"}", FontStyle.Bold, Color.FromArgb(0, 120, 212)),
            ($"赠送余额: ¥{balance?.GrantedBalance ?? "—"}   充值余额: ¥{balance?.ToppedUpBalance ?? "—"}", FontStyle.Regular, Color.Gray),
        });
        main.Controls.Add(card);
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ---- 用量与费用 ----
        if (_usage?.Daily != null && _usage.Daily.Count > 0)
        {
            var totalTokens = _pricing.TotalTokens(_usage.Daily);
            var totalCalls = _pricing.TotalCalls(_usage.Daily);

            var usageCard = MakeCard("📊 用量概览", Color.FromArgb(245, 255, 245), new (string, FontStyle, Color)[] {
                ($"Token: {totalTokens:N0}   调用次数: {totalCalls:N0}", FontStyle.Regular, Color.Black),
            });
            main.Controls.Add(usageCard);
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var totalCost = _pricing.TotalCost(_usage.Daily);
            var costCard = MakeCard("💵 本月费用", Color.FromArgb(255, 250, 240), new (string, FontStyle, Color)[] {
                ($"¥{totalCost:N4}", FontStyle.Bold, Color.DarkOrange),
            });
            main.Controls.Add(costCard);
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        // 填充剩余空间
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(new Panel());

        tabDashboard.Controls.Add(main);
    }

    // Helper: create a styled card panel
    private Panel MakeCard(string title, Color backColor, (string Text, FontStyle Style, Color Color)[] lines)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = backColor,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12)
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Microsoft YaHei", 13, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 28
        };
        card.Controls.Add(titleLabel);

        var y = 34;
        foreach (var (text, style, color) in lines)
        {
            var fontStyle = style == FontStyle.Bold
                ? new Font("Microsoft YaHei", style == FontStyle.Bold ? 18 : 10, style)
                : new Font("Microsoft YaHei", 10);
            var lbl = new Label
            {
                Text = text,
                Font = fontStyle,
                ForeColor = color,
                Location = new Point(16, y),
                AutoSize = true
            };
            card.Controls.Add(lbl);
            y += style == FontStyle.Bold ? 34 : 22;
        }

        return card;
    }

    private void BuildBalanceTab()
    {
        tabBalance.Controls.Clear();

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(40, 24, 40, 24),
            AutoScroll = true
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "💰 账户余额",
            Font = new Font("Microsoft YaHei", 18, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 36,
            Margin = new Padding(0, 0, 0, 16)
        };
        main.Controls.Add(title);
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var balance = _balance?.BalanceInfos?.FirstOrDefault();
        if (balance == null)
        {
            main.Controls.Add(new Label { Text = "暂无数据", AutoSize = true });
            tabBalance.Controls.Add(main);
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
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 12),
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

            main.Controls.Add(card);
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        main.Controls.Add(new Label
        {
            Text = $"币种: {balance.Currency}",
            Font = new Font("Microsoft YaHei", 10),
            ForeColor = Color.Gray,
            Margin = new Padding(0, 4, 0, 0),
            AutoSize = true
        });
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // fill rest
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(new Panel());

        tabBalance.Controls.Add(main);
    }

    private void BuildUsageTab()
    {
        tabUsage.Controls.Clear();

        var byModel = _usage?.ByModel;
        if (byModel == null || byModel.Count == 0)
        {
            var empty = new Label
            {
                Text = "暂无用量数据",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            tabUsage.Controls.Add(empty);
            return;
        }

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

        var title = new Label
        {
            Text = "📈 Token 用量（按模型）",
            Font = new Font("Microsoft YaHei", 13, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30
        };
        panel.Controls.Add(title);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };

        grid.Columns.Add("Model", "模型");
        grid.Columns.Add("Calls", "调用次数");
        grid.Columns.Add("Tokens", "Token 消耗");
        grid.Columns.Add("Cost", "费用");

        foreach (var item in byModel)
            grid.Rows.Add(item.Model, $"{item.Calls:N0}", $"{item.Tokens:N0}", $"¥{item.Cost:N4}");

        grid.Columns["Calls"].DefaultCellStyle.Format = "N0";
        grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
        grid.Columns["Cost"].DefaultCellStyle.Format = "C";

        panel.Controls.Add(grid);
        tabUsage.Controls.Add(panel);
    }

    private void BuildCostTab()
    {
        tabCost.Controls.Clear();

        var daily = _usage?.Daily;
        if (daily == null || daily.Count == 0)
        {
            var empty = new Label
            {
                Text = "暂无数据",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            tabCost.Controls.Add(empty);
            return;
        }

        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

        var title = new Label
        {
            Text = "💵 费用明细（按日期）",
            Font = new Font("Microsoft YaHei", 13, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30
        };
        panel.Controls.Add(title);

        var sorted = daily.OrderByDescending(d => d.Date).ToList();

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };

        grid.Columns.Add("Date", "日期");
        grid.Columns.Add("Calls", "调用次数");
        grid.Columns.Add("Tokens", "Token 消耗");
        grid.Columns.Add("Cost", "费用");

        foreach (var item in sorted)
            grid.Rows.Add(item.Date, $"{item.Calls:N0}", $"{item.Tokens:N0}", $"¥{item.Cost:N4}");

        grid.Columns["Calls"].DefaultCellStyle.Format = "N0";
        grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
        grid.Columns["Cost"].DefaultCellStyle.Format = "C";

        panel.Controls.Add(grid);
        tabCost.Controls.Add(panel);
    }
}
