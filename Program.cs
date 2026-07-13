using System.Runtime.InteropServices;
using TwitchIrcMinimal;
using TwitchIrcMinimal.Gui;

namespace TwitchIrcMinimal;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error($"FATAL: {ex}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error($"UNOBSERVED: {e.Exception}");
            e.SetObserved();
        };

        Application.Run(new TrayForm());
    }
}
