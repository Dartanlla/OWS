<#
.SYNOPSIS
    Open Samsara Management, the staff console running on the Hetzner dev server.

.DESCRIPTION
    The console (owsmanagement) is bound to 127.0.0.1 on the server and has no TLS, so it is
    only ever reached over an SSH tunnel. Staff sign in through Steam. This script:

      1. optionally pulls origin/main on the server and rebuilds the console image,
      2. starts the console (compose profile "admin", never touching the database),
      3. opens an SSH tunnel from this PC to it,
      4. opens the browser,
      5. on Ctrl+C, closes the tunnel and — unless -KeepRunning — stops the container.

    The console is stopped by default when you are done because the 4 GB dev host has
    little headroom left once the game services have taken their reservations.

.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-AdminConsole.ps1

.EXAMPLE
    # First run after pushing console changes:
    .\Start-AdminConsole.ps1 -Update

.EXAMPLE
    # Leave it running on the server after the tunnel closes:
    .\Start-AdminConsole.ps1 -KeepRunning
#>
[CmdletBinding()]
param(
    [string] $Server = '87.99.150.89',
    [string] $User = 'root',
    [string] $KeyPath = "$env:USERPROFILE\.ssh\id_ed25519_codex_20260509",
    [string] $RemoteSrcDir = '/opt/owsfork/src',
    [int]    $Port = 44410,

    # git pull --ff-only origin main on the server, then force a rebuild of the console
    # image. Needed the first time, and after any console change you have pushed.
    [switch] $Update,

    # Leave the console container running on the server after the tunnel closes.
    [switch] $KeepRunning,

    # Do not launch a browser.
    [switch] $NoBrowser
)

$ErrorActionPreference = 'Stop'

# Avoid PowerShell 7-only syntax throughout: the repo's other helpers are launched with
# powershell.exe (5.1), so this must run there too.
$sshCmd = Get-Command ssh -ErrorAction SilentlyContinue
if (-not $sshCmd) { throw "ssh not found on PATH. Install the Windows OpenSSH client." }
$sshExe = $sshCmd.Source
if (-not (Test-Path $KeyPath)) { throw "SSH key not found: $KeyPath" }

# -F NUL keeps a personal ~/.ssh/config from redirecting the connection.
$sshBase = @('-F', 'NUL', '-i', $KeyPath, '-o', 'StrictHostKeyChecking=accept-new')
$target = "$User@$Server"

function Invoke-Remote {
    param([Parameter(Mandatory)][string] $Command, [switch] $AllowFailure)

    & $sshExe @sshBase $target $Command
    if ($LASTEXITCODE -ne 0 -and -not $AllowFailure) {
        throw "Remote command failed (exit $LASTEXITCODE): $Command"
    }
    return $LASTEXITCODE
}

$remoteRepoDir = $RemoteSrcDir -replace '/src/?$', ''
$scriptPath = "$remoteRepoDir/scripts/hetzner/admin-console.sh"

function Test-RemoteScript {
    # `test -f` over ssh: exit 0 when the helper is present on the server.
    & $sshExe @sshBase $target "test -f $scriptPath" 2>$null | Out-Null
    return ($LASTEXITCODE -eq 0)
}

$missingScriptHelp = @"
The server does not have $scriptPath.

That means the admin console changes have not reached it yet. From your PC:

    git -C "<repo>" add -A
    git -C "<repo>" commit -m "Add the admin console"
    git -C "<repo>" push origin main

then run Update-AdminConsole.cmd (not Start-AdminConsole.cmd), which pulls
origin/main on the server and builds the console image before opening it.
"@

if ($Update) {
    Write-Host "==> Pulling origin/main on $Server" -ForegroundColor Cyan
    Invoke-Remote "cd $RemoteSrcDir && git fetch origin && git pull --ff-only origin main"

    # A successful pull that still leaves the helper missing means the commit was never
    # pushed - a much more useful thing to say than "bash: no such file".
    if (-not (Test-RemoteScript)) { throw $missingScriptHelp }

    Write-Host "==> Rebuilding the console image (database untouched)" -ForegroundColor Cyan
    Invoke-Remote "chmod +x $scriptPath; OWS_SRC_DIR=$RemoteSrcDir OWS_MANAGEMENT_HOST_PORT=$Port bash $scriptPath rebuild"
}
else {
    if (-not (Test-RemoteScript)) { throw $missingScriptHelp }

    Write-Host "==> Starting the console on $Server" -ForegroundColor Cyan
    Invoke-Remote "chmod +x $scriptPath; OWS_SRC_DIR=$RemoteSrcDir OWS_MANAGEMENT_HOST_PORT=$Port bash $scriptPath up"
}

# A local listener on the same port would make the tunnel silently connect to the wrong
# thing, which is confusing when the wrong thing is a local dev build of the same app.
$inUse = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($inUse) {
    $owner = (Get-Process -Id $inUse[0].OwningProcess -ErrorAction SilentlyContinue).ProcessName
    throw "Local port $Port is already in use by '$owner'. Stop it, or re-run with -Port <other>."
}

Write-Host "==> Opening tunnel 127.0.0.1:$Port -> ${Server}:127.0.0.1:$Port" -ForegroundColor Cyan
$tunnelArgs = $sshBase + @(
    '-N',
    '-o', 'ExitOnForwardFailure=yes',
    '-o', 'ServerAliveInterval=30',
    '-L', "${Port}:127.0.0.1:$Port",
    $target
)
$tunnel = Start-Process -FilePath $sshExe -ArgumentList $tunnelArgs -PassThru -WindowStyle Hidden

try {
    $ready = $false
    foreach ($attempt in 1..20) {
        Start-Sleep -Milliseconds 500
        if ($tunnel.HasExited) { throw "SSH tunnel exited immediately (code $($tunnel.ExitCode))." }
        try {
            Invoke-WebRequest -Uri "http://127.0.0.1:$Port/api/System/Status" -TimeoutSec 3 -UseBasicParsing | Out-Null
            $ready = $true
            break
        }
        catch { }
    }
    if (-not $ready) { throw "Tunnel is open but the console did not answer on 127.0.0.1:$Port." }

    $url = "http://localhost:$Port"
    Write-Host ""
    Write-Host "Samsara Management ready: $url" -ForegroundColor Green
    Write-Host "  Sign in through Steam with an account whose role is Moderator or higher." -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Press Ctrl+C to close the tunnel." -ForegroundColor Yellow
    if (-not $KeepRunning) {
        Write-Host "  If cleanup is skipped, stop it with: ssh $target 'bash $scriptPath down'" -ForegroundColor DarkGray
    }

    if (-not $NoBrowser) { Start-Process $url }

    while (-not $tunnel.HasExited) { Start-Sleep -Seconds 1 }
}
finally {
    if ($tunnel -and -not $tunnel.HasExited) {
        Write-Host "`n==> Closing tunnel" -ForegroundColor Cyan
        Stop-Process -Id $tunnel.Id -Force -ErrorAction SilentlyContinue
    }

    if ($KeepRunning) {
        Write-Host "Console left running on $Server (loopback-only)." -ForegroundColor DarkGray
    }
    else {
        Write-Host "==> Stopping the console on $Server" -ForegroundColor Cyan
        Invoke-Remote "OWS_SRC_DIR=$RemoteSrcDir bash $scriptPath down" -AllowFailure | Out-Null
    }
}
