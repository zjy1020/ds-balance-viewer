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
        btnChangeKey.Click += (_, _) => ShowKeyManager();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RefreshData();
    }

    // ========== Data Loading ==========

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
            lblStatus.Text = "Key 无效";
            lblStatus.ForeColor = Color.Red;
            MessageBox.Show("API Key 无效，请更换 Key。", "认证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ShowKeyManager();
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
        var key = _vault.GetActiveKey();
        if (!string.IsNullOrWhiteSpace(key)) return key;

        return ShowNewKeyDialog();
    }

    private string? ShowNewKeyDialog()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "请输入 DeepSeek API Key：\n\n（可选：输入 '名称=Key' 格式命名，如：\n主账号=sk-xxxx）",
            "添加 API Key", "", -1, -1);
        if (string.IsNullOrWhiteSpace(input)) return null;

        string name, key;
        if (input.Contains('='))
        {
            var parts = input.Split('=', 2);
            name = parts[0].Trim();
            key = parts[1].Trim();
        }
        else
        {
            name = "默认";
            key = input.Trim();
        }

        _vault.SaveKey(name, key);
        lblStatus.Text = $"Key「{name}」已保存";
        return key;
    }

    // ========== Key Manager ==========

    private void ShowKeyManager()
    {
        var keys = _vault.ListKeys();
        var form = new Form
        {
            Text = "管理 API Keys",
            Size = new Size(450, 320),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = this.Font
        };

        var listBox = new ListBox
        {
            Location = new Point(12, 12),
            Size = new Size(410, 160),
            IntegralHeight = false
        };
        foreach (var k in keys)
            listBox.Items.Add(k.IsActive ? $"★ {k.Name}" : $"   {k.Name}");
        form.Controls.Add(listBox);

        var btnUse = new Button { Text = "使用选中", Location = new Point(12, 182), Size = new Size(90, 28) };
        var btnAdd = new Button { Text = "新增...", Location = new Point(108, 182), Size = new Size(90, 28) };
        var btnDel = new Button { Text = "删除选中", Location = new Point(204, 182), Size = new Size(90, 28) };
        var btnClose = new Button { Text = "关闭", Location = new Point(340, 182), Size = new Size(80, 28) };

        form.Controls.Add(btnUse);
        form.Controls.Add(btnAdd);
        form.Controls.Add(btnDel);
        form.Controls.Add(btnClose);

        btnUse.Click += (_, _) =>
        {
            if (listBox.SelectedIndex >= 0 && listBox.SelectedIndex < keys.Count)
            {
                _vault.SetActive(keys[listBox.SelectedIndex].Name);
                form.Close();
                lblStatus.Text = $"已切换到「{keys[listBox.SelectedIndex].Name}」，点击刷新";
            }
        };

        btnAdd.Click += (_, _) =>
        {
            var newKey = ShowNewKeyDialog();
            if (newKey != null) form.Close();
        };

        btnDel.Click += (_, _) =>
        {
            if (listBox.SelectedIndex >= 0 && listBox.SelectedIndex < keys.Count)
            {
                var name = keys[listBox.SelectedIndex].Name;
                var result = MessageBox.Show($"确定删除 Key「{name}」吗？", "确认删除",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    _vault.DeleteKey(name);
                    form.Close();
                    lblStatus.Text = $"已删除「{name}」";
                }
            }
        };

        btnClose.Click += (_, _) => form.Close();

        form.ShowDialog(this);
    }

    // ========== Tab Builders ==========

    private void BuildDashboard()
    {
        tabDashboard.Controls.Clear();
        var panel = NewScrollPanel();

        // 余额卡片
        var balance = _balance?.BalanceInfos?.FirstOrDefault();
        if (balance != null)
        {
            var card = NewCard(Color.FromArgb(240, 248, 255));
            card.Controls.Add(NewBoldLabel("💰 余额", 13, 16, 10, Color.Black));
            card.Controls.Add(NewBoldLabel($"¥{balance.TotalBalance}", 24, 16, 36, Color.FromArgb(0, 120, 212)));
            card.Controls.Add(NewLabel(
                $"充值余额: ¥{balance.ToppedUpBalance}    赠送余额: ¥{balance.GrantedBalance}    币种: {balance.Currency}",
                9, 16, 68, Color.Gray));
            card.Height = 95;
            panel.Controls.Add(card);
        }

        // 用量概览
        if (_usage?.Daily != null && _usage.Daily.Count > 0)
        {
            var totalTokens = _pricing.TotalTokens(_usage.Daily);
            var totalCalls = _pricing.TotalCalls(_usage.Daily);
            var totalCost = _pricing.TotalCost(_usage.Daily);

            var card2 = NewCard(Color.FromArgb(245, 255, 245));
            card2.Controls.Add(NewBoldLabel("📊 用量概览", 13, 16, 10, Color.Black));
            card2.Controls.Add(NewLabel($"Token 消耗: {totalTokens:N0}    调用次数: {totalCalls:N0}", 10, 16, 36, Color.Black));
            card2.Controls.Add(NewBoldLabel($"本月费用: ¥{totalCost:N4}", 14, 16, 58, Color.DarkOrange));
            card2.Height = 90;
            panel.Controls.Add(card2);
        }

        tabDashboard.Controls.Add(panel);
    }

    private void BuildBalanceTab()
    {
        tabBalance.Controls.Clear();
        var panel = NewScrollPanel();
        panel.Padding = new Padding(40, 24, 40, 24);

        var balTitle = NewBoldLabel("💰 账户余额", 18, 0, 0, Color.Black);
        balTitle.Dock = DockStyle.Top;
        balTitle.Height = 40;
        panel.Controls.Add(balTitle);

        var balance = _balance?.BalanceInfos?.FirstOrDefault();
        if (balance == null)
        {
            panel.Controls.Add(NewLabel("暂无数据", 10, 0, 50, Color.Gray));
            tabBalance.Controls.Add(panel);
            return;
        }

        var items = new[] {
            ("总余额", $"¥{balance.TotalBalance}", Color.FromArgb(0, 120, 212)),
            ("充值余额", $"¥{balance.ToppedUpBalance}", Color.FromArgb(0, 150, 100)),
            ("赠送余额", $"¥{balance.GrantedBalance}", Color.FromArgb(200, 120, 0)),
        };

        int y = 50;
        foreach (var (label, value, color) in items)
        {
            var card = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(panel.ClientSize.Width - 80, 90),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            card.Controls.Add(NewLabel(label, 12, 20, 12, Color.Gray));
            card.Controls.Add(NewBoldLabel(value, 26, 20, 38, color));
            panel.Controls.Add(card);
            y += 102;
        }

        tabBalance.Controls.Add(panel);
    }

    private void BuildUsageTab()
    {
        tabUsage.Controls.Clear();

        var byModel = _usage?.ByModel;
        if (byModel == null || byModel.Count == 0)
        {
            var empty = new Label { Text = "暂无用量数据", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray };
            tabUsage.Controls.Add(empty);
            return;
        }

        var usageTitle = NewBoldLabel("📈 用量（按模型）", 13, 12, 8, Color.Black);
        usageTitle.Dock = DockStyle.Top;
        usageTitle.Height = 30;
        usageTitle.Padding = new Padding(12, 8, 0, 0);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        grid.Columns.Add("Model", "模型");
        grid.Columns.Add("Calls", "调用次数");
        grid.Columns.Add("Tokens", "Token");
        grid.Columns.Add("Cost", "费用");

        foreach (var item in byModel)
            grid.Rows.Add(item.Model, $"{item.Calls:N0}", $"{item.Tokens:N0}", $"¥{item.Cost:N4}");

        grid.Columns["Calls"].DefaultCellStyle.Format = "N0";
        grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
        grid.Columns["Cost"].DefaultCellStyle.Format = "C";

        tabUsage.Controls.Add(grid);
        tabUsage.Controls.Add(usageTitle);
    }

    private void BuildCostTab()
    {
        tabCost.Controls.Clear();

        var daily = _usage?.Daily;
        if (daily == null || daily.Count == 0)
        {
            var empty = new Label { Text = "暂无数据", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray };
            tabCost.Controls.Add(empty);
            return;
        }

        var costTitle = NewBoldLabel("💵 费用明细（按日期）", 13, 12, 8, Color.Black);
        costTitle.Dock = DockStyle.Top;
        costTitle.Height = 30;
        costTitle.Padding = new Padding(12, 8, 0, 0);

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
            BorderStyle = BorderStyle.None
        };
        grid.Columns.Add("Date", "日期");
        grid.Columns.Add("Calls", "调用次数");
        grid.Columns.Add("Tokens", "Token");
        grid.Columns.Add("Cost", "费用");

        foreach (var item in sorted)
            grid.Rows.Add(item.Date, $"{item.Calls:N0}", $"{item.Tokens:N0}", $"¥{item.Cost:N4}");

        grid.Columns["Calls"].DefaultCellStyle.Format = "N0";
        grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
        grid.Columns["Cost"].DefaultCellStyle.Format = "C";

        tabCost.Controls.Add(grid);
        tabCost.Controls.Add(costTitle);
    }

    // ========== UI Helpers ==========

    private static Panel NewScrollPanel() => new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding(16)
    };

    private static Panel NewCard(Color bg) => new()
    {
        Dock = DockStyle.Top,
        BackColor = bg,
        Padding = new Padding(16),
        Height = 80,
        Margin = new Padding(0, 0, 0, 10)
    };

    private static Label NewBoldLabel(string text, float size, int x, int y, Color color) => new()
    {
        Text = text,
        Font = new Font("Microsoft YaHei", size, FontStyle.Bold),
        ForeColor = color,
        Location = new Point(x, y),
        AutoSize = true
    };

    private static Label NewLabel(string text, float size, int x, int y, Color color) => new()
    {
        Text = text,
        Font = new Font("Microsoft YaHei", size),
        ForeColor = color,
        Location = new Point(x, y),
        AutoSize = true
    };
}
