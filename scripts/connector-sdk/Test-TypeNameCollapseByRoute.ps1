#Requires -Version 7.0
<#
.SYNOPSIS
    Detects type-name collapses across routes in generated connector Extensions.cs files.

.DESCRIPTION
    The CodefulSdkGenerator derives C# type names from route paths and operation IDs.
    When two distinct API routes produce the same C# class name, one silently overwrites
    the other, creating a type that is semantically wrong for at least one route.

    This script finds cases where the same public class name appears more than once in
    a generated Extensions.cs file (excluding the client class itself and enums, which
    can legitimately share names in separate namespaces).

    Reports each collision as a blocker. Investigate and fix in the generator rather than
    hand-editing the generated output.

.PARAMETER GeneratedDir
    Path to the directory containing *Extensions.cs files to analyze.
    Defaults to src\Azure.Connectors.Sdk\Generated relative to the script's grandparent.

.PARAMETER Connectors
    Optional comma-separated list of connector base names to limit analysis to.

.EXAMPLE
    .\Test-TypeNameCollapseByRoute.ps1 -GeneratedDir .\src\Azure.Connectors.Sdk\Generated

.EXAMPLE
    .\Test-TypeNameCollapseByRoute.ps1 -Connectors "Teams,SigningHub"
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

$collapses = [System.Collections.Generic.List[pscustomobject]]::new()

foreach ($f in $files) {
    $lines = Get-Content $f.FullName

    # Collect declared public type names (class/struct only; enums are typically value types
    # and expected to be reused as extensible enums with the same name inside one namespace).
    $typeNames = @()
    foreach ($line in $lines) {
        if ($line -match '^\s+public\s+(sealed\s+|abstract\s+|readonly\s+)*(class|struct)\s+(\w+)') {
            $typeNames += $Matches[3]
        }
    }

    $counts = $typeNames | Group-Object | Where-Object { $_.Count -gt 1 }
    foreach ($dup in $counts) {
        $collapses.Add([pscustomobject]@{
            File      = $f.Name
            TypeName  = $dup.Name
            Occurrences = $dup.Count
            Status    = 'COLLISION'
        })
    }
}

if (-not $collapses) {
    Write-Output 'Test-TypeNameCollapseByRoute: PASS — no type-name collapses found.'
    exit 0
} else {
    Write-Warning "Test-TypeNameCollapseByRoute: FAIL — $($collapses.Count) type-name collapse(s) found (blocker)."
    $collapses | Format-Table -AutoSize
    exit 1
}
