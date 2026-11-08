$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build-winui.ps1')
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'build\winui\x64\Release\net10.0-windows10.0.26100.0\win-x64\PromptEnhancer.WinUI.exe'
Start-Process -FilePath $executable
