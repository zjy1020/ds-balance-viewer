namespace DSBalanceViewer;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Button btnRefresh;
    private Button btnChangeKey;
    private Label lblStatus;
    private Panel mainPanel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.btnRefresh = new Button();
        this.btnChangeKey = new Button();
        this.lblStatus = new Label();
        this.mainPanel = new Panel();

        this.SuspendLayout();

        // status bar at bottom
        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Padding = new Padding(10, 4, 10, 4)
        };

        this.lblStatus.Text = "就绪";
        this.lblStatus.Dock = DockStyle.Fill;
        this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        this.btnChangeKey = new Button { Text = "更换Key", AutoSize = true, Dock = DockStyle.Right };
        this.btnRefresh = new Button { Text = "刷新", AutoSize = true, Dock = DockStyle.Right };

        statusBar.Controls.Add(this.lblStatus);
        statusBar.Controls.Add(this.btnRefresh);
        statusBar.Controls.Add(this.btnChangeKey);

        // main scrollable area
        this.mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        // MainForm
        this.ClientSize = new Size(780, 560);
        this.MinimumSize = new Size(500, 350);
        this.Text = "DeepSeek 用量仪表盘";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Microsoft YaHei", 9);
        this.Controls.Add(this.mainPanel);
        this.Controls.Add(statusBar);

        this.ResumeLayout(false);
    }
}
