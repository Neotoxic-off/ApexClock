#Requires -RunAsAdministrator
# Stops and removes the ApexClock Windows service. The schedule in %ProgramData%\ApexClock is kept.
$ErrorActionPreference = 'Stop'
$serviceName = 'ApexClock'

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $existing) {
    Write-Host "Service $serviceName is not installed."
    return
}

if ($existing.Status -ne 'Stopped') {
    Stop-Service -Name $serviceName
    $existing.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
}

sc.exe delete $serviceName | Out-Null
Write-Host "Service $serviceName removed."
