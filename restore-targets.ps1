<#
.SYNOPSIS
    Restores NuGet assets for CopilotTrayStats for win-x64 and win-arm64.

.DESCRIPTION
    Fixes "Assets file doesn't have a target for net10.0-windows10.0.17763.0"
    by running dotnet restore with each RID.

.EXAMPLE
    .\restore-and-publish.ps1
#>
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Project = Join-Path $PSScriptRoot 'CopilotTrayStats\CopilotTrayStats.csproj'
$Rids    = @('win-x64', 'win-arm64')

$ProjectDir = Split-Path $Project

foreach ($rid in $Rids) {
    # VS publish uses a separate restore output path: obj\wappublish\<rid>\
    $wappublishDir = Join-Path $ProjectDir "obj\wappublish\$rid"

    Write-Host ""
    Write-Host "==> Restoring for $rid (standard) ..." -ForegroundColor Cyan
    dotnet restore $Project --runtime $rid
    if ($LASTEXITCODE -ne 0) { Write-Error "Restore failed for $rid (exit $LASTEXITCODE)." }

    Write-Host "==> Restoring for $rid (wappublish) -> $wappublishDir ..." -ForegroundColor Cyan
    dotnet restore $Project --runtime $rid /p:RestoreOutputPath="$wappublishDir"
    if ($LASTEXITCODE -ne 0) { Write-Error "Restore (wappublish) failed for $rid (exit $LASTEXITCODE)." }

    Write-Host "    Done." -ForegroundColor Green
}

Write-Host ""
Write-Host "All targets restored successfully." -ForegroundColor Green
