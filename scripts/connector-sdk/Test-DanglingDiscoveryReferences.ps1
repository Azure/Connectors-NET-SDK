#Requires -Version 7.0
<#
.SYNOPSIS
    Detects dangling discovery operation references in generated connector Extensions.cs files.

.DESCRIPTION
    The connector SDK generator emits DynamicValues and DynamicSchema attributes whose
    OperationId arguments must resolve to a method on the same client class. This script
    finds attribute references that point to operation IDs not present in the generated
    output, which would silently produce empty drop-downs or schema failures at runtime.

    Reports each dangling reference as a blocker that must be resolved before shipping.

.PARAMETER GeneratedDir
    Path to the directory containing *Extensions.cs files to analyze.
    Defaults to src\Azure.Connectors.Sdk\Generated relative to the script's grandparent.

.PARAMETER Connectors
    Optional comma-separated list of connector base names to limit analysis to.

.EXAMPLE
    .\Test-DanglingDiscoveryReferences.ps1 -GeneratedDir .\src\Azure.Connectors.Sdk\Generated

.EXAMPLE
    .\Test-DanglingDiscoveryReferences.ps1 -Connectors "Teams,Office365"
#>
[CmdletBinding()]
param(
    [string]$GeneratedDir = '',
    [string]$Connectors = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $GeneratedDir) {
    $GeneratedDir = Join-Path $PSScriptRoot '..\..\src\Azure.Connectors.Sdk\Generated'
}
$GeneratedDir = Resolve-Path $GeneratedDir

$filterSet = @()
if ($Connectors) { $filterSet = $Connectors -split ',' | ForEach-Object { "$($_.Trim())Extensions.cs" } }

$files = Get-ChildItem $GeneratedDir -Filter "*Extensions.cs" |
    Where-Object { -not $filterSet -or $filterSet -contains $_.Name }

$danglers = [System.Collections.Generic.List[pscustomobject]]::new()

foreach ($f in $files) {
    $lines = Get-Content $f.FullName

    # Collect all method names with a route-invoking implementation
    $methods = $lines | Where-Object { $_ -match '^\s+public\s+(virtual\s+|override\s+)?async\s+Task' } |
        ForEach-Object {
            if ($_ -match 'Task[^>]*>\s+(\w+)\s*\(') { $Matches[1].ToLower() }
        }

    # Find all DynamicValues/DynamicSchema references — both named (OperationId = "...") and
    # positional (first argument string literal, e.g. [DynamicValues("GetAllTeams")]).
    $refs = $lines |
        Where-Object { $_ -match '\[DynamicValues\s*\(|OperationId\s*=' } |
        ForEach-Object {
            # Named: OperationId = "..."
            if ($_ -match 'OperationId\s*=\s*"([^"]+)"') { $Matches[1].ToLower() }
            # Positional: [DynamicValues("...")] or [DynamicSchema("...")]
            elseif ($_ -match '\[DynamicValues\s*\(\s*"([^"]+)"') { $Matches[1].ToLower() }
            elseif ($_ -match '\[DynamicSchema\s*\(\s*"([^"]+)"') { $Matches[1].ToLower() }
        } |
        Where-Object { $_ }

    foreach ($ref in $refs) {
        # Normalize: strip trailing 'async' suffix variants and check presence
        $normalized = $ref -replace 'async$',''
        $found = $methods | Where-Object { $_ -eq $ref -or $_ -eq $normalized -or $_ -eq "${ref}async" }
        if (-not $found) {
            $danglers.Add([pscustomobject]@{
                File       = $f.Name
                OperationId = $ref
                Status     = 'DANGLING'
            })
        }
    }
}

if (-not $danglers) {
    Write-Output 'Test-DanglingDiscoveryReferences: PASS — no dangling discovery references found.'
    exit 0
} else {
    Write-Warning "Test-DanglingDiscoveryReferences: FAIL — $($danglers.Count) dangling reference(s) found (blocker)."
    $danglers | Format-Table -AutoSize
    exit 1
}
