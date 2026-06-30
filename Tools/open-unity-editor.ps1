[CmdletBinding()]
param(
    [string]$UnityExe = 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe',
    [int]$StartupTimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectName = Split-Path -Leaf $projectRoot
$logPath = Join-Path $projectRoot 'Logs\UnityInteractiveOpen.log'
$lockPath = Join-Path $projectRoot 'Temp\UnityLockfile'

if (-not (Test-Path -LiteralPath $UnityExe)) {
    throw "Unity executable not found: $UnityExe"
}

$unityProcesses = @(Get-Process Unity -ErrorAction SilentlyContinue)
$openProjectProcess = $unityProcesses | Where-Object {
    $_.MainWindowTitle -like "*$projectName*" -or $_.MainWindowTitle -eq 'Opening project...'
} | Select-Object -First 1

if ($openProjectProcess) {
    Write-Host "Unity is already open for this project. PID: $($openProjectProcess.Id)"
    Write-Host "Window: $($openProjectProcess.MainWindowTitle)"
    exit 0
}

if ((Test-Path -LiteralPath $lockPath) -and $unityProcesses.Count -eq 0) {
    Write-Host "Removing stale Unity lockfile: $lockPath"
    Remove-Item -LiteralPath $lockPath -Force
}
elseif (Test-Path -LiteralPath $lockPath) {
    throw "Unity lockfile exists while Unity process(es) are running. Close Unity first or inspect: $lockPath"
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath) | Out-Null
Remove-Item -LiteralPath $logPath -ErrorAction SilentlyContinue

$argumentLine = '-projectPath "{0}" -logFile "{1}"' -f $projectRoot, $logPath
$process = Start-Process -FilePath $UnityExe -ArgumentList $argumentLine -PassThru
Write-Host "Started Unity. PID: $($process.Id)"
Write-Host "Log: $logPath"

$deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
do {
    Start-Sleep -Seconds 2
    $current = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    if (-not $current) {
        Write-Host "Unity exited during startup."
        if (Test-Path -LiteralPath $logPath) {
            Get-Content -LiteralPath $logPath -Tail 120
        }

        exit 1
    }

    if ($current.MainWindowTitle -like "*$projectName*") {
        Write-Host "Unity opened successfully."
        Write-Host "Window: $($current.MainWindowTitle)"
        exit 0
    }
} while ((Get-Date) -lt $deadline)

Write-Host "Unity is still starting. PID: $($process.Id)"
Write-Host "Current window: $($current.MainWindowTitle)"
exit 0
