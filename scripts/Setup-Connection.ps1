<#
.SYNOPSIS
    Sets up a Logic Apps connection for local DirectClient SDK testing.

.DESCRIPTION
    This script:
    1. Gets the connection runtime URL
    2. Adds an access policy for your Azure CLI identity
    3. Tests the connection
    4. Outputs the runtime URL for local.settings.json

    Supports both standalone API connections (Microsoft.Web/connections) and
    Connector Namespace connections (Microsoft.Web/connectorGateways). Use the
    -NamespaceName parameter to target a Connector Namespace connection.

.PARAMETER SubscriptionId
    The Azure subscription ID containing the connection.

.PARAMETER ResourceGroup
    The resource group containing the connection.

.PARAMETER NamespaceName
    The Connector Namespace name. When provided, the script uses
    Microsoft.Web/connectorGateways URIs (api-version 2026-05-01-preview).
    When omitted, legacy Microsoft.Web/connections behavior is used.

.PARAMETER ConnectionName
    The name of the API connection (e.g., "sharepointonline-1").

.PARAMETER PolicyName
    The name for the access policy (default: "local-dev").

.PARAMETER TestPath
    The API path to test (default: "/datasets" for SharePoint, "/Categories" for Office365).

.PARAMETER SkipTest
    Skip the connection test step.

.EXAMPLE
    # Legacy: standalone API connection
    .\Setup-Connection.ps1 -SubscriptionId "00000000-0000-0000-0000-000000000000" `
        -ResourceGroup "my-resource-group" -ConnectionName "sharepointonline-1"

.EXAMPLE
    # Legacy: standalone Office365 connection
    .\Setup-Connection.ps1 -SubscriptionId "00000000-0000-0000-0000-000000000000" `
        -ResourceGroup "my-resource-group" -ConnectionName "office365" -TestPath "/Categories"

.EXAMPLE
    # Connector Namespace connection
    .\Setup-Connection.ps1 -SubscriptionId "00000000-0000-0000-0000-000000000000" `
        -ResourceGroup "my-resource-group" -NamespaceName "my-namespace" `
        -ConnectionName "office365-test"
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$ResourceGroup,

    [string]$NamespaceName,

    [Parameter(Mandatory = $true, Position = 2)]
    [string]$ConnectionName,

    [string]$PolicyName = "local-dev",

    [string]$TestPath = "/datasets",

    [switch]$SkipTest
)

$ErrorActionPreference = "Stop"

$useNamespace = -not [string]::IsNullOrWhiteSpace($NamespaceName)
if ($useNamespace) { $NamespaceName = $NamespaceName.Trim() }

$connectorNamespacePortalUrl = "https://nice-desert-04d03581e.2.azurestaticapps.net/"

Write-Host "=== DirectClient SDK Connection Setup ===" -ForegroundColor Cyan
if ($useNamespace) {
    Write-Host "  Mode: Connector Namespace (Microsoft.Web/connectorGateways)" -ForegroundColor Magenta
} else {
    Write-Host "  Mode: Legacy (Microsoft.Web/connections)" -ForegroundColor Magenta
}
Write-Host ""

# Step 1: Get user info
Write-Host "[1/4] Getting user identity..." -ForegroundColor Yellow
$userObjectId = az ad signed-in-user show --query "id" -o tsv
if (-not $userObjectId) {
    Write-Error "Failed to get user object ID. Make sure you're logged in with 'az login'."
    exit 1
}
$tenantId = az account show --query "tenantId" -o tsv
Write-Host "  User Object ID: $userObjectId"
Write-Host "  Tenant ID: $tenantId"

# Step 2: Get runtime URL
Write-Host ""
Write-Host "[2/4] Getting connection runtime URL..." -ForegroundColor Yellow

