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
    private Button btnChangeKey;
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
        this.btnChangeKey = new Button();
        this.lblStatus = new Label();

        this.SuspendLayout();

        // tabControl
        this.tabControl.Controls.Add(this.tabDashboard);
        this.tabControl.Controls.Add(this.tabBalance);
        this.tabControl.Controls.Add(this.tabUsage);
        this.tabControl.Controls.Add(this.tabCost);
        this.tabControl.Dock = DockStyle.Fill;
        this.tabControl.SelectedIndex = 0;

        this.tabDashboard.Text = "仪表盘";
        this.tabBalance.Text = "余额";
        this.tabUsage.Text = "用量";
        this.tabCost.Text = "费用";

        // status bar
        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Padding = new Padding(8, 4, 8, 4)
        };

        this.lblStatus.Text = "就绪";
        this.lblStatus.Dock = DockStyle.Left;
        this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        this.lblStatus.AutoSize = true;

        this.btnChangeKey.Text = "更换Key";
        this.btnChangeKey.Dock = DockStyle.Right;
        this.btnChangeKey.AutoSize = true;

        this.btnRefresh.Text = "刷新";
        this.btnRefresh.Dock = DockStyle.Right;
        this.btnRefresh.AutoSize = true;

        statusBar.Controls.Add(this.btnRefresh);
        statusBar.Controls.Add(this.btnChangeKey);
        statusBar.Controls.Add(this.lblStatus);

        // MainForm
        this.ClientSize = new Size(780, 560);
        this.MinimumSize = new Size(560, 380);
        this.Text = "DeepSeek 用量仪表盘";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Microsoft YaHei", 9);
        this.Controls.Add(this.tabControl);
        this.Controls.Add(statusBar);

        this.ResumeLayout(false);
    }
}
