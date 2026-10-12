#Requires -RunAsAdministrator
# Publishes ApexClock.Service and registers it as an auto-start Windows service (re-run to update).
param(
    [string]$InstallDir = (Join-Path $env:ProgramFiles 'ApexClock\Service')
)

$ErrorActionPreference = 'Stop'
$serviceName = 'ApexClock'
$project = Join-Path $PSScriptRoot '..\Apex Clock\ApexClock.Service\ApexClock.Service.csproj'

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing -and $existing.Status -ne 'Stopped') {
    Stop-Service -Name $serviceName
    $existing.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
}

dotnet publish $project -c Release -r win-x64 --self-contained false -o $InstallDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }

$exe = Join-Path $InstallDir 'ApexClock.Service.exe'
if (-not $existing) {
    New-Service -Name $serviceName `
        -BinaryPathName "`"$exe`"" `
        -DisplayName 'Apex Clock' `
        -Description 'Tracks the Apex Legends ranked map rotation and notifies the Apex Clock app on every change.' `
        -StartupType Automatic | Out-Null
}

Start-Service -Name $serviceName
Get-Service -Name $serviceName
