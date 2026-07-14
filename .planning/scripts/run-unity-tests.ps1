param(
    [string[]]$TestName = @("LearnHearthstone.Tests.EditMode.TrinketSystemTests"),
    [string]$Mode = "EditMode",
    [string]$RefreshScope = "all",
    [string]$Compile = "request",
    [switch]$SkipRefresh,
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

        $handshakeBytes = [System.Collections.Generic.List[byte]]::new()
        do {
            $handshakeBytes.Add((Read-Exact -Stream $stream -Length 1)[0])
            if ($handshakeBytes.Count -gt 128) {
                throw "Unity MCP handshake exceeded 128 bytes."
            }
        } while ($handshakeBytes[$handshakeBytes.Count - 1] -ne 10)

        $handshake = [System.Text.Encoding]::ASCII.GetString($handshakeBytes.ToArray()).TrimEnd()
        if (-not $handshake.StartsWith("WELCOME UNITY-MCP ")) {
            throw "Unexpected Unity MCP handshake: $handshake"
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

function Wait-UnityIdle {
    param([int]$InitialPort)

    $port = $InitialPort
    for ($attempt = 1; $attempt -le $MaxPolls; $attempt += 1) {
        try {
            $state = Invoke-UnityMcpCommand -Port $port -Payload @{ type = "get_editor_state"; params = @{} } -CommandTimeoutMs 10000
            $data = $state.result.data
            $phase = $data.activity.phase
            $compiling = [bool]$data.compilation.is_compiling
            $updating = [bool]$data.assets.is_updating

            Write-Host "Unity state: port=$port phase=$phase compiling=$compiling updating=$updating"
            if ($phase -eq "idle" -and -not $compiling -and -not $updating) {
                return $port
            }
        }
        catch {
            Start-Sleep -Seconds $PollSeconds
            try {
                $port = Find-UnityMcpPort
            }
            catch {
                Write-Host "Unity MCP bridge is temporarily unavailable while Unity reloads; continuing to wait."
            }
            continue
        }

        Start-Sleep -Seconds $PollSeconds
    }

    throw "Unity did not become idle after $MaxPolls polls."
}

$port = Find-UnityMcpPort
Write-Host "Using Unity MCP port $port"

if (-not $SkipRefresh) {
    [void](Invoke-UnityMcpCommand -Port $port -Payload @{
        type = "refresh_unity"
        params = @{
            scope = $RefreshScope
            compile = $Compile
        }
    })

    $port = Wait-UnityIdle -InitialPort $port
}

$start = Invoke-UnityMcpCommand -Port $port -Payload @{
    type = "run_tests"
    params = @{
        mode = $Mode
        testNames = $TestName
    }
}

$jobId = $start.result.data.job_id
Write-Host "Started Unity test job $jobId"

for ($poll = 1; $poll -le $MaxPolls; $poll += 1) {
    $job = Invoke-UnityMcpCommand -Port $port -Payload @{
        type = "get_test_job"
        params = @{
            job_id = $jobId
            include_details = $false
            include_failed_tests = $true
        }
    } -CommandTimeoutMs $TimeoutMs

    if ($job.status -eq "error" -and $job.error -like "*timed out*") {
        Write-Host "Unity test job query timed out; continuing to poll."
        Start-Sleep -Seconds $PollSeconds
        continue
    }

    $data = $job.result.data
    if ($data.status -eq "running") {
        if (($poll % 5) -eq 0) {
            Write-Host "Tests running: completed=$($data.progress.completed)/$($data.progress.total)"
        }

        Start-Sleep -Seconds $PollSeconds
        continue
    }

    if ($null -eq $data.result) {
        Write-Host "Unity test job ended without a result. Status: $($data.status)"
        $job | ConvertTo-Json -Depth 16
        if ($data.progress.failures_so_far) {
            $data.progress.failures_so_far | ConvertTo-Json -Depth 8
        }

        exit 1
    }

    $summary = $data.result.summary
    Write-Host "Unity tests $($summary.resultState): total=$($summary.total) passed=$($summary.passed) failed=$($summary.failed) skipped=$($summary.skipped) duration=$($summary.durationSeconds)s"
    if ($summary.failed -gt 0) {
        $data.result.failed_tests | ConvertTo-Json -Depth 8
        exit 1
    }

    exit 0
}

throw "Unity test job $jobId did not finish after $MaxPolls polls."
