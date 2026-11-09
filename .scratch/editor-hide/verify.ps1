$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $locator -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\amd64\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'MSBuild was not found.' }
$dotnetDirectory = Join-Path $env:ProgramW6432 'dotnet'
$previousPath = $env:PATH
$previousRoot = $env:DOTNET_ROOT
$previousResolver = $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR
$env:PATH = $dotnetDirectory + ';' + $env:PATH
$env:DOTNET_ROOT = $dotnetDirectory
$env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $dotnetDirectory
try {
    $output = (Join-Path $projectRoot 'build\hide-verification').Replace('\', '/') + '/'
    & $msbuild (Join-Path $projectRoot 'tests\PromptEnhancer.WinUI.Tests.csproj') /restore /m /v:minimal /nologo /p:Configuration=Release /p:Platform=x64 /p:VsInstallRoot= /warnaserror "/p:BaseOutputPath=$output"
    if ($LASTEXITCODE -ne 0) { throw 'Isolated Hide verification build failed.' }
} finally {
    $env:PATH = $previousPath
    $env:DOTNET_ROOT = $previousRoot
    $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $previousResolver
}
$directory = Join-Path $projectRoot 'build\hide-verification\x64\Release\net10.0-windows10.0.26100.0\win-x64'
$report = Join-Path $directory 'checks.txt'
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
$process = Start-Process -FilePath (Join-Path $directory 'PromptEnhancer.WinUI.Tests.exe') -ArgumentList @('--only', 'Hide') -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Hide checks timed out.' }
if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report -Encoding UTF8 }
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $report)) { throw "Hide checks failed (exit $($process.ExitCode))." }
