namespace DSBalanceViewer;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Button btnRefresh;
    private Button btnChangeKey;
    private Label lblStatus;
    private Label lblLastUpdate;
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
        this.lblLastUpdate = new Label();

        this.SuspendLayout();

        // ---- Top bar ----
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(22, 27, 34),
            Padding = new Padding(12, 8, 12, 8)
        };

        var brand = new Label
        {
            Text = "DEEPSEEK MONITOR",
            Font = new Font("Consolas", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(88, 166, 255),
            Location = new Point(12, 10),
            AutoSize = true
        };
        topBar.Controls.Add(brand);

        this.lblStatus = new Label
        {
            Text = "● 就绪",
            Font = new Font("Microsoft YaHei", 9),
            ForeColor = Color.FromArgb(63, 185, 80),
            Location = new Point(200, 13),
            AutoSize = true
        };
        topBar.Controls.Add(this.lblStatus);

        this.btnChangeKey = new Button
        {
            Text = "Keys",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.FromArgb(139, 148, 158),
            FlatAppearance = { BorderSize = 0 },
            Location = new Point(0, 8),
            Size = new Size(56, 28),
            Font = new Font("Consolas", 9)
        };
        this.btnChangeKey.FlatAppearance.BorderSize = 0;

        this.btnRefresh = new Button
        {
            Text = "刷新",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(33, 38, 45),
            ForeColor = Color.FromArgb(201, 209, 217),
            FlatAppearance = { BorderSize = 0 },
            Size = new Size(56, 28),
            Font = new Font("Microsoft YaHei", 9)
        };

        var btnPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Right,
            Width = 126,
            BackColor = Color.Transparent
        };
        btnPanel.Controls.Add(this.btnRefresh);
        btnPanel.Controls.Add(this.btnChangeKey);
        topBar.Controls.Add(btnPanel);

        // ---- Bottom bar ----
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Color.FromArgb(22, 27, 34),
            Padding = new Padding(12, 4, 12, 4)
        };

        this.lblLastUpdate = new Label
        {
            Text = "最后更新: —",
            Font = new Font("Consolas", 8),
            ForeColor = Color.FromArgb(72, 79, 88),
            Dock = DockStyle.Fill
        };
        bottomBar.Controls.Add(this.lblLastUpdate);

        // ---- Main scroll area ----
        this.mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(13, 17, 23),
            Padding = new Padding(16)
        };
        this.mainPanel.HorizontalScroll.Enabled = false;

        // MainForm
        this.ClientSize = new Size(760, 580);
        this.MinimumSize = new Size(560, 400);
        this.Text = "DeepSeek Monitor";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(13, 17, 23);
        this.Font = new Font("Microsoft YaHei", 9);
        this.Controls.Add(this.mainPanel);
        this.Controls.Add(bottomBar);
        this.Controls.Add(topBar);

        this.ResumeLayout(false);
    }
}
