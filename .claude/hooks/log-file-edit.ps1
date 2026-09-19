#requires -Version 7
$ErrorActionPreference = 'SilentlyContinue'

try {
    $payload = $input | Out-String | ConvertFrom-Json
    $filePath = $payload.tool_input.file_path
    if (-not $filePath) { exit 0 }

    $repoRoot = (& git rev-parse --show-toplevel 2>$null).Trim()
    if (-not $repoRoot) { exit 0 }

    $logsDir = Join-Path $repoRoot 'Operator/debug-logs'
    if (-not (Test-Path $logsDir)) { exit 0 }

    $latest = Get-ChildItem -Path $logsDir -Filter '*.md' -File |
        Where-Object { $_.Name -ne '_template.md' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $latest) { exit 0 }

    $timestamp = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')
    $line = "<!-- auto-logged: $timestamp - edited: $filePath -->"
    Add-Content -Path $latest.FullName -Value $line
} catch {
    # PostToolUse must never block
}

exit 0
