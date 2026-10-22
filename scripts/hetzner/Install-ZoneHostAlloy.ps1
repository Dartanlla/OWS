<#
.SYNOPSIS
Installs or updates Grafana Alloy on a Windows zone host, from an elevated PowerShell:
    powershell -ExecutionPolicy Bypass -File scripts\hetzner\Install-ZoneHostAlloy.ps1

.DESCRIPTION
Ships the zone server logs and the host's and zone servers' CPU and memory to Grafana Cloud (alloy/zone-host-windows.alloy).
The Grafana Cloud endpoints and token come from the API box's /etc/default/alloy over SSH: they are not printed or
written to a file, only to the Alloy service environment (HKLM\SOFTWARE\GrafanaLabs\Alloy, Environment).
Run it again after a config change.
#>
param(
    [string] $Server = '87.99.150.89',
    [string] $User = 'root',
    [string] $KeyPath = "$env:USERPROFILE\.ssh\id_ed25519_codex_20260509",

    # The launcher's -AbsLog folder: each zone instance writes UEServer-<zoneinstanceid>-<port>.log here.
    [string] $LogDir = 'C:/OWS/Logs',

    # The host label on this machine's logs and metrics.
    [string] $Instance = $env:COMPUTERNAME.ToLower(),

    [string] $AlloyVersion = 'v1.20.1'
)

$ErrorActionPreference = 'Stop'

# Avoid PowerShell 7-only syntax: this runs under powershell.exe (5.1) too.
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this from an elevated PowerShell (Run as administrator).'
}
if (-not (Test-Path $KeyPath)) { throw "SSH key not found: $KeyPath" }
if (-not (Test-Path $LogDir)) { throw "Zone log folder not found: $LogDir" }

$installDir = Join-Path $env:ProgramFiles 'GrafanaLabs\Alloy'
if (-not (Get-Service Alloy -ErrorAction SilentlyContinue)) {
    $temp = Join-Path $env:TEMP "alloy-installer-$AlloyVersion"
    New-Item -ItemType Directory -Force $temp | Out-Null
    $zip = Join-Path $temp 'alloy-installer-windows-amd64.exe.zip'
    Write-Host "Downloading Alloy $AlloyVersion"
    Invoke-WebRequest -UseBasicParsing -OutFile $zip "https://github.com/grafana/alloy/releases/download/$AlloyVersion/alloy-installer-windows-amd64.exe.zip"
    Expand-Archive -Force $zip $temp
    Start-Process -Wait -FilePath (Join-Path $temp 'alloy-installer-windows-amd64.exe') -ArgumentList '/S', '/DISABLEREPORTING=yes'
    if (-not (Get-Service Alloy -ErrorAction SilentlyContinue)) { throw 'The Alloy installer did not create the Alloy service.' }
}

# -F NUL keeps a personal ~/.ssh/config from redirecting the connection.
$settings = & ssh -F NUL -i $KeyPath -o BatchMode=yes -o StrictHostKeyChecking=accept-new "$User@$Server" `
    "grep -E '^GRAFANA_CLOUD_[A-Z_]+=' /etc/default/alloy"
if ($LASTEXITCODE -ne 0) { throw "Could not read the Grafana Cloud settings from $Server (ssh exit $LASTEXITCODE)." }

$environment = @($settings | ForEach-Object { $_.Trim() } | Where-Object { $_ }) +
    "ALLOY_INSTANCE=$Instance", "OWS_ZONE_LOG_DIR=$($LogDir -replace '\\', '/')"
foreach ($name in 'GRAFANA_CLOUD_TOKEN', 'GRAFANA_CLOUD_LOKI_URL', 'GRAFANA_CLOUD_LOKI_USER', 'GRAFANA_CLOUD_PROM_URL', 'GRAFANA_CLOUD_PROM_USER') {
    if (-not ($environment -match "^$name=.")) { throw "$name is missing from /etc/default/alloy on $Server." }
}
New-ItemProperty -Force -Path 'HKLM:\SOFTWARE\GrafanaLabs\Alloy' -Name Environment -PropertyType MultiString -Value $environment | Out-Null

$configDir = Join-Path $PSScriptRoot 'alloy'
Copy-Item -Force (Join-Path $configDir 'zone-host-windows.alloy') (Join-Path $installDir 'config.alloy')
Copy-Item -Force (Join-Path $configDir 'zone-logs.alloy') $installDir

Restart-Service Alloy
Start-Sleep -Seconds 10
$components = (Invoke-RestMethod -UseBasicParsing 'http://127.0.0.1:12345/api/v0/web/components')
$unhealthy = @($components | Where-Object { $_.health.state -ne 'healthy' })
if ($unhealthy.Count -gt 0) {
    $unhealthy | ForEach-Object { Write-Warning "$($_.localID): $($_.health.state) $($_.health.message)" }
    throw "Alloy is running but $($unhealthy.Count) component(s) are not healthy. Details: Event Viewer, Windows Logs, Application, source Alloy."
}
Write-Host "Alloy is running with $($components.Count) healthy components. Host label: $Instance" -ForegroundColor Green
