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

        // tab pages
        this.tabDashboard.Text = "仪表盘";
        this.tabBalance.Text = "余额";
        this.tabUsage.Text = "用量";
        this.tabCost.Text = "费用";

        // status bar: TableLayoutPanel at bottom
        var statusBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            ColumnCount = 3,
            Padding = new Padding(8, 4, 8, 4)
        };
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // status text, fills rest
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // change-key button
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));       // refresh button

        // lblStatus
        this.lblStatus.Text = "就绪";
        this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        this.lblStatus.Dock = DockStyle.Fill;
        this.lblStatus.AutoSize = true;

        // btnChangeKey
        this.btnChangeKey.Text = "更换Key";
        this.btnChangeKey.AutoSize = true;
        this.btnChangeKey.Margin = new Padding(4, 0, 4, 0);

        // btnRefresh
        this.btnRefresh.Text = "刷新";
        this.btnRefresh.AutoSize = true;

        statusBar.Controls.Add(this.lblStatus, 0, 0);
        statusBar.Controls.Add(this.btnChangeKey, 1, 0);
        statusBar.Controls.Add(this.btnRefresh, 2, 0);

        // MainForm
        this.ClientSize = new Size(780, 560);
        this.MinimumSize = new Size(600, 400);
        this.Text = "DeepSeek 用量仪表盘";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Microsoft YaHei", 9);
        this.Controls.Add(this.tabControl);
        this.Controls.Add(statusBar);

        this.ResumeLayout(false);
    }
}
