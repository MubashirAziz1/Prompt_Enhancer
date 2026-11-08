param([switch]$Tests)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path -LiteralPath $locator)) { throw 'Install stable Visual Studio Build Tools 2026 and .NET 10. See docs/winui-development.md.' }
$msbuild = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\amd64\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'MSBuild was not found. See docs/winui-development.md.' }
$project = if ($Tests) { 'winui-tests\PromptEnhancer.WinUI.Tests.csproj' } else { 'winui\PromptEnhancer.WinUI.csproj' }
$dotnetDirectory = Join-Path $env:ProgramW6432 'dotnet'
if (!(Test-Path -LiteralPath (Join-Path $dotnetDirectory 'dotnet.exe'))) { throw 'Install the x64 .NET 10 SDK. See docs/winui-development.md.' }
$previousPath = $env:PATH
$previousRoot = $env:DOTNET_ROOT
$previousResolver = $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR
$env:PATH = $dotnetDirectory + ';' + $env:PATH
$env:DOTNET_ROOT = $dotnetDirectory
$env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $dotnetDirectory
Push-Location $projectRoot
try {
    # Managed-only XAML needs no VC metadata. Avoid the SDK's unconditional
    # C++ directory probe when Visual Studio has only managed build tools.
    & $msbuild $project /restore /m /v:minimal /nologo /p:Configuration=Release /p:Platform=x64 /p:VsInstallRoot= /warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'WinUI build failed.' }
} finally {
    Pop-Location
    $env:PATH = $previousPath
    $env:DOTNET_ROOT = $previousRoot
    $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $previousResolver
}
if ($Tests) {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
    $probe = Join-Path $projectRoot 'build\CredentialProbe.exe'
    & (Join-Path $framework 'csc.exe') /nologo /warn:4 /warnaserror+ /target:exe "/out:$probe" `
        "/reference:$framework\System.Security.dll" `
        (Join-Path $projectRoot 'src\Enhancement.cs') `
        (Join-Path $projectRoot 'src\DpapiCredentialStore.cs') `
        (Join-Path $projectRoot 'winui-tests\legacy\CredentialProbe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Legacy credential fixture compilation failed.' }
}
Write-Host 'WinUI build passed (warnings treated as errors).'
