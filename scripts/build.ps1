param([switch]$Tests, [switch]$Publish)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path -LiteralPath $locator)) { throw 'Install stable Visual Studio Build Tools 2026 and .NET 10. See docs/winui-development.md.' }
$msbuild = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\amd64\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'MSBuild was not found. See docs/winui-development.md.' }
$project = if ($Tests) { 'tests\PromptEnhancer.WinUI.Tests.csproj' } else { 'src\PromptEnhancer.WinUI.csproj' }
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
    $buildArguments = @($project, '/restore', '/m', '/v:minimal', '/nologo', '/p:Configuration=Release', '/p:Platform=x64', '/p:VsInstallRoot=', '/warnaserror')
    if ($Publish) {
        $folder = if ($Tests) { 'publish-tests' } else { 'publish' }
        $publishDirectory = (Join-Path $projectRoot "build\$folder\win-x64").Replace('\', '/') + '/'
        $buildArguments += @('/t:Publish', "/p:PublishDir=$publishDirectory", '/p:SelfContained=true', '/p:WindowsAppSDKSelfContained=true', '/p:PublishSingleFile=false', '/p:PublishTrimmed=false')
    }
    & $msbuild @buildArguments
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
        (Join-Path $projectRoot 'tests\fixtures\CredentialProbe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Legacy credential fixture compilation failed.' }
    $automation = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
    & (Join-Path $framework 'csc.exe') /nologo /warn:4 /warnaserror+ /target:exe "/out:$projectRoot\build\DesktopProbe.exe" "/win32manifest:$projectRoot\src\app.manifest" `
        "/reference:$framework\System.Windows.Forms.dll" "/reference:$framework\System.Drawing.dll" `
        "/reference:$automation\UIAutomationClient.dll" "/reference:$automation\UIAutomationTypes.dll" "/reference:$automation\WindowsBase.dll" `
        (Join-Path $projectRoot 'tests\fixtures\DesktopProbe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Native desktop fixture compilation failed.' }
    if ($Publish) {
        $fixtures = Join-Path $publishDirectory 'fixtures'
        New-Item -ItemType Directory -Path $fixtures -Force | Out-Null
        Copy-Item -LiteralPath $probe, (Join-Path $projectRoot 'build\DesktopProbe.exe') -Destination $fixtures
    }
}
Write-Host 'WinUI build passed (warnings treated as errors).'
