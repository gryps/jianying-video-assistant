param(
    [string]$OutputPath = (Join-Path $env:TEMP "jianying-video-assistant-current.png"),
    [string]$ExecutablePath = (Join-Path $env:LOCALAPPDATA "JianyingVideoAssistant\App-V10-SecureStandalone\JianyingVideoAssistant.exe")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WindowCaptureNativeMethods {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
}
"@

$process = Get-Process JianyingVideoAssistant -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $process) {
    if (-not (Test-Path $ExecutablePath)) { throw "找不到当前客户端：$ExecutablePath" }
    $process = Start-Process $ExecutablePath -PassThru
}

for ($attempt = 0; $attempt -lt 20 -and $process.MainWindowHandle -eq 0; $attempt++) {
    Start-Sleep -Milliseconds 500
    $process.Refresh()
}
if ($process.MainWindowHandle -eq 0) { throw "客户端没有可见主窗口" }

[WindowCaptureNativeMethods]::ShowWindowAsync($process.MainWindowHandle, 9) | Out-Null
[WindowCaptureNativeMethods]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
Start-Sleep -Seconds 2

$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}

Write-Output $OutputPath
