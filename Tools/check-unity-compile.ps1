param(
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'

function Get-UnityVersion {
    param([string]$Root)

    $versionFile = Join-Path $Root 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile)) {
        throw "ProjectVersion.txt not found under $Root"
    }

    $line = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } | Select-Object -First 1
    if (-not $line) {
        throw "Could not read Unity editor version from $versionFile"
    }

    return ($line -replace '^m_EditorVersion:\s*', '').Trim()
}

function Resolve-UnityPath {
    param(
        [string]$Root,
        [string]$ExplicitPath
    )

    if ($ExplicitPath -and (Test-Path -LiteralPath $ExplicitPath)) {
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $version = Get-UnityVersion -Root $Root
    $candidates = @(
        "D:\unity hub Editor\$version\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "C:\Program Files\Unity $version\Editor\Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $shortcut = "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Unity $version\Unity.lnk"
    if (Test-Path -LiteralPath $shortcut) {
        $shell = New-Object -ComObject WScript.Shell
        $target = $shell.CreateShortcut($shortcut).TargetPath
        if ($target -and (Test-Path -LiteralPath $target)) {
            return (Resolve-Path -LiteralPath $target).Path
        }
    }

    throw "Unity $version executable was not found. Pass -UnityPath explicitly."
}

$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$unityExe = Resolve-UnityPath -Root $projectRoot -ExplicitPath $UnityPath
$logDir = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$logPath = Join-Path $logDir 'CodexCompileCheck.log'
Write-Host "Unity: $unityExe"
Write-Host "Project: $projectRoot"
Write-Host "Log: $logPath"

$unityArgs = '-batchmode -quit -projectPath "{0}" -logFile "{1}"' -f $projectRoot, $logPath
$unityProcess = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -NoNewWindow -Wait -PassThru
$unityExitCode = $unityProcess.ExitCode

for ($attempt = 0; $attempt -lt 20 -and -not (Test-Path -LiteralPath $logPath); $attempt += 1) {
    Start-Sleep -Milliseconds 250
}

if (-not (Test-Path -LiteralPath $logPath)) {
    throw "Unity did not create a log file at $logPath"
}

$failurePattern = 'error CS\d+|Compilation failed|Scripts have compiler errors|Safe Mode|Aborting batchmode due to failure|Fatal Error'
$failureLines = Select-String -Path $logPath -Pattern $failurePattern -CaseSensitive:$false
$successPattern = 'Tundra build success|Mono: successfully reloaded assembly|CompileScripts'
$successLines = Select-String -Path $logPath -Pattern $successPattern -CaseSensitive:$false

if ($failureLines -or ($unityExitCode -ne 0 -and -not $successLines)) {
    Write-Host ''
    Write-Host 'Unity compile check failed:'
    if ($failureLines) {
        $failureLines | Select-Object -First 80 | ForEach-Object { Write-Host $_.Line }
    }
    else {
        Write-Host "Unity exited with code $unityExitCode"
    }

    exit 1
}

if ($unityExitCode -ne 0) {
    Write-Warning "Unity exited with code $unityExitCode, but the compile log contains no compiler errors."
}

Write-Host 'Unity compile check passed.'
exit 0
