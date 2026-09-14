using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace JianyingVideoAssistant.Infrastructure;

internal sealed class LocalWorkbenchHost : IAsyncDisposable
{
    private Process? _process;

    public Uri WorkbenchUri { get; private set; } = null!;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_process is { HasExited: false }) return;

        LocalWorkbenchInstallation.Validate(AppContext.BaseDirectory);
        var serverDirectory = LocalWorkbenchInstallation.ServerDirectory(AppContext.BaseDirectory);
        var executable = Path.Combine(serverDirectory, "JianyingVideoAssistant.Server.exe");
        var staticDirectory = Path.Combine(serverDirectory, "static-workbench");
        var dataRoot = LocalWorkbenchInstallation.DataRoot;
        Directory.CreateDirectory(dataRoot);
        var port = ReserveLoopbackPort();
        WorkbenchUri = new Uri($"http://127.0.0.1:{port}/workbench/");

        var startInfo = new ProcessStartInfo(executable, $"--host 127.0.0.1 --port {port}")
        {
            WorkingDirectory = serverDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.Environment["PVA_RUNTIME_DIR"] = dataRoot;
        startInfo.Environment["PVA_WORKSPACE_DIR"] = Path.Combine(dataRoot, "workspace");
        startInfo.Environment["PVA_STATIC_DIR"] = staticDirectory;
        startInfo.Environment["PVA_FFMPEG_BINARY"] = Path.Combine(serverDirectory, "ffmpeg", "ffmpeg.exe");
        startInfo.Environment["PVA_FFPROBE_BINARY"] = Path.Combine(serverDirectory, "ffmpeg", "ffprobe.exe");
        startInfo.Environment["PVA_DESKTOP_MODE"] = "1";
        startInfo.Environment["PYTHONUTF8"] = "1";

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("本地工作台进程无法启动。");
        await WaitUntilReadyAsync(cancellationToken);
    }

    private async Task WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var healthUri = new Uri(WorkbenchUri, "/api/health");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_process?.HasExited == true)
                throw new InvalidOperationException($"本地工作台启动失败（退出代码 {_process.ExitCode}）。");
            try
            {
                using var response = await client.GetAsync(healthUri, cancellationToken);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            await Task.Delay(200, cancellationToken);
        }
        throw new TimeoutException("本地工作台启动超时，请重新启动应用。");
    }

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(3000);
        }
        _process?.Dispose();
        _process = null;
        return ValueTask.CompletedTask;
    }
}
