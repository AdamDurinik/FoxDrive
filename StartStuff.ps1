# ============================
# Supervisor.ps1  (Windows PowerShell 5.1+ / PowerShell 7)
# Starts and monitors your Website script, Minecraft server script, and Playit service.
# - Auto-start at boot (use Install-Orchestrator.ps1 below)
# - Restarts crashed processes, logs exit codes + stderr tail
# - Ensures Playit service is running
# - Optional OS reboot on repeated crashes (disabled by default)
# ============================

param(
    [switch]$Once
)

$ErrorActionPreference = 'Stop'

# ---------- CONFIG ----------
$Config = @{
    LogRoot = 'C:\Ops\SupervisorLogs'
    CheckIntervalSeconds = 10
    # Set the *actual* Playit service name (run: Get-Service *play* to discover)
    PlayitServiceName = 'playit'
    RestartOnRapidCrash = @{ Enabled = $true; ThresholdCrashes = 3; TimeWindowMinutes = 10; RebootEnabled = $false }

    Processes = @(
        # 1) Website starter script (PowerShell, BAT, CMD, or EXE)
        @{ Name = 'Website'; FilePath = 'C:\Apps\Website\start-website.ps1'; WorkingDir = 'C:\Apps\Website'; Args = '' },

        # 2) Minecraft server script (usually a BAT/CMD or PS1 that launches Java)
        @{ Name = 'Minecraft'; FilePath = 'C:\Games\Minecraft\start-minecraft.bat'; WorkingDir = 'C:\Games\Minecraft'; Args = '' }
    )
}
# ---------------------------

# Helpers
function Ensure-Directory([string]$path) { if (-not (Test-Path $path)) { New-Item -Path $path -ItemType Directory -Force | Out-Null } }
function Timestamp() { Get-Date -Format 'yyyy-MM-dd HH:mm:ss' }
function Write-Log([string]$msg) {
    Ensure-Directory $Config.LogRoot
    $log = Join-Path $Config.LogRoot ("supervisor-" + (Get-Date -Format 'yyyyMMdd') + '.log')
    $line = "[$(Timestamp)] $msg"
    $line | Tee-Object -FilePath $log -Append | Out-Null
}
function Get-LastLines([string]$Path, [int]$Lines = 50) {
    if (-not (Test-Path $Path)) { return @() }
    # Tail without loading whole file if large
    try { Get-Content -Path $Path -Tail $Lines -ErrorAction Stop } catch { @() }
}

function Resolve-Launch($p) {
    $fp = [string]$p.FilePath
    $args = [string]$p.Args
    $ext = ([System.IO.Path]::GetExtension($fp) ?? '').ToLowerInvariant()
    switch ($ext) {
        '.ps1' { return @{ Program = 'powershell.exe'; Args = "-NoProfile -ExecutionPolicy Bypass -File \"$fp\" $args" } }
        '.bat' { return @{ Program = 'cmd.exe';        Args = "/c \"$fp\" $args" } }
        '.cmd' { return @{ Program = 'cmd.exe';        Args = "/c \"$fp\" $args" } }
        default { return @{ Program = $fp;              Args = $args } }
    }
}

function Start-ManagedProcess($p, [ref]$state) {
    Ensure-Directory $Config.LogRoot
    $name = $p.Name
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $out = Join-Path $Config.LogRoot ("$name-$stamp.out.log")
    $err = Join-Path $Config.LogRoot ("$name-$stamp.err.log")

    $launch = Resolve-Launch $p
    Write-Log "Starting [$name]: $($launch.Program) $($launch.Args) in $($p.WorkingDir) -> out: $out, err: $err"

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $launch.Program
    $psi.Arguments = $launch.Args
    $psi.WorkingDirectory = $p.WorkingDir
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    # Async stream-to-file redirection
    $stdoutWriter = [System.IO.StreamWriter]::new($out)
    $stderrWriter = [System.IO.StreamWriter]::new($err)
    $proc.add_OutputDataReceived({ param($sender,$e) if ($e.Data) { $stdoutWriter.WriteLine($e.Data) } })
    $proc.add_ErrorDataReceived( { param($sender,$e) if ($e.Data) { $stderrWriter.WriteLine($e.Data) } })

    if (-not $proc.Start()) { throw "Failed to start $name" }
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()

    # Save state
    $state.Value = [ordered]@{
        Process = $proc
        OutLog = $out
        ErrLog = $err
        StartTime = Get-Date
        CrashTimes = @()
        WriterOut = $stdoutWriter
        WriterErr = $stderrWriter
    }
}

