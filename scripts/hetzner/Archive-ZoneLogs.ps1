<#
.SYNOPSIS
Uploads this zone host's finished zone server logs to the API box for the 90-day log archive, one zip per UTC day:
    powershell -ExecutionPolicy Bypass -File scripts\hetzner\Archive-ZoneLogs.ps1             # upload now
    powershell -ExecutionPolicy Bypass -File scripts\hetzner\Archive-ZoneLogs.ps1 -Register   # daily task

.DESCRIPTION
A log belongs to the UTC day of its last write, so a day is archived once it is over and each file lands in one zip.
Days already on the box are skipped, so a missed run catches up. The box keeps 90 days and mirrors them to the
Storage Box (ows-log-archive; docs/hosting/hetzner-dev.md, Log archive). Local logs are not deleted.
#>
param(
    [string] $Server = '87.99.150.89',
    [string] $User = 'root',
    [string] $KeyPath = "$env:USERPROFILE\.ssh\id_ed25519_codex_20260509",

    # The launcher's -AbsLog folder; every *.log in it is archived (zone servers, their -backup- copies, the launcher).
    [string] $LogDir = 'C:\OWS\Logs',

    # The host folder on the box, matching the Alloy host label.
    [string] $Instance = $env:COMPUTERNAME.ToLower(),

    [int] $KeepDays = 90,

    # Register a daily task (10:00, or at the next logon when the PC was off) that runs this script, then exit.
    [switch] $Register
)

$ErrorActionPreference = 'Stop'

# Avoid PowerShell 7-only syntax: the task runs it under powershell.exe (5.1).
if ($Register) {
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' `
        -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$PSCommandPath`" -LogDir `"$LogDir`" -Instance $Instance"
    $trigger = New-ScheduledTaskTrigger -Daily -At '10:00'
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 1)
    Register-ScheduledTask -Force -TaskName 'OWS zone log archive' -Action $action -Trigger $trigger -Settings $settings `
        -Description 'Uploads finished zone server logs to the OWS API box (OWSFork scripts\hetzner\Archive-ZoneLogs.ps1).' | Out-Null
    Write-Host "Registered the daily task 'OWS zone log archive'. Its last result is in Task Scheduler."
    return
}

if (-not (Test-Path $KeyPath)) { throw "SSH key not found: $KeyPath" }
if (-not (Test-Path $LogDir)) { throw "Log folder not found: $LogDir" }

# -F NUL keeps a personal ~/.ssh/config from redirecting the connection.
$sshArgs = @('-F', 'NUL', '-i', $KeyPath, '-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=accept-new')
$target = "$User@$Server"
$remoteDir = "/var/backups/ows-logs/$Instance"

$onBox = @(& ssh @sshArgs $target "mkdir -p $remoteDir && ls $remoteDir")
if ($LASTEXITCODE -ne 0) { throw "Could not reach $remoteDir on $Server (ssh exit $LASTEXITCODE)." }

$today = [DateTime]::UtcNow.Date
$days = Get-ChildItem -LiteralPath $LogDir -Filter '*.log' -File |
    Where-Object { $_.LastWriteTimeUtc.Date -lt $today -and $_.LastWriteTimeUtc.Date -ge $today.AddDays(-$KeepDays) } |
    Group-Object { $_.LastWriteTimeUtc.ToString('yyyyMMdd') } |
    Sort-Object Name

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$uploaded = 0
foreach ($day in $days) {
    $name = "zone-logs-$Instance-$($day.Name).zip"
    if ($onBox -contains $name) { continue }

    $zipPath = Join-Path $env:TEMP $name
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
    try {
        foreach ($file in $day.Group) {
            $entry = $zip.CreateEntry($file.Name, 'Optimal')
            $entry.LastWriteTime = $file.LastWriteTime
            # ReadWrite sharing: a server may still hold its log open.
            $in = [System.IO.File]::Open($file.FullName, 'Open', 'Read', 'ReadWrite')
            $out = $entry.Open()
            try { $in.CopyTo($out) } finally { $out.Dispose(); $in.Dispose() }
        }
    } finally { $zip.Dispose() }

    & scp @sshArgs -q $zipPath "${target}:$remoteDir/$name.partial"
    if ($LASTEXITCODE -ne 0) { throw "Upload of $name failed (scp exit $LASTEXITCODE)." }
    & ssh @sshArgs $target "mv $remoteDir/$name.partial $remoteDir/$name"
    if ($LASTEXITCODE -ne 0) { throw "Could not finish $name on $Server (ssh exit $LASTEXITCODE)." }

    Write-Host ("archived {0}: {1} logs, {2:N1} MB" -f $name, $day.Count, ((Get-Item $zipPath).Length / 1MB))
    Remove-Item $zipPath
    $uploaded++
}
Write-Host "$uploaded day(s) uploaded; $($onBox.Count) already on the box."
