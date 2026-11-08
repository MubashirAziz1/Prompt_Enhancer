param([switch]$Tests)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) {
    throw 'The Windows .NET Framework 4.8 compiler is required.'
}
$output = Join-Path $projectRoot 'build'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Net.Http.dll',
    'System.Runtime.Serialization.dll', 'System.Security.dll', 'System.Web.Extensions.dll') |
    ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -Recurse |
    ForEach-Object { $_.FullName })
$arguments = @('/nologo', '/warn:4', '/warnaserror+', '/optimize+', '/platform:anycpu')
& $compiler @arguments @references '/target:winexe' "/out:$output\PromptEnhancer.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\PromptEnhancer.exe.config') -Destination $output
if ($Tests) {
    $testSources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests') -Filter '*.cs' -Recurse |
        ForEach-Object { $_.FullName })
    & $compiler @arguments @references '/target:exe' '/main:PromptEnhancer.Tests.TestRunner' "/out:$output\PromptEnhancer.Tests.exe" @sources @testSources
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'src\PromptEnhancer.exe.config') -Destination (Join-Path $output 'PromptEnhancer.Tests.exe.config')
}
Write-Host 'Build passed (compiler warnings treated as errors).'
