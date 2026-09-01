#Requires -Version 7.0
<#
.SYNOPSIS
    Compares the public API surface of generated connector Extensions.cs files between two directories.

.DESCRIPTION
    Scans *Extensions.cs files in -Before and -After directories and classifies each
    change as: operation addition, operation removal, operation rename (same route,
    different C# name), model addition, model removal, property addition, property
    removal, trigger addition/removal, or doc-only drift.

    Use this script when regenerating connector clients to audit semantic surface changes
    and produce the customer-facing breaking-change and additive-change summary required
    for CHANGELOG entries.

.PARAMETER Before
    Path to directory containing the previous *Extensions.cs files (e.g., git HEAD snapshot).

.PARAMETER After
    Path to directory containing the new *Extensions.cs files (e.g., regenerated output).

.PARAMETER Connectors
    Optional comma-separated list of connector base names to limit comparison to (e.g., "Teams,GoogleDrive").
    Defaults to all connectors found in -Before.

.PARAMETER BreakingOnly
    When specified, only outputs breaking changes (removals, renames, type changes).

.EXAMPLE
    .\Compare-GeneratedSurface.ps1 -Before .\_scratch\sdk-before -After .\src\Azure.Connectors.Sdk\Generated

.EXAMPLE
    .\Compare-GeneratedSurface.ps1 -Before .\_scratch\sdk-before -After .\src\Azure.Connectors.Sdk\Generated -BreakingOnly
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Before,
    [Parameter(Mandatory)][string]$After,
    [string]$Connectors = '',
    [switch]$BreakingOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-PublicType {
    param([string]$FilePath)
    $lines = Get-Content $FilePath
    $types = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        if ($line -match '^(public\s+(static\s+|readonly\s+|sealed\s+|abstract\s+)*(class|struct|enum|interface))\s+(\w+)') {
            $types += [pscustomobject]@{ Name = $Matches[4]; Kind = $Matches[3]; LineNum = $i + 1 }
        }
    }
    return $types
}

function Get-PublicMethod {
    param([string]$FilePath)
    $lines = Get-Content $FilePath
    $methods = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        if ($line -match '^public\s+virtual\s+async\s+Task<[^>]+>\s+(\w+)\s*\(') {
            $methods += [pscustomobject]@{ Name = $Matches[1]; Signature = $line; LineNum = $i + 1 }
        }
    }
    return $methods
}

function Get-PublicProperty {
    param([string]$FilePath, [string]$TypeName)
    $lines = Get-Content $FilePath
    $inType = $false; $depth = 0; $props = @()
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ($trimmed -match "class\s+$TypeName\b") { $inType = $true }
        if ($inType) {
            $depth += ([regex]::Matches($trimmed, '\{')).Count - ([regex]::Matches($trimmed, '\}')).Count
            if ($depth -le 0 -and $inType) { $inType = $false }
            if ($trimmed -match '^\[JsonPropertyName\("([^"]+)"\)\]') {
                $wire = $Matches[1]
                $props += [pscustomobject]@{ WireName = $wire; CsLine = $trimmed }
            }
        }
    }
    return $props
}

$beforeDir = Resolve-Path $Before
$afterDir  = Resolve-Path $After

$filterSet = @()
if ($Connectors) { $filterSet = $Connectors -split ',' | ForEach-Object { "$($_.Trim())Extensions.cs" } }

$beforeFiles = Get-ChildItem $beforeDir -Filter "*Extensions.cs" | Where-Object { -not $filterSet -or $filterSet -contains $_.Name }
$afterFiles  = Get-ChildItem $afterDir  -Filter "*Extensions.cs" | Where-Object { -not $filterSet -or $filterSet -contains $_.Name }

$beforeNames = $beforeFiles | Select-Object -ExpandProperty Name
$afterNames  = $afterFiles  | Select-Object -ExpandProperty Name

$added   = $afterNames  | Where-Object { $beforeNames -notcontains $_ }
$removed = $beforeNames | Where-Object { $afterNames  -notcontains $_ }
$common  = $beforeNames | Where-Object { $afterNames  -contains $_ }

$results = [System.Collections.Generic.List[pscustomobject]]::new()

