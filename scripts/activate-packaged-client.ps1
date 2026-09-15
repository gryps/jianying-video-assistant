param(
    [Parameter(Mandatory = $true)]
    [string]$ApplicationDirectory
)

$ErrorActionPreference = "Stop"
$executable = Join-Path $ApplicationDirectory "JianyingVideoAssistant.exe"
if (-not (Test-Path $executable)) { throw "Application executable not found: $executable" }

$shell = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath("Desktop")
$updated = 0
foreach ($linkFile in Get-ChildItem $desktop -Filter "*.lnk") {
    $link = $shell.CreateShortcut($linkFile.FullName)
    if ([System.IO.Path]::GetFileName($link.TargetPath) -eq "JianyingVideoAssistant.exe") {
        $link.TargetPath = $executable
        $link.WorkingDirectory = $ApplicationDirectory
        $link.IconLocation = "$executable,0"
        $link.Save()
        $updated++
    }
}
if ($updated -eq 0) {
    $link = $shell.CreateShortcut((Join-Path $desktop "JianyingVideoAssistant.lnk"))
    $link.TargetPath = $executable
    $link.WorkingDirectory = $ApplicationDirectory
    $link.IconLocation = "$executable,0"
    $link.Save()
}

Get-Process JianyingVideoAssistant -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Process $executable
