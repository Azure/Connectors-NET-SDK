---
name: connection-setup
description: 'Create and configure Connector Gateway connections for SDK-supported connectors. USE WHEN: setting up a new connector connection, creating a Connector Gateway, authorizing OAuth consent, adding access policies, or configuring local.settings.json / app settings. Covers Office365, SharePoint, Teams, and any Microsoft.Web/connections connector. NOT FOR: general SDK usage, trigger registration, or code generation.'
---

# Connector Gateway Connection Setup

Automates the end-to-end connection lifecycle for SDK-supported connectors, keeping the developer in VS Code.

## When to Use

- Developer needs a new connector connection for local dev or a deployed compute host
- Developer needs to authorize (OAuth consent) a connection
- Developer needs to wire connection URLs into `local.settings.json` or deployed app settings
- Developer needs to grant access policies (CLI identity for local, managed identity for deployed)

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Target subscription and resource group known
- For deployed scenarios: compute host (e.g., Function App, App Service) with managed identity enabled
- **Supported regions** for Connector Gateway: `brazilsouth`, `centraluseuap`, `eastus2euap`, `centralusstage`, `eastusstage`. Only the gateway `location` must be in a supported region; the resource group and Function App can be in any region.

## Procedure

### Step 1: Create or Select Connector Gateway

Check for an existing Connector Gateway in the resource group:

```powershell
$subscriptionId = "<subscription-id>"
$resourceGroup = "<resource-group>"

az rest --method GET `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways?api-version=2026-05-01-preview" `
    -o json | ConvertFrom-Json | Select-Object -ExpandProperty value | Select-Object name
```

If none exists, create one:

```powershell
$gatewayName = "<gateway-name>"
$location = "<azure-region>"

$gwBody = "{`"location`":`"$location`",`"identity`":{`"type`":`"SystemAssigned`"},`"properties`":{}}"
$tempFile = Join-Path $env:TEMP "gw-body.json"
[System.IO.File]::WriteAllText($tempFile, $gwBody)
az rest --method PUT `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways/$gatewayName?api-version=2026-05-01-preview" `
    --body "@$tempFile" --headers "Content-Type=application/json" -o json
Remove-Item $tempFile -ErrorAction SilentlyContinue
```

> **Important:** The gateway must have a managed identity enabled (`SystemAssigned`) for trigger callback authentication. If the gateway was created without an identity, update it:
>
> ```powershell
> $gwBody = "{`"location`":`"$location`",`"identity`":{`"type`":`"SystemAssigned`"},`"properties`":{}}"
> $tempFile = Join-Path $env:TEMP "gw-identity.json"
> [System.IO.File]::WriteAllText($tempFile, $gwBody)
> az rest --method PUT `
>     --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways/$gatewayName?api-version=2026-05-01-preview" `
>     --body "@$tempFile" --headers "Content-Type=application/json" -o json
> Remove-Item $tempFile -ErrorAction SilentlyContinue
> ```

### Step 2: Create Connection

Supported SDK connector names: `azureblob`, `mq`, `office365`, `office365users`, `onedriveforbusiness`, `sharepointonline`, `smtp`, `teams`, `msgraphgroupsanduser` (and any `Microsoft.Web/connections` connector name).

```powershell
$connectorName = "<connector-name>"      # e.g., "mq", "office365", "office365users", "onedriveforbusiness", "sharepointonline", "smtp", "teams", "msgraphgroupsanduser"
$connectionName = "<connection-name>"    # e.g., "office365-test", "sharepoint-test"

$gwId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways/$gatewayName"
$connBody = "{`"properties`":{`"connectorName`":`"$connectorName`"}}"
$tempFile = Join-Path $env:TEMP "conn-body.json"
[System.IO.File]::WriteAllText($tempFile, $connBody)
az rest --method PUT `
    --uri "https://management.azure.com${gwId}/connections/${connectionName}?api-version=2026-05-01-preview" `
    --body "@$tempFile" --headers "Content-Type=application/json" -o json | ConvertFrom-Json | Select-Object name, @{n='status';e={$_.properties.statuses[0].status}}
Remove-Item $tempFile -ErrorAction SilentlyContinue
```

The connection starts in **Error** state (unauthenticated). Proceed to Step 3.

### Step 3: OAuth Consent (In-Browser)

Retrieve the consent link and open it in the default browser — no portal needed:

```powershell
$consentBody = '{"parameters":[{"redirectUrl":"https://portal.azure.com","parameterName":"token"}]}'
$tempFile = Join-Path $env:TEMP "consent-body.json"
[System.IO.File]::WriteAllText($tempFile, $consentBody)
$result = az rest --method POST `
    --uri "https://management.azure.com${gwId}/connections/${connectionName}/listConsentLinks?api-version=2026-05-01-preview" `
    --body "@$tempFile" --headers "Content-Type=application/json" -o json | ConvertFrom-Json
Remove-Item $tempFile -ErrorAction SilentlyContinue

$link = $result.value[0].link
Start-Process $link
```

The user completes OAuth in the browser. After consent, verify:

```powershell
az rest --method GET `
    --uri "https://management.azure.com${gwId}/connections/${connectionName}?api-version=2026-05-01-preview" `
    -o json | ConvertFrom-Json | Select-Object @{n='status';e={$_.properties.statuses[0].status}}
