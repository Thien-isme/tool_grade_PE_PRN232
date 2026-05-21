using System.Threading;
using System.Windows.Forms;

namespace ApiRunnerTool.API.Helpers
{
    /// <summary>
    /// Mo hop thoai chon thu muc tren desktop (STA thread). Khong dung PowerShell an.
    /// </summary>
    public static class NativeFolderPicker
    {
        public static (bool Cancelled, string? Path, string? Error) Pick(string description, int timeoutSeconds = 120)
        {
            string? selectedPath = null;
            Exception? threadError = null;

            var thread = new Thread(() =>
            {
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using var dialog = new FolderBrowserDialog
                    {
                        Description = description,
                        ShowNewFolderButton = false,
                        UseDescriptionForTitle = true
                    };
                    var owner = new Form
                    {
                        TopMost = true,
                        ShowInTaskbar = true,
                        WindowState = FormWindowState.Minimized,
                        ShowIcon = false,
                        Width = 0,
                        Height = 0
                    };
                    owner.Show();
                    owner.Hide();
                    var result = dialog.ShowDialog(owner);
                    owner.Close();
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                        selectedPath = dialog.SelectedPath;
                }
                catch (Exception ex)
                {
                    threadError = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = false;
            thread.Start();

            if (!thread.Join(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                try { thread.Interrupt(); } catch { }
                return (true, null, "Hết thời gian chờ chọn thư mục.");
            }

            if (threadError != null)
                return (true, null, threadError.Message);

            if (string.IsNullOrEmpty(selectedPath))
                return (true, null, null);

            return (false, selectedPath, null);
        }
    }
}
