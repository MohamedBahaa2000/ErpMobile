using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ErpMobile.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        private const string LogPath = @"D:\ErpMobile\crash.log";

        private static void Log(string tag, string message)
        {
            try
            {
                System.IO.File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {tag}\n{message}\n\n");
            }
            catch
            {
                // لا ندع تسجيل الخطأ يمنع التطبيق
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Log("UNHANDLED", e.ExceptionObject?.ToString() ?? "unknown");

            TaskScheduler.UnobservedTaskException += (_, e) =>
                Log("TASK", e.Exception?.ToString() ?? "unknown");

            this.UnhandledException += (_, e) =>
            {
                Log("WINUI", e.Exception.ToString());
                e.Handled = true;
            };

            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
