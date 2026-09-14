using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JianyingVideoAssistant.Infrastructure;
using Microsoft.Web.WebView2.Core;

namespace JianyingVideoAssistant;

public partial class MainWindow : Window
{
    private LocalWorkbenchHost? _localHost;
    private Uri? _workbenchUri;
    private bool _browserInitialized;
    private bool _starting;

    public MainWindow() => InitializeComponent();

    private async void OnLoaded(object sender, RoutedEventArgs e) => await StartAsync();

    private async Task StartAsync()
    {
        if (_starting) return;
        _starting = true;
        LoadingPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
        SetStatus("正在启动本地工作台", Brushes.DarkGoldenrod, true);
        try
        {
            if (_localHost is not null) await _localHost.DisposeAsync();
            _localHost = new LocalWorkbenchHost();
            await _localHost.StartAsync();
            _workbenchUri = _localHost.WorkbenchUri;
            await InitializeBrowserAsync();
            WorkbenchBrowser.CoreWebView2.Navigate(_workbenchUri.AbsoluteUri);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowError("安装包中的网页显示组件不可用，请重新安装剪映视频助手。");
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
        finally { _starting = false; }
    }

    private async Task InitializeBrowserAsync()
    {
        if (_browserInitialized) return;
        var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JianyingVideoAssistant", "WebView2");
        Directory.CreateDirectory(profile);
        var fixedRuntime = Path.Combine(AppContext.BaseDirectory, "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(Directory.Exists(fixedRuntime) ? fixedRuntime : null, profile);
        await WorkbenchBrowser.EnsureCoreWebView2Async(environment);
        var core = WorkbenchBrowser.CoreWebView2;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.ProcessFailed += (_, _) => Dispatcher.Invoke(() => ShowError("显示进程意外停止，请重新启动本地工作台。"));
        core.NewWindowRequested += (_, args) => { args.Handled = true; OpenExternal(args.Uri); };
        _browserInitialized = true;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        SetStatus("正在载入", Brushes.DarkGoldenrod, true);
        if (_workbenchUri is null || e.Uri.StartsWith(_workbenchUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;
        OpenExternal(e.Uri);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        if (e.IsSuccess)
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
            SetStatus("本机运行", (Brush)FindResource("SuccessBrush"), false);
        }
        else ShowError("本地页面载入失败，请重新启动。");
    }

    private void OnReloadClick(object sender, RoutedEventArgs e) => WorkbenchBrowser.CoreWebView2?.Reload();
    private async void OnRetryClick(object sender, RoutedEventArgs e) => await StartAsync();

    private void OnOpenDataFolderClick(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JianyingVideoAssistant", "Data");
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5) { OnReloadClick(sender, e); e.Handled = true; }
        else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Left && WorkbenchBrowser.CanGoBack) { WorkbenchBrowser.GoBack(); e.Handled = true; }
        else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0 && e.Key == Key.Right && WorkbenchBrowser.CanGoForward) { WorkbenchBrowser.GoForward(); e.Handled = true; }
    }

    private void ShowError(string detail)
    {
        try
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JianyingVideoAssistant", "Logs");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(Path.Combine(logDirectory, "client.log"), $"{DateTimeOffset.Now:O} {detail}{Environment.NewLine}");
        }
        catch { }
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorDetail.Text = detail;
        ErrorPanel.Visibility = Visibility.Visible;
        SetStatus("启动失败", (Brush)FindResource("ErrorBrush"), false);
    }

    private void SetStatus(string text, Brush brush, bool busy)
    {
        StatusText.Text = text;
        StatusDot.Fill = brush;
        StartupProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OpenExternal(string target)
    {
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        if (_localHost is not null) await _localHost.DisposeAsync();
    }

    private void OnSourceInitialized(object? sender, EventArgs e) => WindowBackdrop.TryApply(this);
}
