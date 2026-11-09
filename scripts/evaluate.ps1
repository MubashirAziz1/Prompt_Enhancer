param([switch]$Live)
$ErrorActionPreference = 'Stop'
if (!$Live) { throw 'Live evaluation uses your saved OpenRouter account and incurs usage charges. Run with -Live to opt in.' }
& (Join-Path $PSScriptRoot 'build.ps1') -Tests
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'build\winui-tests\x64\Release\net10.0-windows10.0.26100.0\win-x64\PromptEnhancer.WinUI.Tests.exe'
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = $executable
$info.Arguments = '--live-evaluation'
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$process = [Diagnostics.Process]::Start($info)
try {
    $errors = $process.StandardError.ReadToEndAsync()
    while (!$process.StandardOutput.EndOfStream) { Write-Output $process.StandardOutput.ReadLine() }
    $process.WaitForExit()
    $feedback = $errors.GetAwaiter().GetResult()
    if ($feedback) { Write-Output $feedback }
    if ($process.ExitCode -ne 0) { throw 'Live evaluation did not complete. See safe application feedback above.' }
} finally {
    if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $process.Dispose()
}
