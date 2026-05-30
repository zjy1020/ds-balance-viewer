using DSBalanceViewer.Models;
using DSBalanceViewer.Services;

namespace DSBalanceViewer;

public partial class MainForm : Form
{
    private readonly KeyVault _vault = new();
    private readonly PricingService _pricing = new();
    private DeepSeekApiService? _api;
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

            var key = _vault.GetActiveKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                key = ShowNewKeyDialog();
                if (key == null) { lblStatus.Text = "未配置 API Key"; return; }
            }

            _api = new DeepSeekApiService(key);
            var bt = _api.GetBalanceAsync();
            var ut = _api.GetUsageAsync();
            await Task.WhenAll(bt, ut);
            _balance = bt.Result;
            _usage = ut.Result;

            BuildUI();
            lblStatus.Text = $"最后更新: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                                              || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            lblStatus.Text = "Key 无效";
            MessageBox.Show("API Key 无效，请更换。", "认证失败");
            ShowKeyManager();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"错误: {ex.Message}";
            MessageBox.Show($"请求失败: {ex.Message}", "错误");
        }
        finally
        {
            btnRefresh.Enabled = true;
            _api?.Dispose();
        }
    }

    private string? ShowNewKeyDialog()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "请输入 DeepSeek API Key：\n\n格式: 名称=sk-xxxx\n不加名称则默认为「默认」",
            "添加 API Key", "", -1, -1);
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (input.Contains('='))
        {
            var p = input.Split('=', 2);
            _vault.SaveKey(p[0].Trim(), p[1].Trim());
        }
        else
        {
            _vault.SaveKey("默认", input.Trim());
        }
        return _vault.GetActiveKey();
    }

    private void ShowKeyManager()
    {
        var keys = _vault.ListKeys();
        var dlg = new Form
        {
            Text = "管理 API Keys",
            Size = new Size(400, 280),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font
        };

        var list = new ListBox
        {
            Location = new Point(12, 12),
            Size = new Size(360, 150),
            IntegralHeight = false
        };
        foreach (var k in keys)
            list.Items.Add(k.IsActive ? $"[当前] {k.Name}" : $"        {k.Name}");
        dlg.Controls.Add(list);

        Button btn(string t, int x) => new() { Text = t, Location = new Point(x, 172), Size = new Size(80, 28) };
        var bUse = btn("使用", 12);
        var bAdd = btn("新增", 100);
        var bDel = btn("删除", 188);
        var bClose = btn("关闭", 290);

        bUse.Click += (_, _) =>
        {
            if (list.SelectedIndex >= 0 && list.SelectedIndex < keys.Count)
            {
                _vault.SetActive(keys[list.SelectedIndex].Name);
                dlg.Close();
                lblStatus.Text = $"已切换到「{keys[list.SelectedIndex].Name}」";
            }
        };
        bAdd.Click += (_, _) => { var k = ShowNewKeyDialog(); if (k != null) dlg.Close(); };
        bDel.Click += (_, _) =>
        {
            if (list.SelectedIndex >= 0 && list.SelectedIndex < keys.Count
                && MessageBox.Show("确定删除？", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _vault.DeleteKey(keys[list.SelectedIndex].Name);
                dlg.Close();
            }
        };
        bClose.Click += (_, _) => dlg.Close();

        dlg.Controls.Add(bUse);
        dlg.Controls.Add(bAdd);
        dlg.Controls.Add(bDel);
        dlg.Controls.Add(bClose);
        dlg.ShowDialog(this);
    }

    // ========== UI Build ==========

    private void BuildUI()
    {
        mainPanel.Controls.Clear();
        var parent = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false
        };
        mainPanel.Controls.Add(parent);

        // ── Balance Section ──
        var bal = _balance?.BalanceInfos?.FirstOrDefault();
        if (bal != null)
        {
            parent.Controls.Add(SectionTitle("账户余额"));
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 16)
            };
            row.Controls.Add(MetricCard("总余额", $"¥{bal.TotalBalance}", Color.FromArgb(0, 120, 212)));
            row.Controls.Add(MetricCard("充值余额", $"¥{bal.ToppedUpBalance}", Color.FromArgb(0, 140, 90)));
            row.Controls.Add(MetricCard("赠送余额", $"¥{bal.GrantedBalance}", Color.FromArgb(200, 130, 0)));
            row.Controls.Add(MetricCard("币种", bal.Currency, Color.Gray));
            parent.Controls.Add(row);
        }
        else
        {
            parent.Controls.Add(SectionTitle("账户余额"));
            parent.Controls.Add(Placeholder("暂无余额数据"));
        }

        // ── Usage by Model Section ──
        parent.Controls.Add(SectionTitle("用量（按模型）"));
        var byModel = _usage?.ByModel;
        if (byModel != null && byModel.Count > 0)
        {
            parent.Controls.Add(BuildModelGrid(byModel));
        }
        else
        {
            parent.Controls.Add(Placeholder("暂无用量数据（/billing/usage 返回为空或 404）"));
        }

        // ── Recent Activity Section ──
        parent.Controls.Add(SectionTitle("近期活动"));
        var daily = _usage?.Daily;
        if (daily != null && daily.Count > 0)
        {
            parent.Controls.Add(BuildDailyGrid(daily));
        }
        else
        {
            parent.Controls.Add(Placeholder("暂无活动数据"));
        }
    }

    // ========== UI Components ==========

    static Label SectionTitle(string text) => new()
    {
        Text = text,
        Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
        ForeColor = Color.FromArgb(50, 50, 50),
        AutoSize = true,
        Margin = new Padding(0, 12, 0, 0)
    };

    static Label Placeholder(string text) => new()
    {
        Text = text,
        Font = new Font("Microsoft YaHei", 9),
        ForeColor = Color.Gray,
        AutoSize = true,
        Margin = new Padding(0, 4, 0, 8)
    };

    static Panel MetricCard(string label, string value, Color accent)
    {
        var card = new Panel
        {
            AutoSize = true,
            MinimumSize = new Size(140, 70),
            BackColor = Color.FromArgb(250, 250, 252),
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 10, 8)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(220, 220, 228));
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        var lb = new Label { Text = label, Font = new Font("Microsoft YaHei", 9), ForeColor = Color.Gray, AutoSize = true, Location = new Point(14, 10) };
        var vl = new Label { Text = value, Font = new Font("Microsoft YaHei", 18, FontStyle.Bold), ForeColor = accent, AutoSize = true, Location = new Point(14, 32) };
        card.Controls.Add(lb);
        card.Controls.Add(vl);
        return card;
    }

    DataGridView BuildModelGrid(List<ModelUsage> data)
    {
        var grid = new DataGridView
        {
            AutoSize = true,
            MinimumSize = new Size(720, 120),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Microsoft YaHei", 9),
            ColumnHeadersHeight = 32
        };
        grid.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

        grid.Columns.Add("Model", "模型");
        grid.Columns.Add("Calls", "调用次数");
        grid.Columns.Add("Tokens", "Token 消耗");
        grid.Columns.Add("Cost", "费用");

        foreach (var m in data)
            grid.Rows.Add(m.Model, $"{m.Calls:N0}", $"{m.Tokens:N0}", $"¥{m.Cost:N4}");

        grid.Columns["Calls"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Tokens"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Cost"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        if (data.Count > 0)
            grid.Height = grid.ColumnHeadersHeight + data.Count * grid.RowTemplate.Height + 4;

        return grid;
    }

    DataGridView BuildDailyGrid(List<UsagePoint> data)
    {
        var sorted = data.OrderByDescending(d => d.Date).Take(14).ToList();
        var grid = new DataGridView
        {
            AutoSize = true,
            MinimumSize = new Size(720, 120),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Microsoft YaHei", 9),
            ColumnHeadersHeight = 32
        };
        grid.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

        grid.Columns.Add("Date", "日期");
        grid.Columns.Add("Calls", "调用次数");
        grid.Columns.Add("Tokens", "Token 消耗");
        grid.Columns.Add("Cost", "费用");

        foreach (var d in sorted)
            grid.Rows.Add(d.Date, $"{d.Calls:N0}", $"{d.Tokens:N0}", $"¥{d.Cost:N4}");

        grid.Columns["Calls"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Tokens"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Cost"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        if (sorted.Count > 0)
            grid.Height = grid.ColumnHeadersHeight + sorted.Count * grid.RowTemplate.Height + 4;

        return grid;
    }
}