if ($useNamespace) {
    $connectionResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Web/connectorGateways/$NamespaceName/connections/$ConnectionName"
    $apiVersion = "2026-05-01-preview"
    $connectionUri = "https://management.azure.com$connectionResourceId`?api-version=$apiVersion"

    $connectionJson = az rest --method GET --uri $connectionUri -o json
    if (-not $connectionJson) {
        Write-Error @"
Failed to get Connector Namespace connection. Verify:
  - Namespace '$NamespaceName' exists in resource group '$ResourceGroup'
  - Connection '$ConnectionName' exists in the namespace
  - You have completed OAuth consent via the Connector Namespace Manager Portal
    ($connectorNamespacePortalUrl)
"@
        exit 1
    }

    $connectionObj = $connectionJson | ConvertFrom-Json
    $runtimeUrl = $connectionObj.properties.connectionRuntimeUrl
    $statuses = $connectionObj.properties.statuses
    $status = if ($statuses -and $statuses.Count -gt 0) { $statuses[0].status } else { "Unknown" }

    if (-not $runtimeUrl) {
        Write-Error @"
Runtime URL is empty. For Connector Namespace connections, you must complete
OAuth consent via the Connector Namespace Manager Portal before the runtime URL
is available:
  1. Open $connectorNamespacePortalUrl
  2. Select your namespace '$NamespaceName'
  3. Click Authorize on connection '$ConnectionName' and complete the OAuth flow
  4. Re-run this script after the status changes to 'Connected'
"@
        exit 1
    }
} else {
    $connectionResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Web/connections/$ConnectionName"
    $apiVersion = "2018-07-01-preview"

    $runtimeUrl = az resource show --ids $connectionResourceId --query "properties.connectionRuntimeUrl" -o tsv
    if (-not $runtimeUrl) {
        Write-Error @"
Runtime URL is empty. This connection was likely created as a classic ARM connection.
Please create a new connection through a Logic Apps Standard app.
"@
        exit 1
    }

    $status = az resource show --ids $connectionResourceId --query "properties.statuses[0].status" -o tsv
}

Write-Host "  Runtime URL: $runtimeUrl"
Write-Host "  Status: $status"

if ($status -ne "Connected") {
    Write-Warning "Connection is not in 'Connected' state. You may need to re-authorize."
    if ($useNamespace) {
        Write-Host "  Complete OAuth consent via the Connector Namespace Manager Portal:" -ForegroundColor Yellow
        Write-Host "  $connectorNamespacePortalUrl" -ForegroundColor Yellow
    } else {
        Write-Host "  Run: az resource invoke-action --ids '$connectionResourceId' --action 'listConsentLinks' --api-version '$apiVersion'"
    }
}

# Step 3: Add access policy
Write-Host ""
Write-Host "[3/4] Adding access policy '$PolicyName'..." -ForegroundColor Yellow

$accessPolicyBody = @{
    properties = @{
        principal = @{
            type = "ActiveDirectory"
            identity = @{
                objectId = $userObjectId
                tenantId = $tenantId
            }
        }
    }
} | ConvertTo-Json -Depth 5

$tempFile = Join-Path $env:TEMP "access-policy-$ConnectionName.json"
$accessPolicyBody | Out-File $tempFile -Encoding UTF8

$policyUri = "https://management.azure.com$connectionResourceId/accessPolicies/$PolicyName`?api-version=$apiVersion"

try {
    if ($useNamespace) {
        $result = az rest --method PUT --uri $policyUri --body "@$tempFile" --headers "Content-Type=application/json" -o json
    } else {
        $result = az rest --method PUT --uri $policyUri --body "@$tempFile" -o json
    }
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Access policy added successfully."
    } else {
        Write-Warning "Failed to add access policy: $result"
    }
} catch {
    Write-Warning "Failed to add access policy: $_"
}

# Step 4: Test connection
if (-not $SkipTest) {
    Write-Host ""
    Write-Host "[4/4] Testing connection (waiting 30s for ACL propagation)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30

    $testUri = "$runtimeUrl$TestPath"
    Write-Host "  Testing: $testUri"

    try {
        $testResult = az rest --method GET --uri $testUri --resource "https://apihub.azure.com" -o json
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Connection test successful!" -ForegroundColor Green
        } else {
            Write-Warning "Connection test failed. The ACL may still be propagating. Try again in a few minutes."
            Write-Host "  Error: $testResult"
        }
    } catch {
        Write-Warning "Connection test failed: $_"
    }
} else {
    Write-Host ""
    Write-Host "[4/4] Skipping connection test." -ForegroundColor Yellow
}

# Output configuration
Write-Host ""
Write-Host "=== Configuration ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Add this to your local.settings.json:" -ForegroundColor Green

# Derive a settings key name from the connection name
$settingsKey = ($ConnectionName -replace '-', '' -replace '_', '') + "ConnectionRuntimeUrl"
if ([string]::IsNullOrEmpty($settingsKey)) {
    Write-Error "ConnectionName must not be empty."
    exit 1
}
$settingsKey = $settingsKey.Substring(0, 1).ToUpper() + $settingsKey.Substring(1)

Write-Host @"

{
  "Values": {
    "$settingsKey": "$runtimeUrl"
  }
}
"@

Write-Host ""
Write-Host "Done!" -ForegroundColor Green

# Clean up temp file
Remove-Item $tempFile -ErrorAction SilentlyContinue
