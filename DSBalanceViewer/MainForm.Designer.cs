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
        this.btnRefresh.Width = 60;

        // btnChangeKey
        this.btnChangeKey.Text = "更换Key";
        this.btnChangeKey.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnChangeKey.Width = 75;

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
        statusPanel.Controls.Add(btnChangeKey);
        statusPanel.Controls.Add(btnRefresh);
        statusPanel.Controls.Add(lblStatus);
        btnChangeKey.Location = new Point(statusPanel.Width - 190, 7);
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
