param([switch]$Live)
$ErrorActionPreference = 'Stop'
if (!$Live) { throw 'Live evaluation uses your saved OpenRouter account and incurs usage charges. Run with -Live to opt in.' }
& (Join-Path $PSScriptRoot 'build.ps1')
$projectRoot = Split-Path -Parent $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Net.Http.dll', 'System.Web.Extensions.dll') |
    ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$output = Join-Path $projectRoot 'build\PromptEnhancer.Evaluation.exe'
& (Join-Path $framework 'csc.exe') '/nologo' '/warn:4' '/warnaserror+' '/target:exe' @references `
    ('/reference:' + (Join-Path $projectRoot 'build\PromptEnhancer.exe')) `
    ('/out:' + $output) (Join-Path $projectRoot 'evaluation\LiveEvaluation.cs')
if ($LASTEXITCODE -ne 0) { throw 'Evaluation compilation failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\PromptEnhancer.exe.config') -Destination ($output + '.config')
& $output
if ($LASTEXITCODE -ne 0) { throw 'Live evaluation did not complete. See safe application feedback above.' }
