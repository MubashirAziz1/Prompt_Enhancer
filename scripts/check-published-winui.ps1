$ErrorActionPreference = 'Stop'
# The single-instance guard would hand the script's launch to a running application.
if (Get-Process -Name 'PromptEnhancer.WinUI' -ErrorAction SilentlyContinue) { throw 'Quit the running Prompt Enhancer before the published acceptance check.' }
& (Join-Path $PSScriptRoot 'publish-winui.ps1')
& (Join-Path $PSScriptRoot 'check.ps1') -Published
$projectRoot = Split-Path -Parent $PSScriptRoot
$directory = Join-Path $projectRoot 'build\publish\win-x64'
$testDirectory = Join-Path $projectRoot 'build\publish-tests\win-x64'
foreach ($asset in @('PromptEnhancer.WinUI.dll', 'PromptEnhancer.WinUI.pri', 'App.xbf', 'EditorWindow.xbf', 'LauncherWindow.xbf', 'SettingsDialog.xbf')) {
    if ((Get-FileHash -LiteralPath (Join-Path $directory $asset)).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $testDirectory $asset)).Hash) {
        throw "Published workflow checks must exercise identical application code and resources: $asset."
    }
}
$previousDotnetRoot = $env:DOTNET_ROOT
$previousLookup = $env:DOTNET_MULTILEVEL_LOOKUP
try {
    # Startup must use the app-local runtimes; never open the production editor/key.
    $env:DOTNET_ROOT = Join-Path $projectRoot 'build\no-shared-dotnet'
    $env:DOTNET_MULTILEVEL_LOOKUP = '0'
    $process = Start-Process -FilePath (Join-Path $directory 'PromptEnhancer.WinUI.exe') -PassThru
    try {
        if (!$process.WaitForInputIdle(15000)) { throw 'Published startup did not become interactive.' }
        $process.Refresh()
        foreach ($moduleName in @('coreclr.dll', 'Microsoft.ui.xaml.dll', 'Microsoft.WindowsAppRuntime.dll')) {
            $module = @($process.Modules | Where-Object ModuleName -eq $moduleName)
            if ($module.Count -ne 1 -or !(($module[0].FileName).StartsWith($directory + '\', [StringComparison]::OrdinalIgnoreCase))) {
                throw "Published startup did not load app-local $moduleName."
            }
            Write-Host "PASS Published startup loads app-local $moduleName"
        }
        # A second launch must not create a second button or notification icon.
        $second = Start-Process -FilePath (Join-Path $directory 'PromptEnhancer.WinUI.exe') -PassThru
        try {
            if (!$second.WaitForExit(15000)) { throw 'A second launch did not hand off and exit.' }
        } finally {
            if (!$second.HasExited) { $second.Kill() }
            $second.Dispose()
        }
        $process.Refresh()
        if ($process.HasExited) { throw 'A second launch must leave the first instance running.' }
        Write-Host 'PASS Second launch exits; the first instance stays the only floating experience'
        & (Join-Path $projectRoot 'build\DesktopProbe.exe') startup $process.Id
        if ($LASTEXITCODE -ne 0) { throw 'Published floating startup/close check failed.' }
        if (!$process.WaitForExit(10000) -or $process.ExitCode -ne 0) { throw 'Published application did not quit completely.' }
    } finally {
        if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
} finally {
    $env:DOTNET_ROOT = $previousDotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = $previousLookup
}
Write-Host 'PASS Published production startup and complete Quit; identical-assembly workflow acceptance passed.'
