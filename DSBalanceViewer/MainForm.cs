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

    // Dark theme colors
    static readonly Color Bg = Color.FromArgb(13, 17, 23);
    static readonly Color Surface = Color.FromArgb(22, 27, 34);
    static readonly Color Border = Color.FromArgb(48, 54, 61);
    static readonly Color TextPrimary = Color.FromArgb(201, 209, 217);
    static readonly Color TextSecondary = Color.FromArgb(139, 148, 158);
    static readonly Color TextMuted = Color.FromArgb(72, 79, 88);
    static readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
    static readonly Color AccentGreen = Color.FromArgb(63, 185, 80);
    static readonly Color AccentAmber = Color.FromArgb(210, 153, 29);

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

    // ========== Data ==========

    private async Task RefreshData()
    {
        try
        {
            btnRefresh.Enabled = false;
            lblStatus.Text = "● 加载中...";
            lblStatus.ForeColor = AccentAmber;

            var key = _vault.GetActiveKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                var input = ShowNewKeyDialog();
                if (input == null) { lblStatus.Text = "● 未配置 Key"; lblStatus.ForeColor = TextMuted; return; }
                key = input;
            }

            _api = new DeepSeekApiService(key);
            var bt = _api.GetBalanceAsync();
            var ut = _api.GetUsageAsync();
            await Task.WhenAll(bt, ut);
            _balance = bt.Result;
            _usage = ut.Result;

            BuildUI();
            lblStatus.Text = "● 在线";
            lblStatus.ForeColor = AccentGreen;
            lblLastUpdate.Text = $"最后更新: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized
                                              || ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            lblStatus.Text = "● Key 无效";
            lblStatus.ForeColor = Color.FromArgb(248, 81, 73);
            MessageBox.Show("API Key 无效，请更换。", "认证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ShowKeyManager();
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"● {ex.Message}";
            lblStatus.ForeColor = Color.FromArgb(248, 81, 73);
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
            "请输入 DeepSeek API Key：\n\n格式: 名称=sk-xxxx  (不加名称则默认为「默认」)",
            "添加 API Key", "", -1, -1);
        if (string.IsNullOrWhiteSpace(input)) return null;
        string name, key;
        if (input.Contains('=')) { var p = input.Split('=', 2); name = p[0].Trim(); key = p[1].Trim(); }
        else { name = "默认"; key = input.Trim(); }
        _vault.SaveKey(name, key);
        return key;
    }

    private void ShowKeyManager()
    {
        var keys = _vault.ListKeys();
        var dlg = new Form
        {
            Text = "管理 API Keys",
            Size = new Size(420, 300),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = Surface, ForeColor = TextPrimary, Font = Font
        };
        var list = new ListBox { Location = new Point(12, 12), Size = new Size(380, 160), IntegralHeight = false, BackColor = Bg, ForeColor = TextPrimary, BorderStyle = BorderStyle.None };
        foreach (var k in keys) list.Items.Add(k.IsActive ? $"★ {k.Name}" : $"   {k.Name}");
        dlg.Controls.Add(list);
        var bUse = NewDlgBtn("使用", 12, 180); var bAdd = NewDlgBtn("新增", 100, 180); var bDel = NewDlgBtn("删除", 188, 180); var bCl = NewDlgBtn("关闭", 320, 180);
        dlg.Controls.Add(bUse); dlg.Controls.Add(bAdd); dlg.Controls.Add(bDel); dlg.Controls.Add(bCl);
        bUse.Click += (_, _) => { if (list.SelectedIndex >= 0) { _vault.SetActive(keys[list.SelectedIndex].Name); dlg.Close(); } };
        bAdd.Click += (_, _) => { var k = ShowNewKeyDialog(); if (k != null) dlg.Close(); };
        bDel.Click += (_, _) => { if (list.SelectedIndex >= 0 && MessageBox.Show("删除？", "", MessageBoxButtons.YesNo) == DialogResult.Yes) { _vault.DeleteKey(keys[list.SelectedIndex].Name); dlg.Close(); } };
        bCl.Click += (_, _) => dlg.Close();
        dlg.ShowDialog(this);
    }

    static Button NewDlgBtn(string text, int x, int y) => new() { Text = text, Location = new Point(x, y), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(33, 38, 45), ForeColor = TextPrimary, Font = new Font("Microsoft YaHei", 9) };

    // ========== Build ==========

    private void BuildUI()
    {
        mainPanel.Controls.Clear();
        int y = 0;

        // ── Section: Balance ──
        mainPanel.Controls.Add(SectionLabel("BALANCE", ref y));
        var balances = _balance?.BalanceInfos?.FirstOrDefault();
        if (balances != null)
        {
            var row = new FlowLayoutPanel { Location = new Point(0, y), Width = mainPanel.ClientSize.Width - 32, Height = 90, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            row.Controls.Add(BalanceCard("总余额", $"¥{balances.TotalBalance}", AccentBlue));
            row.Controls.Add(BalanceCard("充值余额", $"¥{balances.ToppedUpBalance}", AccentGreen));
            row.Controls.Add(BalanceCard("赠送余额", $"¥{balances.GrantedBalance}", AccentAmber));
            mainPanel.Controls.Add(row);
            y += 100;
        }

        // ── Section: Usage by Model ──
        var byModel = _usage?.ByModel;
        if (byModel != null && byModel.Count > 0)
        {
            mainPanel.Controls.Add(SectionLabel("USAGE BY MODEL", ref y));
            var grid = NewGrid(ref y);
            grid.Columns.Add("Model", "MODEL");
            grid.Columns.Add("Calls", "CALLS");
            grid.Columns.Add("Tokens", "TOKENS");
            grid.Columns.Add("Cost", "COST");
            foreach (var m in byModel)
                grid.Rows.Add(m.Model, $"{m.Calls:N0}", $"{m.Tokens:N0}", $"¥{m.Cost:N4}");
            grid.Columns["Calls"].DefaultCellStyle.Format = "N0";
            grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
            grid.Columns["Cost"].DefaultCellStyle.Format = "C";
            mainPanel.Controls.Add(grid);
            y += 220;
        }

        // ── Section: Recent Usage ──
        var daily = _usage?.Daily;
        if (daily != null && daily.Count > 0)
        {
            mainPanel.Controls.Add(SectionLabel("RECENT ACTIVITY", ref y));
            var grid = NewGrid(ref y);
            grid.Columns.Add("Date", "DATE");
            grid.Columns.Add("Calls", "CALLS");
            grid.Columns.Add("Tokens", "TOKENS");
            grid.Columns.Add("Cost", "COST");
            foreach (var d in daily.OrderByDescending(x => x.Date).Take(14))
                grid.Rows.Add(d.Date, $"{d.Calls:N0}", $"{d.Tokens:N0}", $"¥{d.Cost:N4}");
            grid.Columns["Calls"].DefaultCellStyle.Format = "N0";
            grid.Columns["Tokens"].DefaultCellStyle.Format = "N0";
            grid.Columns["Cost"].DefaultCellStyle.Format = "C";
            mainPanel.Controls.Add(grid);
            y += 220;
        }

        if (y == 0)
        {
            var empty = new Label { Text = "暂无数据\n\n点击「刷新」加载", Font = new Font("Microsoft YaHei", 14), ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            mainPanel.Controls.Add(empty);
        }
    }

    // ========== UI Components ==========

    static Label SectionLabel(string text, ref int y) => new()
    {
        Text = text,
        Font = new Font("Consolas", 10, FontStyle.Bold),
        ForeColor = TextSecondary,
        Location = new Point(0, y + 8),
        Size = new Size(700, 20),
        AutoSize = false
    };

    static Panel BalanceCard(string label, string value, Color accent)
    {
        var card = new Panel
        {
            Width = 220,
            Height = 78,
            BackColor = Surface,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(14)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        var lb = new Label { Text = label, Font = new Font("Consolas", 9), ForeColor = TextSecondary, Location = new Point(14, 12), AutoSize = true };
        var vl = new Label { Text = value, Font = new Font("Consolas", 18, FontStyle.Bold), ForeColor = accent, Location = new Point(14, 34), AutoSize = true };
        card.Controls.Add(lb);
        card.Controls.Add(vl);
        return card;
    }

    DataGridView NewGrid(ref int y)
    {
        // Use a fixed width matching the panel width
        var w = Math.Max(400, mainPanel.ClientSize.Width - 32);
        var grid = new DataGridView
        {
            Location = new Point(0, y + 28),
            Width = w,
            Height = 190,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Surface,
            ForeColor = TextPrimary,
            GridColor = Border,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            Font = new Font("Consolas", 9)
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Bg;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Consolas", 8, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(33, 38, 45);
        grid.DefaultCellStyle.SelectionForeColor = AccentBlue;
        grid.DefaultCellStyle.Padding = new Padding(8, 2, 8, 2);
        grid.RowTemplate.Height = 26;
        grid.ColumnHeadersHeight = 30;
        return grid;
    }
}
