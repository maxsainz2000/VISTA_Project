#requires -Version 7
$ErrorActionPreference = 'Stop'

try {
    $payload = $input | Out-String | ConvertFrom-Json
} catch {
    exit 0
}

$command = $null
if ($payload.tool_input) {
    $command = $payload.tool_input.command
}
if (-not $command) { exit 0 }

if ($command -notmatch 'git\s+commit') { exit 0 }

try {
    $branch = (& git rev-parse --abbrev-ref HEAD 2>$null).Trim()
} catch {
    exit 0
}

if ($branch -eq 'main' -or $branch -eq 'master') {
    [Console]::Error.WriteLine("BLOCKED: Cannot commit directly to $branch during testing. Create a debug branch first: git checkout -b debug/<CHECKLIST-ID>-test-<N>")
    exit 2
}

exit 0
