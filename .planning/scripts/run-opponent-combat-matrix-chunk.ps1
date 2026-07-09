param(
    [int]$ChunkIndex = 0,
    [int]$ChunkSize = 17,
    [int]$PortStart = 6400,
    [int]$PortEnd = 6410,
    [int]$TimeoutMs = 30000,
    [int]$PollSeconds = 2,
    [int]$MaxPolls = 180
)

$ErrorActionPreference = "Stop"

function Read-Exact {
    param(
        [System.IO.Stream]$Stream,
        [int]$Length
    )

    $buffer = New-Object byte[] $Length
    $offset = 0
    while ($offset -lt $Length) {
        $read = $Stream.Read($buffer, $offset, $Length - $offset)
        if ($read -le 0) {
            throw "Connection closed while reading response."
        }

        $offset += $read
    }

    return $buffer
}

function Invoke-UnityMcpCommand {
    param(
        [int]$Port,
        [hashtable]$Payload,
        [int]$CommandTimeoutMs = $TimeoutMs
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    $connect = $client.BeginConnect("127.0.0.1", $Port, $null, $null)
    if (-not $connect.AsyncWaitHandle.WaitOne($CommandTimeoutMs)) {
        $client.Close()
        throw "Timed out connecting to Unity MCP port $Port."
    }

    try {
        $client.EndConnect($connect)
        $stream = $client.GetStream()
        $stream.ReadTimeout = $CommandTimeoutMs
        $stream.WriteTimeout = $CommandTimeoutMs

        $quietReads = 0
        while ($quietReads -lt 6) {
            Start-Sleep -Milliseconds 75
            if ($client.Available -le 0) {
                $quietReads += 1
                continue
            }

            while ($client.Available -gt 0) {
                $banner = New-Object byte[] ([Math]::Min($client.Available, 1024))
                [void]$stream.Read($banner, 0, $banner.Length)
                Start-Sleep -Milliseconds 25
            }

            $quietReads = 0
        }

        $json = $Payload | ConvertTo-Json -Depth 32 -Compress
        $body = [System.Text.Encoding]::UTF8.GetBytes($json)
        $length = [BitConverter]::GetBytes([Int64]$body.Length)
        if ([BitConverter]::IsLittleEndian) {
            [Array]::Reverse($length)
        }

        $stream.Write($length, 0, 8)
        $stream.Write($body, 0, $body.Length)

        $header = Read-Exact -Stream $stream -Length 8
        if ([BitConverter]::IsLittleEndian) {
            [Array]::Reverse($header)
        }

        $responseLength = [BitConverter]::ToInt64($header, 0)
        if ($responseLength -lt 0 -or $responseLength -gt [int]::MaxValue) {
            throw "Unexpected Unity MCP response length: $responseLength."
        }

        $response = Read-Exact -Stream $stream -Length ([int]$responseLength)
        return [System.Text.Encoding]::UTF8.GetString($response) | ConvertFrom-Json
    }
    finally {
        $client.Close()
    }
}

function Find-UnityMcpPort {
    for ($port = $PortStart; $port -le $PortEnd; $port += 1) {
        try {
            $state = Invoke-UnityMcpCommand -Port $port -Payload @{ type = "get_editor_state"; params = @{} } -CommandTimeoutMs 5000
            if ($state.status -eq "success" -and $state.result.success) {
                return $port
            }
        }
        catch {
            continue
        }
    }

    throw "No responsive Unity MCP bridge found on ports $PortStart-$PortEnd."
}

function ConvertTo-TestNameSafe {
    param([string]$Value)

    $chars = foreach ($ch in $Value.ToCharArray()) {
        if ([char]::IsLetterOrDigit($ch)) {
            $ch
        }
        else {
            "_"
        }
    }

    $result = -join $chars
    while ($result.Contains("__")) {
        $result = $result.Replace("__", "_")
    }

    return $result.Trim("_")
}

function Split-MarkdownRow {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line) -or -not $Line.StartsWith("|", [StringComparison]::Ordinal)) {
        return @()
    }

    return @($Line.Trim("|").Split("|") | ForEach-Object { $_.Trim() })
}

