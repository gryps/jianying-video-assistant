using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace JianyingVideoAssistant.Infrastructure;

public static class WindowBackdrop
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmSystemBackdropNone = 1;
    private const int DwmSystemBackdropMainWindow = 2;

    public static void TryApply(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var darkMode = ThemeManager.IsDarkTheme ? 1 : 0;
            var backdrop = ThemeManager.IsHighContrast ? DwmSystemBackdropNone : DwmSystemBackdropMainWindow;
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));
            var result = DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
            if (result == 0 && PresentationSource.FromVisual(window) is HwndSource source)
            {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }
        catch (Exception)
        {
            // Solid application brushes remain the safe fallback.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
