using System.Windows;
using JianyingVideoAssistant.Infrastructure;

namespace JianyingVideoAssistant;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DesktopShortcut.EnsureForCurrentUser();
        ThemeManager.Initialize(this);
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeManager.Shutdown();
        base.OnExit(e);
    }
}