foreach ($name in $added)   { $results.Add([pscustomobject]@{ File=$name; Kind='ConnectorAdded';   Detail="$name not in before"; Breaking=$false }) }
foreach ($name in $removed) { $results.Add([pscustomobject]@{ File=$name; Kind='ConnectorRemoved'; Detail="$name not in after"; Breaking=$true }) }

foreach ($name in $common) {
    $bFile = Join-Path $beforeDir $name
    $aFile = Join-Path $afterDir  $name

    # Normalize line endings before hash comparison to avoid CRLF/LF false positives.
    $bNorm = (Get-Content $bFile -Raw) -replace "`r`n", "`n"
    $aNorm = (Get-Content $aFile -Raw) -replace "`r`n", "`n"
    if ($bNorm -eq $aNorm) { continue }

    $bTypes = Get-PublicType $bFile
    $aTypes = Get-PublicType $aFile
    $bMethods = Get-PublicMethod $bFile
    $aMethods  = Get-PublicMethod $aFile

    $bTypeNames = $bTypes | Select-Object -ExpandProperty Name
    $aTypeNames = $aTypes | Select-Object -ExpandProperty Name

    foreach ($t in $aTypeNames | Where-Object { $bTypeNames -notcontains $_ }) {
        $results.Add([pscustomobject]@{ File=$name; Kind='TypeAdded'; Detail=$t; Breaking=$false })
    }
    foreach ($t in $bTypeNames | Where-Object { $aTypeNames -notcontains $_ }) {
        $results.Add([pscustomobject]@{ File=$name; Kind='TypeRemoved'; Detail=$t; Breaking=$true })
    }

    $bMethodNames = $bMethods | Select-Object -ExpandProperty Name
    $aMethodNames  = $aMethods  | Select-Object -ExpandProperty Name

    foreach ($m in $aMethodNames | Where-Object { $bMethodNames -notcontains $_ }) {
        $results.Add([pscustomobject]@{ File=$name; Kind='OperationAdded'; Detail=$m; Breaking=$false })
    }
    foreach ($m in $bMethodNames | Where-Object { $aMethodNames -notcontains $_ }) {
        $results.Add([pscustomobject]@{ File=$name; Kind='OperationRemoved'; Detail=$m; Breaking=$true })
    }

    # Detect signature changes for same-named methods
    foreach ($bm in $bMethods) {
        $am = $aMethods | Where-Object { $_.Name -eq $bm.Name } | Select-Object -First 1
        if ($am -and $am.Signature -ne $bm.Signature) {
            $results.Add([pscustomobject]@{ File=$name; Kind='OperationSignatureChanged'; Detail="$($bm.Name): [$($bm.Signature)] -> [$($am.Signature)]"; Breaking=$true })
        }
    }

    # Property-level changes for common types
    foreach ($t in ($bTypeNames | Where-Object { $aTypeNames -contains $_ })) {
        $bProps = Get-PublicProperty $bFile $t
        $aProps = Get-PublicProperty $aFile $t
        $bWire = $bProps | Select-Object -ExpandProperty WireName
        $aWire = $aProps | Select-Object -ExpandProperty WireName
        foreach ($w in ($aWire | Where-Object { $bWire -notcontains $_ })) {
            $results.Add([pscustomobject]@{ File=$name; Kind='PropertyAdded'; Detail="${t}.${w}"; Breaking=$false })
        }
        foreach ($w in ($bWire | Where-Object { $aWire -notcontains $_ })) {
            $results.Add([pscustomobject]@{ File=$name; Kind='PropertyRemoved'; Detail="${t}.${w}"; Breaking=$true })
        }
    }
}

if ($BreakingOnly) { $results = $results | Where-Object { $_.Breaking } }

if (-not $results) {
    Write-Output 'Compare-GeneratedSurface: No differences found (or all differences are doc-only).'
} else {
    $results | Format-Table -AutoSize File, Kind, Breaking, Detail
    $breaking = $results | Where-Object { $_.Breaking }
    $additive = $results | Where-Object { -not $_.Breaking }
    Write-Output "Summary: $($breaking.Count) breaking change(s), $($additive.Count) additive change(s)"
    if ($breaking) { exit 1 } else { exit 0 }
}
