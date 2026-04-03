# Connection Setup for DirectClient SDK Validation

This guide documents how to set up and configure API connections for testing DirectClient SDK generated code.

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- Access to an Azure subscription with Logic Apps connections
- A Logic Apps Standard app (for creating connections with runtime URLs)

## Quick Start

Use the setup script to automate the entire process:

```powershell
.\scripts\Setup-Connection.ps1 `
    -SubscriptionId "<your-subscription-id>" `
    -ResourceGroup "<your-resource-group>" `
    -ConnectionName "<your-connection-name>"
```

For Office365 connections, use a different test path:

```powershell
.\scripts\Setup-Connection.ps1 `
    -SubscriptionId "<your-subscription-id>" `
    -ResourceGroup "<your-resource-group>" `
    -ConnectionName "office365" `
    -TestPath "/Categories"
```

---

## Connection Types

There are two ways to create connections, depending on whether you use the AI Gateway:

### Option A: AI Gateway Connection (Recommended for Triggers)

AI Gateway connections are required for connector triggers (AI Gateway manages the polling infrastructure). They also work for actions.

#### A1. Create an AI Gateway

```powershell
$subscriptionId = "<your-subscription-id>"
$resourceGroup = "<your-resource-group>"
$gatewayName = "<your-gateway-name>"
$location = "<azure-region>"  # e.g., "brazilsouth"

az rest --method PUT `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/aigateways/$gatewayName?api-version=2026-03-01-preview" `
    --body "{`"location`":`"$location`",`"properties`":{}}"
```

#### A2. Create a Connection in the AI Gateway

```powershell
$connectorName = "office365"  # API connector name
$connectionName = "office365-test"

az rest --method PUT `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/aigateways/$gatewayName/connections/$connectionName?api-version=2026-03-01-preview" `
    --body "{`"properties`":{`"connectorName`":`"$connectorName`"}}"
```

The connection is created in an **unauthenticated** state. You must complete OAuth consent.

#### A3. OAuth Consent via AI Gateway Manager Portal

1. Open the [AI Gateway Manager Portal](https://nice-desert-04d03581e.2.azurestaticapps.net/)
2. Run the command shown on the portal to get an ARM token, paste it, and save
3. Select your AI Gateway — the connections will appear
4. Click **Authorize** on the connection requiring consent
5. Complete the OAuth flow. Status changes from `Error` to `Connected`.

> **Note:** The DF consent endpoint (`/login`) returns 500. Only the portal UI consent flow works reliably.

#### A4. Get Connection Runtime URL

After OAuth consent, the connection runtime URL is available:
- In the AI Gateway Manager Portal: select the connection → copy the runtime URL
- Via ARM API:

```powershell
$result = az rest --method GET `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/aigateways/$gatewayName/connections/$connectionName?api-version=2026-03-01-preview" `
    -o json | ConvertFrom-Json
$result.properties.connectionRuntimeUrl
```

#### A5. Add Access Policy for Function App MSI

Grant the Function App's managed identity access to use the connection:

```powershell
$msiObjectId = "<function-app-msi-object-id>"
$tenantId = "<aad-tenant-id>"
$policyName = "functionapp-msi"