```

Expected: `Connected`.

### Step 4: Get Connection Runtime URL

```powershell
$conn = az rest --method GET `
    --uri "https://management.azure.com${gwId}/connections/${connectionName}?api-version=2026-05-01-preview" `
    -o json | ConvertFrom-Json
$runtimeUrl = $conn.properties.connectionRuntimeUrl
Write-Output "Runtime URL: $runtimeUrl"
```

### Step 5: Add Access Policies

> **Note:** Access policies control which identities can call the connection's runtime URL for connector **actions** (e.g., send email, list files). For **trigger-only** scenarios, the Connector Gateway polls server-side and does not need an access policy on the connection. Skip this step if your function only receives trigger callbacks and does not call connector actions at runtime.

#### For local development (Azure CLI identity)

```powershell
$userObjectId = az ad signed-in-user show --query "id" -o tsv
$tenantId = az account show --query "tenantId" -o tsv

$policyBody = "{`"properties`":{`"principal`":{`"type`":`"ActiveDirectory`",`"identity`":{`"objectId`":`"$userObjectId`",`"tenantId`":`"$tenantId`"}}}}"
$tempFile = Join-Path $env:TEMP "policy-body.json"
[System.IO.File]::WriteAllText($tempFile, $policyBody)
az rest --method PUT `
    --uri "https://management.azure.com${gwId}/connections/${connectionName}/accessPolicies/local-dev?api-version=2026-05-01-preview" `
    --body "@$tempFile" --headers "Content-Type=application/json" -o json | ConvertFrom-Json | Select-Object name
Remove-Item $tempFile -ErrorAction SilentlyContinue
```

#### For deployed compute host (e.g., Function App with system-assigned MSI)

```powershell
$functionAppName = "<function-app-name>"
$msiObjectId = az functionapp identity show -g $resourceGroup -n $functionAppName --query "principalId" -o tsv
$tenantId = az account show --query "tenantId" -o tsv

$policyBody = "{`"properties`":{`"principal`":{`"type`":`"ActiveDirectory`",`"identity`":{`"objectId`":`"$msiObjectId`",`"tenantId`":`"$tenantId`"}}}}"
$tempFile = Join-Path $env:TEMP "msi-policy-body.json"
[System.IO.File]::WriteAllText($tempFile, $policyBody)
az rest --method PUT `
    --uri "https://management.azure.com${gwId}/connections/${connectionName}/accessPolicies/functionapp-msi?api-version=2026-05-01-preview" `
    --body "@$tempFile" --headers "Content-Type=application/json" -o json | ConvertFrom-Json | Select-Object name
Remove-Item $tempFile -ErrorAction SilentlyContinue
```

> ACL propagation takes 1-5 minutes. If you get 403 errors immediately after adding, wait and retry.

### Step 6: Configure App Settings

> **Note:** Connection app settings are only needed when your function code calls connector **actions** at runtime using `ConnectorConnectionResolver`. For **trigger-only** scenarios, the function receives callbacks directly from the Connector Gateway and does not need these settings. Skip this step if your function only receives trigger callbacks.

The SDK's `ConnectorConnectionResolver` reads connection settings using the Azure Functions `__` (double-underscore) environment variable separator convention.

#### Connection setting name

Choose a connection setting name (e.g., `office365`, `sharepoint`, `teams`). This is passed to `ConnectorConnectionResolver.Resolve(connectionSettingName)` and used as the prefix for the `__` keys.

#### Format B — Direct URL (actions only)

Add to `local.settings.json` under `"Values"`:

```json
{
  "{connectionSettingName}__connectionRuntimeUrl": "<runtime-url-from-step-4>"
}
```

#### Format A — Connector Gateway (triggers + actions)

```json
{
  "{connectionSettingName}__connectorGatewayName": "<gateway-name>",
  "{connectionSettingName}__connectionName": "<connection-name>"
}
```

#### Deployed compute host (e.g., Function App)

Format B:

```powershell
az functionapp config appsettings set `
    -g $resourceGroup -n $functionAppName `
    --settings "{connectionSettingName}__connectionRuntimeUrl=$runtimeUrl"
```

Format A:

```powershell
az functionapp config appsettings set `
    -g $resourceGroup -n $functionAppName `
    --settings "{connectionSettingName}__connectorGatewayName=$gatewayName" "{connectionSettingName}__connectionName=$connectionName"
```

### Step 7: Verify Connection

Test the connection works end-to-end:

```powershell
# Office365
az rest --method GET --uri "$runtimeUrl/Categories" --resource "https://apihub.azure.com" -o json

# SharePoint
az rest --method GET --uri "$runtimeUrl/datasets" --resource "https://apihub.azure.com" -o json

# Teams — list joined teams to verify Teams connection
az rest --method GET --uri "$runtimeUrl/beta/me/joinedTeams" --resource "https://apihub.azure.com" -o json

# OneDrive for Business — list root folder
az rest --method GET --uri "$runtimeUrl/datasets/default/folders" --resource "https://apihub.azure.com" -o json
```

## Next Steps

- **Triggers:** To register polling triggers (e.g., OnNewEmail, OnNewFile), use the [trigger-registration skill](../trigger-registration/SKILL.md).