function Close-Writers($st) {
    try { $st.WriterOut.Flush(); $st.WriterOut.Dispose() } catch {}
    try { $st.WriterErr.Flush(); $st.WriterErr.Dispose() } catch {}
}

function Note-CrashAndMaybeReboot([string]$name, [int]$exit, $st) {
    $st.CrashTimes += (Get-Date)
    $window = (Get-Date).AddMinutes(-[int]$Config.RestartOnRapidCrash.TimeWindowMinutes)
    $recent = @($st.CrashTimes | Where-Object { $_ -ge $window })
    $st.CrashTimes = $recent

    Write-Log "[$name] exited with code $exit. Recent crashes in window: $($recent.Count)"
    $tail = (Get-LastLines -Path $st.ErrLog -Lines 80) -join "`n"
    if ($tail) { Write-Log "[$name] stderr tail:\n$tail" }

    if ($Config.RestartOnRapidCrash.Enabled -and $recent.Count -ge [int]$Config.RestartOnRapidCrash.ThresholdCrashes) {
        if ($Config.RestartOnRapidCrash.RebootEnabled) {
            Write-Log "Crash threshold reached. Rebooting OS in 10 seconds..."
            Start-Sleep -Seconds 10
            Restart-Computer -Force
        } else {
            Write-Log "Crash threshold reached but OS reboot disabled. Continuing restarts."
        }
    }
}

function Ensure-PlayitService() {
    $name = $Config.PlayitServiceName
    if (-not $name) { return }
    try {
        $svc = Get-Service -Name $name -ErrorAction Stop
        if ($svc.Status -ne 'Running') {
            Write-Log "Playit service '$name' is $($svc.Status). Attempting start..."
            Start-Service -Name $name -ErrorAction Stop
            Write-Log "Playit service '$name' started."
        }
    } catch {
        Write-Log "Playit service '$name' not found or failed to start: $($_.Exception.Message)"
    }
}

# State table per process
$State = @{}
Ensure-Directory $Config.LogRoot
Write-Log "==== Supervisor boot $(Timestamp) ===="

# First start of everything
foreach ($p in $Config.Processes) {
    $st = $null
    Start-ManagedProcess $p ([ref]$st)
    $State[$p.Name] = $st
}
Ensure-PlayitService

if ($Once) { exit 0 }

# Monitor loop
while ($true) {
    foreach ($p in $Config.Processes) {
        $st = $State[$p.Name]
        $proc = $st.Process
        if ($proc.HasExited) {
            # Capture exit, close writers for previous instance
            try { Close-Writers $st } catch {}
            Note-CrashAndMaybeReboot -name $p.Name -exit ($proc.ExitCode) -st $st
            # Restart
            $new = $null
            Start-ManagedProcess $p ([ref]$new)
            $State[$p.Name] = $new
        }
    }

    Ensure-PlayitService
    Start-Sleep -Seconds $Config.CheckIntervalSeconds
}

# ============================
# Install-Orchestrator.ps1  (Run once as Administrator)
# Registers Supervisor.ps1 as a startup Scheduled Task under LocalSystem.
# ============================
<#
param(
    [string]$SupervisorPath = 'C:\Ops\Supervisor\Supervisor.ps1',
    [string]$TaskName = 'AppSupervisor'
)

# 1) Make sure Supervisor.ps1 is placed at $SupervisorPath and paths in its $Config are updated.
# 2) Then run this installer as Administrator.

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SupervisorPath)) { throw "Supervisor not found at $SupervisorPath" }

$action  = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ("-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$SupervisorPath`"")
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

try {
    if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    }
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings | Out-Null
    Write-Host "Installed scheduled task '$TaskName'. It will run at every boot as SYSTEM."
} catch {
    Write-Error $_
}

# To start immediately without reboot:
#  Start-ScheduledTask -TaskName $TaskName
# To remove later:
#  Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
#>
