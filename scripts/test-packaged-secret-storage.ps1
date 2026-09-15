param(
    [Parameter(Mandatory = $true)]
    [string]$ApplicationDirectory
)

$ErrorActionPreference = "Stop"
$serverDirectory = Join-Path $ApplicationDirectory "LocalWorkbench"
$serverExecutable = Join-Path $serverDirectory "JianyingVideoAssistant.Server.exe"
$runtime = Join-Path $env:TEMP ("JVA-Secret-Smoke-" + [Guid]::NewGuid().ToString("N"))
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()
$baseUrl = "http://127.0.0.1:$port/api/v1"
$testKey = "sk-packaged-secret-must-not-be-plaintext"

New-Item -ItemType Directory -Force $runtime | Out-Null
$env:PVA_RUNTIME_DIR = $runtime
$env:PVA_WORKSPACE_DIR = Join-Path $runtime "workspace"
$env:PVA_STATIC_DIR = Join-Path $serverDirectory "static-workbench"
$env:PVA_FFMPEG_BINARY = Join-Path $serverDirectory "ffmpeg\ffmpeg.exe"
$env:PVA_FFPROBE_BINARY = Join-Path $serverDirectory "ffmpeg\ffprobe.exe"
$env:PVA_DESKTOP_MODE = "1"
$env:PYTHONUTF8 = "1"
$process = Start-Process $serverExecutable -ArgumentList "--host", "127.0.0.1", "--port", $port -WorkingDirectory $serverDirectory -PassThru

try {
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        try {
            $health = Invoke-RestMethod "http://127.0.0.1:$port/api/health" -TimeoutSec 2
            if ($health.ok -eq $true) { break }
        } catch {
            Start-Sleep -Milliseconds 200
        }
    }
    if ($health.ok -ne $true) { throw "Packaged server health check failed." }

    $credentials = @{ username = "smoke-admin"; password = "smoke-password-123" } | ConvertTo-Json
    Invoke-RestMethod "$baseUrl/auth/bootstrap" -Method Post -ContentType "application/json" -Body $credentials | Out-Null
    $login = Invoke-RestMethod "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body $credentials
    $headers = @{ Authorization = "Bearer $($login.token)" }
    $profiles = Invoke-RestMethod "$baseUrl/model-profiles" -Headers $headers
    $profile = $profiles.profiles | Where-Object stage -eq "copywriting" | Select-Object -First 1
    $profile.base_url = "https://example.invalid/v1"
    $profile.model = "smoke-model"
    $profile.api_key = $testKey
    Invoke-RestMethod "$baseUrl/model-profiles/copywriting" -Method Put -Headers $headers -ContentType "application/json" -Body ($profile | ConvertTo-Json -Depth 8) | Out-Null
} finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit()
    }
}

$database = Join-Path $runtime "databases\workbench.db"
$rawDatabase = ""
foreach ($databaseFile in [System.IO.Directory]::GetFiles((Split-Path $database), "workbench.db*")) {
    $rawDatabase += [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($databaseFile))
}
if ($rawDatabase.Contains($testKey)) { throw "Plaintext API key found in packaged database." }
if (-not $rawDatabase.Contains("dpapi:v1:")) { throw "DPAPI ciphertext marker not found in packaged database." }

Write-Output "PACKAGED_SECRET_STORAGE_OK"
Write-Output $runtime
