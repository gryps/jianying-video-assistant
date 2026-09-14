param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$PipIndexUrl = "https://pypi.tuna.tsinghua.edu.cn/simple"
)

$ErrorActionPreference = "Stop"
$workbench = Join-Path $ProjectRoot "src\LocalWorkbench"
$venv = Join-Path $workbench ".venv-desktop"
$desktopDist = Join-Path $workbench "desktop-dist"
$serverBuild = Join-Path $workbench "build\server"

if (-not (Test-Path (Join-Path $workbench "desktop-dist\static-workbench\index.html"))) {
    throw "Missing desktop frontend assets in desktop-dist/static-workbench."
}

if (-not (Test-Path (Join-Path $venv "Scripts\python.exe"))) {
    py -3.12 -m venv $venv
    if ($LASTEXITCODE -ne 0) { throw "Python virtual environment creation failed." }
}

& (Join-Path $venv "Scripts\python.exe") -m pip install --disable-pip-version-check --timeout 120 --index-url $PipIndexUrl -r (Join-Path $workbench "requirements.txt") "pyinstaller==6.16.0"
if ($LASTEXITCODE -ne 0) { throw "Python dependency installation failed." }
if (Test-Path $serverBuild) { Remove-Item $serverBuild -Recurse -Force }
& (Join-Path $venv "Scripts\pyinstaller.exe") --noconfirm --clean --distpath $serverBuild --workpath (Join-Path $workbench "build\pyinstaller") (Join-Path $workbench "desktop_server.spec")
if ($LASTEXITCODE -ne 0) { throw "PyInstaller build failed." }

$serverOutput = Join-Path $serverBuild "JianyingVideoAssistant.Server"
Get-ChildItem $serverOutput -Force | Copy-Item -Destination $desktopDist -Recurse -Force

$ffmpeg = Get-Command ffmpeg.exe -ErrorAction SilentlyContinue
$ffprobe = Get-Command ffprobe.exe -ErrorAction SilentlyContinue
$ffmpegDist = Join-Path $desktopDist "ffmpeg"
New-Item -ItemType Directory -Path $ffmpegDist -Force | Out-Null
if ($ffmpeg -and $ffprobe) {
    Copy-Item $ffmpeg.Source (Join-Path $ffmpegDist "ffmpeg.exe") -Force
    Copy-Item $ffprobe.Source (Join-Path $ffmpegDist "ffprobe.exe") -Force
} elseif (-not (Test-Path (Join-Path $ffmpegDist "ffmpeg.exe")) -or -not (Test-Path (Join-Path $ffmpegDist "ffprobe.exe"))) {
    throw "ffmpeg.exe and ffprobe.exe are missing from PATH and desktop-dist/ffmpeg."
}

Write-Host "Local workbench build completed: $desktopDist"
