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

    // BuildDashboard / BuildBalanceTab / BuildUsageTab / BuildCostTab
    // will be implemented in subsequent tasks
    private void BuildDashboard() { }
    private void BuildBalanceTab() { }
    private void BuildUsageTab() { }
    private void BuildCostTab() { }
}
