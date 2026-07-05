[CmdletBinding()]
param(
    [string]$UnityPath = "D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe",
    [string]$ProjectPath = "",
    [int]$ShardCount = 8,
    [int]$TimeoutSeconds = 900,
    [string]$LogsPath = "",
    [switch]$NoBisect
)

$ErrorActionPreference = "Stop"

$scriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $scriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($LogsPath)) {
    $LogsPath = Join-Path $ProjectPath "Logs"
}

function Assert-CleanUnityState {
    $unityProcesses = Get-CimInstance Win32_Process |
        Where-Object { $_.Name -eq "Unity.exe" }
    if ($unityProcesses) {
        $details = $unityProcesses | ForEach-Object { "$($_.ProcessId) $($_.CommandLine)" }
        throw "Unity is already running. Close the editor before batch tests:`n$($details -join "`n")"
    }

    $lockPath = Join-Path $ProjectPath "Temp\UnityLockfile"
    if (Test-Path -LiteralPath $lockPath) {
        throw "Unity lock file exists: $lockPath"
    }
}

function Quote-Argument([string]$Value) {
    if ($Value -match "\s") {
        return '"' + ($Value -replace '"', '\"') + '"'
    }

    return $Value
}

function Get-LastStartedTest([string]$LogPath) {
    if (-not (Test-Path -LiteralPath $LogPath)) {
        return $null
    }

    $match = Get-Content -LiteralPath $LogPath -Tail 300 |
        Select-String -Pattern "LearnHearthstone EditMode test started:" |
        Select-Object -Last 1
    if ($match) {
        return ($match.Line -replace "^.*LearnHearthstone EditMode test started:\s*", "").Trim()
    }

    return $null
}

function Write-TestNameFile([string[]]$Names, [string]$Path) {
    $directory = Split-Path -Parent $Path
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    Set-Content -LiteralPath $Path -Value $Names -Encoding UTF8
}

function Add-Summary([string]$Message) {
    $summaryPath = Join-Path $LogsPath "EditModeBisectSummary.txt"
    $directory = Split-Path -Parent $summaryPath
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    Add-Content -LiteralPath $summaryPath -Value "$(Get-Date -Format o) $Message"
}

function Invoke-UnityEditModeRun {
    param(
        [string]$Label,
        [int]$ShardIndex = -1,
        [int]$TotalShards = 1,
        [string[]]$TestNames = $null
    )

    $safeLabel = $Label -replace "[^A-Za-z0-9_.-]", "_"
    $xmlPath = Join-Path $LogsPath "$safeLabel.xml"
    $logPath = Join-Path $LogsPath "$safeLabel.log"
    $manifestPath = Join-Path $LogsPath "$safeLabel.manifest.txt"
    $nameFilePath = Join-Path $LogsPath "$safeLabel.tests.txt"

    $arguments = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $ProjectPath,
        "-executeMethod", "LearnHearthstone.Editor.BatchEditModeTestRunner.RunEditMode",
        "-batchTestResults", $xmlPath,
        "-batchTestManifest", $manifestPath,
        "-logFile", $logPath,
        "-quit"
    )

    if ($ShardIndex -ge 0 -and $TotalShards -gt 1) {
        $arguments += @("-batchTestShardIndex", "$ShardIndex", "-batchTestShardCount", "$TotalShards")
    }

    if ($TestNames -and $TestNames.Count -gt 0) {
        Write-TestNameFile -Names $TestNames -Path $nameFilePath
        $arguments += @("-batchTestNameFile", $nameFilePath)
    }

    $argumentText = ($arguments | ForEach-Object { Quote-Argument $_ }) -join " "
    Add-Summary "START label=$Label shard=$ShardIndex/$TotalShards tests=$($TestNames.Count) xml=$xmlPath log=$logPath"
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentText -PassThru -WindowStyle Hidden
    $finished = $process.WaitForExit([Math]::Max(1, $TimeoutSeconds) * 1000)
    if (-not $finished) {
        Stop-Process -Id $process.Id -Force
        $lastStarted = Get-LastStartedTest -LogPath $logPath
        Add-Summary "TIMEOUT label=$Label pid=$($process.Id) lastStarted=$lastStarted log=$logPath manifest=$manifestPath"
        return [pscustomobject]@{
            Status = "Timeout"
            ExitCode = $null
            Label = $Label
            XmlPath = $xmlPath
            LogPath = $logPath
            ManifestPath = $manifestPath
            LastStarted = $lastStarted
        }
    }

    $status = if ($process.ExitCode -eq 0 -and (Test-Path -LiteralPath $xmlPath)) { "Passed" } else { "Failed" }
    $last = Get-LastStartedTest -LogPath $logPath
    Add-Summary "$status label=$Label exit=$($process.ExitCode) lastStarted=$last xmlExists=$(Test-Path -LiteralPath $xmlPath)"
    return [pscustomobject]@{
        Status = $status
        ExitCode = $process.ExitCode
        Label = $Label
        XmlPath = $xmlPath
        LogPath = $logPath
        ManifestPath = $manifestPath
        LastStarted = $last
    }
}

function Read-ManifestTests([string]$ManifestPath) {
    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        return @()
    }

    return @(Get-Content -LiteralPath $ManifestPath |
        Where-Object { $_ -and -not $_.Trim().StartsWith("#") } |
        ForEach-Object { $_.Trim() })
}

function Find-TimeoutTest {
    param([string[]]$Tests)

    $remaining = @($Tests)
    $round = 0
    while ($remaining.Count -gt 1) {
        $round += 1
        $half = [Math]::Max(1, [int][Math]::Ceiling($remaining.Count / 2))
        $left = @($remaining | Select-Object -First $half)
        $right = @($remaining | Select-Object -Skip $half)

        $leftResult = Invoke-UnityEditModeRun -Label "bisect-$round-left-$($left.Count)" -TestNames $left
        if ($leftResult.Status -eq "Timeout") {
            $remaining = $left
            continue
        }

        if ($right.Count -eq 0) {
            break
        }

        $rightResult = Invoke-UnityEditModeRun -Label "bisect-$round-right-$($right.Count)" -TestNames $right
        if ($rightResult.Status -eq "Timeout") {
            $remaining = $right
            continue
        }

        Add-Summary "BISECT stopped: timeout did not reproduce in either half. left=$($leftResult.Status) right=$($rightResult.Status)"
        return @()
    }

    return $remaining
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable not found: $UnityPath"
}

if (-not (Test-Path -LiteralPath $LogsPath)) {
    New-Item -ItemType Directory -Path $LogsPath | Out-Null
}

Assert-CleanUnityState
Add-Summary "BEGIN project=$ProjectPath shards=$ShardCount timeoutSeconds=$TimeoutSeconds"

$timedOutShard = $null
for ($index = 0; $index -lt [Math]::Max(1, $ShardCount); $index += 1) {
    Assert-CleanUnityState
    $result = Invoke-UnityEditModeRun -Label "editmode-shard-$index-of-$ShardCount" -ShardIndex $index -TotalShards $ShardCount
    if ($result.Status -eq "Timeout") {
        $timedOutShard = $result
        break
    }

    if ($result.Status -eq "Failed") {
        throw "Shard failed before timeout diagnosis completed. See $($result.LogPath) and $($result.XmlPath)"
    }
}

if (-not $timedOutShard) {
    Add-Summary "COMPLETE all shards passed"
    Write-Host "All EditMode shards passed. Summary: $(Join-Path $LogsPath 'EditModeBisectSummary.txt')"
    exit 0
}

if ($NoBisect) {
    Write-Host "Shard timed out. Summary: $(Join-Path $LogsPath 'EditModeBisectSummary.txt')"
    exit 2
}

$tests = Read-ManifestTests -ManifestPath $timedOutShard.ManifestPath
if ($tests.Count -eq 0) {
    Add-Summary "BISECT blocked: no manifest tests for $($timedOutShard.ManifestPath)"
    throw "Timed-out shard did not produce a readable manifest: $($timedOutShard.ManifestPath)"
}

$suspects = Find-TimeoutTest -Tests $tests
if ($suspects.Count -gt 0) {
    Add-Summary "SUSPECT tests=$($suspects -join ', ')"
    Write-Host "Suspect timeout test(s):"
    $suspects | ForEach-Object { Write-Host $_ }
    exit 2
}

Write-Host "Timeout did not reproduce during bisection. Check summary: $(Join-Path $LogsPath 'EditModeBisectSummary.txt')"
exit 3
