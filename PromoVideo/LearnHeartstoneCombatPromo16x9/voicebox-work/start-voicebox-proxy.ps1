$ErrorActionPreference = 'Stop'

$env:HTTP_PROXY = 'http://127.0.0.1:7897'
$env:HTTPS_PROXY = 'http://127.0.0.1:7897'
$env:ALL_PROXY = 'http://127.0.0.1:7897'
$env:NO_PROXY = '127.0.0.1,localhost'

$workDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stdoutLog = Join-Path $workDir 'voicebox-proxy.stdout.log'
$stderrLog = Join-Path $workDir 'voicebox-proxy.stderr.log'

$process = Start-Process `
    -FilePath 'D:\voicebox\voicebox-server.exe' `
    -ArgumentList @(
        '--host', '127.0.0.1',
        '--port', '17493',
        '--data-dir', 'C:\Users\wch\AppData\Roaming\sh.voicebox.app'
    ) `
    -WindowStyle Hidden `
    -RedirectStandardOutput $stdoutLog `
    -RedirectStandardError $stderrLog `
    -PassThru

$process.Id
