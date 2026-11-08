$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
Start-Process -FilePath (Join-Path $projectRoot 'build\PromptEnhancer.exe')
