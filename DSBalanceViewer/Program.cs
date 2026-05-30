namespace DSBalanceViewer;

static class Program
{
    private static readonly Mutex _mutex = new(true, "DSBalanceViewer_SingleInstance");

    [STAThread]
    static void Main()
    {
        if (!_mutex.WaitOne(TimeSpan.Zero, true))
        {
            MessageBox.Show("DeepSeek 用量仪表盘已在运行中。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
}