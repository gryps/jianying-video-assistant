using System.Runtime.InteropServices;

namespace JianyingVideoAssistant.Infrastructure;

public static class DesktopShortcut
{
    public static void EnsureForCurrentUser()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop)) return;
            var executable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable)) return;
            var shortcutPath = Path.Combine(desktop, "剪映视频助手.lnk");
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return;
            object? shell = null;
            object? shortcut = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                if (shell is null) return;
                shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, [shortcutPath]);
                if (shortcut is null) return;
                var shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, [executable]);
                shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, [AppContext.BaseDirectory]);
                shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, [$"{executable},0"]);
                shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, ["剪映视频助手"]);
                shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut is not null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
                if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
            }
        }
        catch
        {
            // Shortcut creation is a convenience and must never block application startup.
        }
    }
}
