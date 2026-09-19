# backup-mysqldump.ps1
# INFRA-29 — VISTA nightly backup script
# Produces a compressed mysqldump of merchsys_central and enforces retention:
#   - Daily:   keep last 7
#   - Weekly:  keep last 4 (Sunday dumps)
#   - Monthly: keep last 6 (1st-of-month dumps)
#
# Usage: Run via Windows Task Scheduler (see vista-nightly-backup.xml).
# Contact for issues: IT responsible person at Villon Farm Supply.
#
# Requirements:
#   - XAMPP installed at C:\xampp\
#   - 7-Zip installed at "C:\Program Files\7-Zip\7z.exe"
#   - Backup destination accessible (UNC path or local folder)
#   - backup_user account exists in MariaDB (see Runbook 01 §4c)

param(
    [string]$BackupUser     = "backup_user",
    [string]$BackupPassword = "",                          # Set via Task Scheduler action or environment
    [string]$Database       = "merchsys_central",
    [string]$BackupRoot     = "\\BACKUP-MACHINE\vista",   # Edit to match your backup share
    [string]$MysqlDumpPath  = "C:\xampp\mysql\bin\mysqldump.exe",
    [string]$SevenZipPath   = "C:\Program Files\7-Zip\7z.exe",
    [int]$KeepDaily         = 7,
    [int]$KeepWeekly        = 4,
    [int]$KeepMonthly       = 6
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Validate tools ────────────────────────────────────────────────────────────

if (-not (Test-Path $MysqlDumpPath)) {
    Write-Error "mysqldump.exe not found at: $MysqlDumpPath"
    exit 1
}
if (-not (Test-Path $SevenZipPath)) {
    Write-Error "7-Zip not found at: $SevenZipPath"
    exit 1
}
if ([string]::IsNullOrWhiteSpace($BackupPassword)) {
    Write-Error "BackupPassword parameter is required. Set it in the Task Scheduler action."
    exit 1
}

# ── Prepare output path ───────────────────────────────────────────────────────

$now   = Get-Date
$ts    = $now.ToString("yyyy-MM-dd_HH-mm")
$today = $now.ToString("yyyy-MM-dd")

New-Item -ItemType Directory -Force $BackupRoot | Out-Null
$outFile = Join-Path $BackupRoot "${Database}_${ts}.sql.gz"

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Starting backup → $outFile"

# ── Run mysqldump → pipe → 7-Zip gzip ────────────────────────────────────────

$dumpArgs = @(
    "--single-transaction",
    "--routines",
    "--triggers",
    "--events",
    "--user=$BackupUser",
    "--password=$BackupPassword",
    $Database
)
$zipArgs = @("a", "-tgzip", "-si", $outFile)

$dump = Start-Process -FilePath $MysqlDumpPath -ArgumentList $dumpArgs `
    -RedirectStandardOutput "PIPE" -NoNewWindow -PassThru
$zip  = Start-Process -FilePath $SevenZipPath  -ArgumentList $zipArgs `
    -RedirectStandardInput  "PIPE" -NoNewWindow -PassThru

# Pipe stdout of mysqldump into stdin of 7-Zip via temp file
# (PowerShell does not support native process-to-process piping cleanly)
$tempSql = [System.IO.Path]::GetTempFileName()
& $MysqlDumpPath @dumpArgs | Out-File -FilePath $tempSql -Encoding utf8 -NoNewline
& $SevenZipPath a -tgzip $outFile $tempSql 2>&1 | Out-Null
Remove-Item $tempSql -Force

# ── Verify output ─────────────────────────────────────────────────────────────

if (-not (Test-Path $outFile)) {
    Write-Error "Backup file not created: $outFile"
    exit 1
}
$sizeMb = [math]::Round((Get-Item $outFile).Length / 1MB, 2)
if ((Get-Item $outFile).Length -lt 1024) {
    Write-Error "Backup suspiciously small (${sizeMb} MB): $outFile"
    exit 1
}
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Backup complete: $outFile (${sizeMb} MB)"

# ── Retention cleanup ─────────────────────────────────────────────────────────

$allDumps = Get-ChildItem -Path $BackupRoot -Filter "${Database}_*.sql.gz" |
    Sort-Object LastWriteTime -Descending

# Categorise each dump
$daily   = [System.Collections.Generic.List[object]]::new()
$weekly  = [System.Collections.Generic.List[object]]::new()
$monthly = [System.Collections.Generic.List[object]]::new()

foreach ($f in $allDumps) {
    $fileDate = $f.LastWriteTime
    if ($fileDate.DayOfWeek -eq [DayOfWeek]::Sunday) { $weekly.Add($f) }
    if ($fileDate.Day -eq 1)                          { $monthly.Add($f) }
    $daily.Add($f)
}

$keep = [System.Collections.Generic.HashSet[string]]::new()

# Keep newest N of each tier
$daily   | Select-Object -First $KeepDaily   | ForEach-Object { $keep.Add($_.FullName) | Out-Null }
$weekly  | Select-Object -First $KeepWeekly  | ForEach-Object { $keep.Add($_.FullName) | Out-Null }
$monthly | Select-Object -First $KeepMonthly | ForEach-Object { $keep.Add($_.FullName) | Out-Null }

$pruned = 0
foreach ($f in $allDumps) {
    if (-not $keep.Contains($f.FullName)) {
        Remove-Item $f.FullName -Force
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Pruned: $($f.Name)"
        $pruned++
    }
}

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Retention cleanup: $pruned file(s) removed."
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Backup run complete."
exit 0