az rest --method PUT `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/aigateways/$gatewayName/connections/$connectionName/accessPolicies/$policyName?api-version=2026-03-01-preview" `
    --body "{`"properties`":{`"principal`":{`"type`":`"ActiveDirectory`",`"identity`":{`"objectId`":`"$msiObjectId`",`"tenantId`":`"$tenantId`"}}}}" `
    --headers "Content-Type=application/json"
```

> **Important:** The `Content-Type: application/json` header is required — omitting it returns HTTP 415.

The same connection (and access policy) is used for **both triggers and actions**.

### Option B: Standalone ARM Connection (Actions Only)

Standalone connections work for actions but do not support AI Gateway triggers.

#### Manual Steps (Option B — Standalone)

If you prefer standalone connections, follow these steps.

### Step 1: Create a Connection

The easiest way is through the Azure Portal:

1. Open your Logic Apps Standard app
2. Go to **Connections** > **Add connection**
3. Select the connector (e.g., SharePoint, Office365)
4. Complete OAuth authorization

Alternatively, connections can be created via ARM templates or CLI.

---

### Step 2: Get Connection Runtime URL

```powershell
# Replace with your values
$subscriptionId = "<your-subscription-id>"
$resourceGroup = "<your-resource-group>"
$connectionName = "<your-connection-name>"

# Get the runtime URL
az resource show `
    --ids "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connections/$connectionName" `
    --query "properties.connectionRuntimeUrl" `
    -o tsv
```

Expected output format:
```
https://{instance}.{region}.common.logic-{environment}.azure-apihub.net/apim/{connector}/{connection-id}
```

> **Note:** If the runtime URL is empty, the connection was created as a classic ARM connection, not through a Logic Apps Standard app. You'll need to create a new connection from a Logic Apps Standard app.

---

### Step 3: Check Connection Status

```powershell
az resource show `
    --ids "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connections/$connectionName" `
    --query "properties.statuses[0]" `
    -o json
```

Expected for healthy connection:
```json
{ "status": "Connected" }
```

If you see `invalid_grant` or `Unauthorized`, the connection needs to be re-authorized (see Step 4).

---

### Step 4: Re-authorize an Expired Connection

If the OAuth token has expired:

```powershell
# Create consent body file
$consentBody = @{
    parameters = @(
        @{
            redirectUrl = "https://portal.azure.com"
            parameterName = "token"
        }
    )
} | ConvertTo-Json -Depth 3
$consentBody | Out-File "$env:TEMP\consent-body.json" -Encoding UTF8

# Get consent link
az resource invoke-action `
    --ids "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connections/$connectionName" `
    --action "listConsentLinks" `
    --api-version "2018-07-01-preview" `
    --request-body "@$env:TEMP\consent-body.json" `
    -o json
```

Open the returned `link` URL in your browser to complete OAuth authorization.

---

### Step 5: Add Access Policy for CLI/Local Testing

DirectClient SDK uses `DefaultAzureCredential` which authenticates as your Azure CLI identity. You must grant your identity access to the connection.

#### Get Your Identity Information

```powershell
# Get your user object ID
$userObjectId = az ad signed-in-user show --query "id" -o tsv

# Get your tenant ID
$tenantId = az account show --query "tenantId" -o tsv
```

#### Create Access Policy

```powershell
$policyName = "local-dev"  # Any unique name

# Create access policy body
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
$accessPolicyBody | Out-File "$env:TEMP\access-policy.json" -Encoding UTF8

# Add the access policy
az rest --method PUT `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connections/$connectionName/accessPolicies/$policyName?api-version=2018-07-01-preview" `
    --body "@$env:TEMP\access-policy.json" `
    -o json
```

#### Verify Access Policy

```powershell
az rest --method GET `
    --uri "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connections/$connectionName/accessPolicies?api-version=2018-07-01-preview" `
    -o json
```

> **Note:** ACL propagation can take 1-5 minutes. If you get 403 errors immediately after adding the policy, wait and retry.

---

### Step 6: Test Connection via CLI

Before testing in your application, verify the connection works:

```powershell
# Replace with your actual runtime URL from Step 2
$runtimeUrl = "<your-runtime-url>"

# Test SharePoint - list available sites
az rest --method GET `
    --uri "$runtimeUrl/datasets" `
    --resource "https://apihub.azure.com" `
    -o json

# Test Office365 - get categories
az rest --method GET `
    --uri "$runtimeUrl/Categories" `
    --resource "https://apihub.azure.com" `
    -o json
```

---

### Step 7: Configure Your Application

Add the runtime URL to your application settings:

#### Azure Functions (local.settings.json)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Office365ConnectionRuntimeUrl": "<your-office365-runtime-url>",
    "SharepointonlineConnectionRuntimeUrl": "<your-sharepoint-runtime-url>"
  }
}
```

#### ASP.NET Core (appsettings.json)

```json
{
  "ConnectionRuntimeUrls": {
    "Office365": "<your-office365-runtime-url>",
    "Sharepointonline": "<your-sharepoint-runtime-url>"
  }
}
```

---

## Authentication Modes

The generated connector clients (e.g., `Office365Client`, `TeamsClient`) authenticate to API Hub using Azure credentials. Each client offers two constructors that cover three authentication modes:

### Mode 1: DefaultAzureCredential (Recommended for Local Development)

Uses the [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/?tabs=command-line#defaultazurecredential) chain, which tries multiple sources automatically — Azure CLI, Visual Studio, environment variables, and managed identity.

```csharp
var client = new Office365Client(connectionRuntimeUrl);
```

This is the simplest setup. Locally, it authenticates as your Azure CLI identity (`az login`), which must have an [access policy](#step-5-add-access-policy-for-clilocal-testing) on the connection for local testing. When deployed to Azure, it falls through to the app's managed identity, which must have an [access policy](#a5-add-access-policy-for-function-app-msi) on the connection.

### Mode 2: System-Assigned Managed Identity

Uses `ManagedIdentityCredential` with no client ID, targeting the app's system-assigned identity.

```csharp
var client = new Office365Client(
    connectionRuntimeUrl,
    managedIdentityClientId: null);
