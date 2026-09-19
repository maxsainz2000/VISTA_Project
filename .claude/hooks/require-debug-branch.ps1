#requires -Version 7
$ErrorActionPreference = 'Stop'

try {
    $payload = $input | Out-String | ConvertFrom-Json
} catch {
    exit 0
}

$filePath = $null
if ($payload.tool_input) {
    $filePath = $payload.tool_input.file_path
}
if (-not $filePath) { exit 0 }

# Normalize to forward slashes for matching
$normalized = $filePath -replace '\\', '/'

# Whitelist: always allowed regardless of branch
$whitelistPatterns = @(
    '/\.claude/',
    '/Operator/debug-logs/',
    '/CLAUDE\.md$',
    '/LLM_Wiki/agent_wiki/',
    '/Progress/',
    '/Pending_Tasks/',
    '/Resolved/'
)

foreach ($pat in $whitelistPatterns) {
    if ($normalized -match $pat) { exit 0 }
}

# Determine current branch
try {
    $branch = (& git rev-parse --abbrev-ref HEAD 2>$null).Trim()
} catch {
    exit 0
}

if ($branch -like 'debug/*') {
    exit 0
}

[Console]::Error.WriteLine("BLOCKED: You must create a debug branch before editing source code during testing. Run: git checkout -b debug/<CHECKLIST-ID>-test-<N>")
exit 2
