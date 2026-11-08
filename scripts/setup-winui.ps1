$ErrorActionPreference = 'Stop'
# Stable Microsoft tooling; run from an elevated PowerShell if installation requests it.
winget install --id Microsoft.VisualStudio.BuildTools --exact --source winget --accept-source-agreements --accept-package-agreements --silent --override '--wait --quiet --norestart --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools --add Microsoft.VisualStudio.Component.Windows11SDK.26100 --includeRecommended'
if ($LASTEXITCODE -notin @(0, -1978335189, 3010)) { throw 'Visual Studio Build Tools setup failed.' }
winget install --id Microsoft.DotNet.SDK.10 --version 10.0.403 --exact --source winget --accept-source-agreements --accept-package-agreements --silent
if ($LASTEXITCODE -notin @(0, -1978335189, 3010)) { throw '.NET SDK setup failed.' }
Write-Host 'Restart PowerShell after setup. Reboot first if the installer requests it.'
