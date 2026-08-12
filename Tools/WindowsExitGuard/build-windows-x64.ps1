param(
    [string]$VisualStudioRoot = 'D:\vs2026'
)

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$source = Join-Path $PSScriptRoot 'WindowsExitGuard.cpp'
$intermediateDirectory = Join-Path $PSScriptRoot 'build\windows-x64'
$outputDirectory = Join-Path $projectRoot 'Assets\Plugins\x86_64'
$output = Join-Path $outputDirectory 'LearnHearthstone.WindowsExitGuard.dll'
$object = Join-Path $intermediateDirectory 'WindowsExitGuard.obj'
$importLibrary = Join-Path $intermediateDirectory 'WindowsExitGuard.lib'
$vcVars = Join-Path $VisualStudioRoot 'VC\Auxiliary\Build\vcvars64.bat'

if (-not (Test-Path -LiteralPath $vcVars))
{
    throw "Visual Studio x64 environment script was not found: $vcVars"
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $intermediateDirectory -Force | Out-Null

$command = '"{0}" && cl.exe /nologo /LD /O2 /EHsc /W4 /WX /DUNICODE /D_UNICODE /Fo"{1}" "{2}" /link /OUT:"{3}" /IMPLIB:"{4}"' -f $vcVars, $object, $source, $output, $importLibrary
& $env:ComSpec /d /s /c $command
if ($LASTEXITCODE -ne 0)
{
    throw "Windows exit guard compilation failed with exit code $LASTEXITCODE."
}

Get-Item -LiteralPath $output | Select-Object FullName, Length, LastWriteTime