function Get-OpponentCombatMatrixTests {
    $matrixPath = Join-Path (Get-Location) ".planning\tavern-ui-screenshot-requirements\step-4-opponent-combat-mechanics-blackbox-cases.md"
    $rows = @()
    $trinketIndex = 0

    foreach ($rawLine in Get-Content -LiteralPath $matrixPath) {
        $parts = Split-MarkdownRow -Line $rawLine.Trim()
        if ($parts.Length -eq 0) {
            continue
        }

        if ($parts[0].StartsWith("OCM-BB-HP-", [StringComparison]::Ordinal)) {
            $caseId = $parts[0]
            $name = $parts[1]
        }
        elseif (($parts[0] -eq "Greater" -or $parts[0] -eq "Lesser") -and $parts.Length -ge 5 -and $parts[1].StartsWith('`', [StringComparison]::Ordinal)) {
            $trinketIndex += 1
            $caseId = "OCM-BB-TR-" + $trinketIndex.ToString("000")
            $name = $parts[2]
        }
        else {
            continue
        }

        $safeName = ConvertTo-TestNameSafe -Value ($caseId + "_" + $name)
        $rows += [pscustomobject]@{
            Index = $rows.Count
            CaseId = $caseId
            Name = $name
            TestName = "LearnHearthstone.Tests.EditMode.OpponentCombatMechanicBlackBoxTests.RunCombatTest_" + $safeName
        }
    }

    return $rows
}

$allTests = @(Get-OpponentCombatMatrixTests)
if ($allTests.Count -ne 102) {
    throw "Expected 102 matrix tests, found $($allTests.Count)."
}

$skip = $ChunkIndex * $ChunkSize
$chunk = @($allTests | Select-Object -Skip $skip -First $ChunkSize)
if ($chunk.Count -eq 0) {
    throw "Chunk $ChunkIndex is empty for chunk size $ChunkSize."
}

$port = Find-UnityMcpPort
$start = Invoke-UnityMcpCommand -Port $port -Payload @{
    type = "run_tests"
    params = @{
        mode = "EditMode"
        testNames = @($chunk | ForEach-Object { $_.TestName })
    }
}

$jobId = $start.result.data.job_id
$lastData = $null
for ($poll = 1; $poll -le $MaxPolls; $poll += 1) {
    $job = Invoke-UnityMcpCommand -Port $port -Payload @{
        type = "get_test_job"
        params = @{
            job_id = $jobId
            include_details = $false
            include_failed_tests = $true
        }
    } -CommandTimeoutMs 20000

    $lastData = $job.result.data
    if ($lastData.status -eq "running") {
        Start-Sleep -Seconds $PollSeconds
        continue
    }

    break
}

if ($null -eq $lastData) {
    throw "Unity test job $jobId produced no poll data."
}

$failedTests = @()
if ($lastData.result -and $lastData.result.failed_tests) {
    $failedTests = @($lastData.result.failed_tests)
}
elseif ($lastData.progress -and $lastData.progress.failures_so_far) {
    $failedTests = @($lastData.progress.failures_so_far)
}

$completed = $chunk.Count
if ($lastData.progress -and $null -ne $lastData.progress.completed) {
    $completed = [int]$lastData.progress.completed
}
elseif ($lastData.result -and $lastData.result.summary -and $null -ne $lastData.result.summary.total) {
    $completed = [int]$lastData.result.summary.total
}

$failed = $failedTests.Count
if ($lastData.result -and $lastData.result.summary -and $null -ne $lastData.result.summary.failed) {
    $failed = [int]$lastData.result.summary.failed
}

[pscustomobject]@{
    chunkIndex = $ChunkIndex
    chunkSize = $ChunkSize
    port = $port
    jobId = $jobId
    status = $lastData.status
    startCase = $chunk[0].CaseId
    endCase = $chunk[$chunk.Count - 1].CaseId
    requested = $chunk.Count
    completed = $completed
    passed = $completed - $failed
    failed = $failed
    failedTests = @($failedTests | ForEach-Object {
        [pscustomobject]@{
            fullName = $_.full_name
            message = $_.message
        }
    })
} | ConvertTo-Json -Depth 8
