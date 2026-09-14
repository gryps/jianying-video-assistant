using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JianyingVideoAssistant.Infrastructure;
using Microsoft.Web.WebView2.Core;

namespace JianyingVideoAssistant;

public partial class MainWindow : Window
{
    private readonly Uri _workbenchUri = WorkbenchEndpoint.Resolve();
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync()
    {
        if (_initialized)
        {
            WorkbenchBrowser.CoreWebView2?.Navigate(_workbenchUri.AbsoluteUri);
            return;
        }

        SetLoadingState("正在连接", Brushes.DarkGoldenrod);
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;

        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JianyingVideoAssistant",
                "WebView2");
            Directory.CreateDirectory(userDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await WorkbenchBrowser.EnsureCoreWebView2Async(environment);

            var core = WorkbenchBrowser.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.NavigationStarting += OnNavigationStarting;
            core.NavigationCompleted += OnNavigationCompleted;
            core.ProcessFailed += OnBrowserProcessFailed;
            core.NewWindowRequested += OnNewWindowRequested;
            _initialized = true;
            core.Navigate(_workbenchUri.AbsoluteUri);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowError("此电脑缺少 Microsoft Edge WebView2 Runtime。请先安装 WebView2 Runtime，或暂时在浏览器中打开工作台。");
        }
        catch (Exception exception)
        {
            ShowError($"浏览器组件启动失败：{exception.Message}");
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        NavigationProgress.Visibility = Visibility.Visible;
        SetLoadingState("正在载入", Brushes.DarkGoldenrod);
        if (!e.Uri.StartsWith(_workbenchUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            OpenExternal(e.Uri);
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        NavigationProgress.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Collapsed;
        if (e.IsSuccess)
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
            SetLoadingState("已连接", (Brush)FindResource("SuccessBrush"));
            return;
        }

        ShowError($"无法连接 {WorkbenchEndpoint.DisplayHost}。请确认服务器已开机并与此电脑处于同一网络，然后重试。");
    }

    private void OnBrowserProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        Dispatcher.Invoke(() => ShowError("工作台显示进程意外停止，请点击“重试”重新载入。"));
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        OpenExternal(e.Uri);
    }

    private void OnReloadClick(object sender, RoutedEventArgs e)
    {
        if (_initialized) WorkbenchBrowser.Reload();
        else _ = InitializeBrowserAsync();
    }

    private void OnRetryClick(object sender, RoutedEventArgs e)
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;
        if (_initialized) WorkbenchBrowser.CoreWebView2.Navigate(_workbenchUri.AbsoluteUri);
        else _ = InitializeBrowserAsync();
    }

    private void OnOpenBrowserClick(object sender, RoutedEventArgs e) => OpenExternal(_workbenchUri.AbsoluteUri);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            OnReloadClick(sender, e);
            e.Handled = true;
        }
        else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Left && WorkbenchBrowser.CanGoBack)
        {
            WorkbenchBrowser.GoBack();
            e.Handled = true;
        }
        else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Right && WorkbenchBrowser.CanGoForward)
        {
            WorkbenchBrowser.GoForward();
            e.Handled = true;
        }
    }

    private void ShowError(string detail)
    {
        NavigationProgress.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorDetail.Text = detail;
        ErrorPanel.Visibility = Visibility.Visible;
        SetLoadingState("连接失败", (Brush)FindResource("ErrorBrush"));
    }

    private void SetLoadingState(string text, Brush brush)
    {
        ConnectionText.Text = text;
        ConnectionDot.Fill = brush;
    }

    private static void OpenExternal(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
            // The actionable in-app retry remains available if Windows has no URL handler.
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e) => WindowBackdrop.TryApply(this);
}
