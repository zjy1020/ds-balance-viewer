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

        // status bar at bottom — TableLayoutPanel avoids Dock conflicts
        var statusBar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(10, 4, 10, 4)
        };
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        statusBar.ColumnStyles.Add(new ColumnStyle());
        statusBar.ColumnStyles.Add(new ColumnStyle());

        this.lblStatus.Text = "就绪";
        this.lblStatus.Dock = DockStyle.Fill;
        this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;

        this.btnRefresh = new Button { Text = "刷新", AutoSize = true };
        this.btnChangeKey = new Button { Text = "更换 Key", AutoSize = true };

        statusBar.Controls.Add(this.lblStatus, 0, 0);
        statusBar.Controls.Add(this.btnRefresh, 1, 0);
        statusBar.Controls.Add(this.btnChangeKey, 2, 0);

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
