$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -Publish
$projectRoot = Split-Path -Parent $PSScriptRoot
$directory = Join-Path $projectRoot 'build\publish\win-x64'
foreach ($file in @('PromptEnhancer.WinUI.exe', 'PromptEnhancer.WinUI.pri', 'App.xbf', 'EditorWindow.xbf', 'LauncherWindow.xbf', 'SettingsDialog.xbf', 'coreclr.dll', 'hostfxr.dll', 'Microsoft.ui.xaml.dll', 'Microsoft.WindowsAppRuntime.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $directory $file))) { throw "The published folder is missing $file." }
}
Write-Host "Self-contained Windows 11 x64 folder: $directory"
