param([ValidateSet('All', 'DesktopWorkflow', 'OpenRouter', 'CloudRecovery')][string]$Only = 'All')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -Tests
$projectRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $projectRoot 'build\PromptEnhancer.Tests.exe') $Only
if ($LASTEXITCODE -ne 0) { throw 'Checks failed.' }