```

Pass `null` (or an empty string) for `managedIdentityClientId`. The Function App must have system-assigned managed identity enabled, and the identity must have an [access policy](#a5-add-access-policy-for-function-app-msi) on the connection.

### Mode 3: User-Assigned Managed Identity

Uses `ManagedIdentityCredential` with a specific client ID, targeting a user-assigned managed identity attached to the app.

```csharp
var client = new Office365Client(
    connectionRuntimeUrl,
    managedIdentityClientId: "<client-id-of-user-assigned-msi>");
```

Use this when multiple apps share a single identity, or when you need the identity to outlive any single app deployment.

### Choosing an Auth Mode

| Mode | When to use | Access policy identity |
|------|-------------|------------------------|
| DefaultAzureCredential | Local development, quick prototyping | Your Azure CLI user object ID |
| System-assigned MSI | Production deployment, single-app scenarios | Function App's system-assigned MSI object ID |
| User-assigned MSI | Production deployment, shared identity across apps | User-assigned MSI object ID |

> **Note:** All three modes require an access policy granting the identity permission to use the connection. The only difference is *which* identity's object ID goes into the policy.

### Custom Credentials

The primary constructor also accepts any `TokenCredential` directly, enabling advanced scenarios (e.g., certificate-based auth, chained credentials):

```csharp
var credential = new ChainedTokenCredential(
    new ManagedIdentityCredential(),
    new AzurePowerShellCredential());

var client = new Office365Client(connectionRuntimeUrl, credential);
```

---

## Troubleshooting

### Error: "Permission denied due to missing connection ACL"

The access policy hasn't propagated yet, or is missing. Solutions:
1. Wait 1-5 minutes for propagation
2. Verify the policy exists with the GET accessPolicies command
3. Ensure the objectId matches your signed-in identity

### Error: "invalid_grant" or token expired

The OAuth token has expired. Re-authorize using Step 4.

### Error: "connectionRuntimeUrl is null"

The connection was created as a classic ARM connection. Create a new connection through a Logic Apps Standard app.

### Error: 404 on runtime URL

The endpoint path may be incorrect. Check the connector's swagger/OpenAPI spec for valid paths.

---

## See Also

- [scripts/Setup-Connection.ps1](../scripts/Setup-Connection.ps1) - Automated setup script
- [ROADMAP.md](../ROADMAP.md) - Connector generation progress and lessons learned
- [DirectConnector POC](https://github.com/Azure/azure-connector-sdk-samples) - Working example with Office365 and SharePoint (planned)
