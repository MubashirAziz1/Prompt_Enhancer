# Normal launch: the WinUI 3 application (floating button, tray, prompt editor).
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'build\winui\x64\Release\net10.0-windows10.0.26100.0\win-x64\PromptEnhancer.WinUI.exe'
if (!(Test-Path -LiteralPath $executable)) { throw 'The WinUI executable was not produced.' }
# A running instance simply brings back its floating button.
Start-Process -FilePath $executable
