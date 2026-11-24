param([ValidateSet('All', 'CurrentPromptSession', 'FreshProcess', 'Settings', 'Workflow', 'Recovery', 'Appearance', 'Session', 'Hide', 'OpenRouter')][string]$Only = 'All', [switch]$Published)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ($Only -in @('All', 'CurrentPromptSession')) {
    & (Join-Path $env:ProgramW6432 'dotnet/dotnet.exe') run --project (Join-Path $projectRoot 'tests/CurrentPromptSession/PromptEnhancer.Session.Tests.csproj') --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Current prompt session checks failed.' }
    if ($Only -eq 'CurrentPromptSession') { return }
}
& (Join-Path $PSScriptRoot 'build.ps1') -Tests -Publish:$Published
$directory = Join-Path $projectRoot 'build\winui-tests\x64\Release\net10.0-windows10.0.26100.0\win-x64'
if ($Published) { $directory = Join-Path $projectRoot 'build\publish-tests\win-x64' }
$report = Join-Path $directory 'checks.txt'
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
$process = Start-Process -FilePath (Join-Path $directory 'PromptEnhancer.WinUI.Tests.exe') -ArgumentList @('--only', $Only) -PassThru
if (!$process.WaitForExit(120000)) {
    $process.Kill()
    throw 'WinUI checks timed out.'
}
if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report -Encoding UTF8 }
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $report)) { throw "WinUI checks failed (exit $($process.ExitCode))." }
