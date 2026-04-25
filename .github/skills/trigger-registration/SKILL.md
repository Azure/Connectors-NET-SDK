---
name: trigger-registration
description: 'Register Connector Gateway trigger configs for SDK-supported connectors. USE WHEN: setting up polling triggers (e.g., OnNewEmail, OnNewFile, OnUpdatedFile) that call back to an Azure Function when events occur, creating a .NET Function App project with ConnectorTrigger, or wiring post-deploy trigger config scripts. Covers function app scaffolding, trigger config creation, callback URL wiring, parameter discovery, and binary vs metadata payload handling. NOT FOR: connection setup (use connection-setup skill).'
---

# Connector Gateway Trigger Registration

Registers polling trigger configs on a Connector Gateway so that connector events (new email, new file, etc.) call back to your Azure Function endpoint. Also covers scaffolding a .NET Function App project with the connector extension packages.

## When to Use

- Developer needs a connector trigger (e.g., "when a new file is created in OneDrive")
- Developer has an existing Connector Gateway connection (use the `connection-setup` skill first if not)
- Developer needs to wire up the callback URL from a deployed Azure Function
- Developer needs to understand binary vs metadata trigger payload shapes

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Connector Gateway with a connected connector (see `connection-setup` skill)
- The gateway must have a **system-assigned managed identity** enabled (required for trigger callback authentication)
- Deployed Azure Function App with a connector trigger function
- **Supported regions** for Connector Gateway: `brazilsouth`, `centraluseuap`, `eastus2euap`, `centralusstage`, `eastusstage`. Only the gateway `location` must be in a supported region; the Function App can be in any region.
- For .NET: the Function App project must reference:
  - `Microsoft.Azure.Functions.Worker.Extensions.Connector` — provides the `[ConnectorTrigger]` attribute
  - `Microsoft.Azure.Connectors.Sdk` — provides typed connector clients and trigger payload types

## Key Concepts

### Trigger Config vs Connection

Connections (managed by the `connection-setup` skill) authenticate your app to the connector API. Trigger configs tell the Connector Gateway to **poll** the connector for events and **POST callbacks** to your function when events occur.

```text
Connector Gateway
├── connections/
│   └── onedrive-test          ← auth + runtime URL (connection-setup skill)
└── triggerConfigs/
    └── onedrive-newfile       ← poll + callback config (THIS skill)
```

### Binary vs Metadata Triggers

Some connectors offer two variants of the same trigger:

| Variant | Example | Payload Shape | Body Field |
|---------|---------|---------------|------------|
| **File content (binary)** | `OnNewFileV2` | `{"body":"<base64-string>"}` | String — base64-encoded file bytes |
| **Properties only (metadata)** | `OnNewFilesV2` | `{"body":{"value":[{...}]}}` | Object — array of typed metadata items |

**Critical:** Both variants arrive with `Content-Type: application/json`. You cannot use content-type to distinguish them. Instead, parse the JSON and inspect whether `body` is a string or an object.

#### Identifying binary triggers in the SDK

In the generated `*TriggerOperations` class, **binary triggers lack a `Payload type:` annotation** in their XML doc comment, while metadata triggers reference their typed payload class:

```csharp
// Binary — no payload type annotation
/// <summary>When a file is created.</summary>
public const string OnNewFile = "OnNewFileV2";

// Metadata — has payload type annotation
/// <summary>
/// When a file is created (properties only).
/// Payload type: <see cref="OnedriveforbusinessOnNewFilesTriggerPayload"/>.
/// </summary>
public const string OnNewFiles = "OnNewFilesV2";
```

If the connector generates a `*Triggers.Operations` dictionary, that dictionary only maps metadata triggers (those with typed payloads). If an operation name is **not** in the dictionary, it is a binary trigger.

Some connectors may not generate a `*Triggers` registry at all. When `*Triggers.Operations` is unavailable, check the XML doc comment on the `*TriggerOperations` constant: a `Payload type:` annotation indicates a metadata trigger, while no annotation indicates a binary trigger.

## .NET Function App with Connector Extension

### Scaffolding a New Project

There is no `ConnectorTrigger` template yet. Use `azd` with an HTTP trigger template and replace the trigger:

1. **Initialize** with the Azure Functions .NET quickstart:

   ```shell
   azd init -t functions-quickstart-dotnet-azd
   ```

2. **Replace the HTTP trigger** with a `[ConnectorTrigger]` function. Delete any sample HTTP functions and create your trigger function (see example below).

3. **Add packages** to the Function App `.csproj`:

   ```xml
   <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Connector" Version="0.1.0-alpha" />
   ```

   For SDK typed payloads, also add:

   ```xml
   <PackageReference Include="Microsoft.Azure.Connectors.Sdk" Version="*" />
   ```

   > **Note:** If `Microsoft.Azure.Connectors.Sdk` is not yet published on NuGet, use a project reference to the local SDK repo instead:
   > ```xml
   > <ProjectReference Include="..\..\Connectors-NET-SDK\src\Microsoft.Azure.Connectors.Sdk\Microsoft.Azure.Connectors.Sdk.csproj" />
   > ```

4. **Build and deploy**:

   ```shell
   azd up
   ```

### Packages

- `Microsoft.Azure.Functions.Worker.Extensions.Connector` — provides the `[ConnectorTrigger]` attribute and POCO converter. See [azure-functions-connector-extension](https://github.com/Azure/azure-functions-connector-extension).
- `Microsoft.Azure.Connectors.Sdk` — typed connector clients and trigger payload types (e.g., `Office365OnNewEmailTriggerPayload`, `TeamsClient`)

### Example: ConnectorTrigger Function

Use the `[ConnectorTrigger]` attribute with SDK typed payloads for POCO binding. Payload types are in the `Microsoft.Azure.Connectors.DirectClient.<Connector>` namespace:

```csharp
using Microsoft.Azure.Connectors.DirectClient.Office365;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class EmailTrigger(ILogger<EmailTrigger> logger)
{
    [Function("OnNewEmailReceived")]
    public void OnNewEmailReceived(
        [ConnectorTrigger] Office365OnNewEmailTriggerPayload payload)
    {
        var emails = payload.Body?.Value ?? [];
        foreach (var email in emails)
        {
            logger.LogInformation("Subject: {Subject}, From: {From}", email.Subject, email.From);
        }
    }
}
```

The connector extension registers a webhook endpoint on the Function App at:

```
POST /runtime/webhooks/connector?functionName={FunctionName}&code={connector_extension_key}
```

The `connector_extension` system key is auto-generated when the extension loads. Use this URL as the callback when creating trigger configs (see Step 1 below).

## Procedure

### Step 1: Get the Callback URL

The connector extension exposes a webhook endpoint on the Function App. Build the callback URL using the `connector_extension` system key:

```powershell
$resourceGroup = "<resource-group>"
$functionAppName = "<function-app-name>"
$functionName = "<connector-trigger-function-name>"

$connectorExtensionKey = az functionapp keys list -g $resourceGroup -n $functionAppName --query "systemKeys.connector_extension" -o tsv
$callbackUrl = "https://$functionAppName.azurewebsites.net/runtime/webhooks/connector?functionName=$functionName&code=$connectorExtensionKey"
```

> **Important:** The `functionName` query parameter must exactly match the `[Function("...")]` attribute name in your code. A mismatch means the connector extension cannot route the callback to your function.

### Step 2: Get Trigger Parameters

Each trigger operation requires specific parameters. Find them in the SDK's generated `*TriggerParameters` class:

```csharp
// Example: OneDrive OnNewFileV2 parameters
OnedriveforbusinessTriggerParameters.OnNewFile.FolderId      // Required — folder to watch
OnedriveforbusinessTriggerParameters.OnNewFile.IncludeSubfolders  // Optional, default: false
OnedriveforbusinessTriggerParameters.OnNewFile.InferContentType   // Optional, default: true
```

For folder IDs, use the connector's list operations to discover them:

> **Note:** Listing folders requires a data-plane call to the connection runtime URL. If you skipped access policies in the `connection-setup` skill (trigger-only flow), you must first add a local-dev access policy (`connection-setup` Step 5) to avoid 403 errors.

```powershell
$runtimeUrl = "<connection-runtime-url>"  # from connection-setup skill Step 4
az rest --method GET `
    --uri "$runtimeUrl/datasets/default/folders" `
    --resource "https://apihub.azure.com" -o json
```

### Step 3: Create Trigger Config

```powershell
$subscriptionId = "<subscription-id>"
$resourceGroup = "<resource-group>"
$gatewayName = "<gateway-name>"
$gwId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/connectorGateways/$gatewayName"

$triggerName = "<trigger-config-name>"   # e.g., "onedrive-newfile-binary"
$connectionName = "<connection-name>"    # e.g., "onedrive-test"
$connectorName = "<connector-name>"      # e.g., "onedriveforbusiness"
$operationName = "<operation-name>"      # e.g., "OnNewFileV2"
```

Build and send the PUT request. **Use `Invoke-WebRequest`** (not `az rest`) because `az rest` silently swallows error responses from this API:

```powershell
$token = az account get-access-token `
    --resource "https://management.core.windows.net/" `
    --query "accessToken" -o tsv

$body = @{
    properties = @{
        operationName = $operationName
        connectionDetails = @{
            connectorName = $connectorName
            connectionName = $connectionName
        }
        notificationDetails = @{
            callbackUrl = $callbackUrl
            httpMethod = "Post"
        }
        parameters = @(
            @{ name = "folderId"; value = "<folder-id-from-step-2>" }
            @{ name = "includeSubfolders"; value = "false" }
        )
    }
} | ConvertTo-Json -Depth 4

$uri = "https://management.azure.com${gwId}/triggerConfigs/${triggerName}?api-version=2026-05-01-preview"
try {
    $response = Invoke-WebRequest -Uri $uri -Method PUT -Body $body `
        -ContentType "application/json" `
        -Headers @{ Authorization = "Bearer $token" }
    Write-Output "Status: $($response.StatusCode)"
} catch {
    Write-Output "Error: $($_.Exception.Response.StatusCode) $($_.Exception.Response.ReasonPhrase)"
    $_.ErrorDetails.Message
}
```

#### Alternative: Using az rest

```powershell
$bodyFile = [System.IO.Path]::GetTempFileName()
$body | Out-File -FilePath $bodyFile -Encoding utf8

az rest --method PUT --url $uri --body "@$bodyFile" --headers "Content-Type=application/json"
Remove-Item $bodyFile -ErrorAction SilentlyContinue
```

Expected: HTTP 201 Created.

### Step 4: Verify Trigger Config

```powershell
az rest --method GET `
    --uri "https://management.azure.com${gwId}/triggerConfigs/${triggerName}?api-version=2026-05-01-preview" `
    --query "properties.{operation:operationName, state:state, hasCallback:notificationDetails.callbackUrl!=null}" `
    -o table
```

Expected: `state = Enabled`, `hasCallback = True`.

### Step 5: List All Trigger Configs

```powershell
az rest --method GET `
    --uri "https://management.azure.com${gwId}/triggerConfigs?api-version=2026-05-01-preview" `
    --query "value[].{name:name, operation:properties.operationName, state:properties.state}" `
    -o table
```

### Step 6: Fire the Trigger

Upload or create content in the watched location to trigger the callback:

```powershell
# Example: upload a file to OneDrive to fire OnNewFileV2
# Set $baseUrl to your deployed Function App, e.g.:
# $baseUrl = "https://<function-app-name>.azurewebsites.net/api"
$uploadBody = '{"folderPath":"/Documents","fileName":"trigger-test.txt","content":"Hello from trigger test"}'
Invoke-RestMethod -Uri "$baseUrl/onedrive/upload?code=$functionKey" `
    -Method POST -Body $uploadBody -ContentType "application/json"
```

The Connector Gateway polls the connector every 1-5 minutes. After polling detects the new content, it POSTs the trigger payload to your callback URL.

### Step 7: Verify Callback Received

Check function app logs:

```powershell
az webapp log download -g $resourceGroup -n $functionAppName --log-file "$env:TEMP/func-logs.zip"
Expand-Archive -Path "$env:TEMP/func-logs.zip" -DestinationPath "$env:TEMP/func-logs" -Force
Get-ChildItem "$env:TEMP/func-logs" -Recurse -Filter "*.log" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 3 |
    ForEach-Object { Select-String -Path $_.FullName -Pattern "TriggerCallback" }
```

## API Schema Reference

### TriggerConfig PUT Body

```json
{
  "properties": {
    "operationName": "OnNewFileV2",
    "connectionDetails": {
      "connectorName": "onedriveforbusiness",
      "connectionName": "onedrive-test"
    },
    "notificationDetails": {
      "callbackUrl": "https://my-func.azurewebsites.net/runtime/webhooks/connector?functionName=OnNewFile&code=...",
      "httpMethod": "Post"
    },
    "parameters": [
      { "name": "folderId", "value": "<folder-id>" },
      { "name": "includeSubfolders", "value": "false" }
    ]
  }
}
```

### Property Names (Validated)

| Property | Notes |
|----------|-------|
| `properties.connectionDetails` | **Not** `connectionName` at top level |
| `properties.connectionDetails.connectorName` | Required — the API connector name |
| `properties.connectionDetails.connectionName` | Required — the connection resource name |
| `properties.notificationDetails.callbackUrl` | **Missing = trigger has no target** — trigger provisions but never calls back |
| `properties.notificationDetails.httpMethod` | `"Post"` |
| `parameters[].name` | **Not** `parameterName` |
| `parameters[].value` | String value |

### Common Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `Could not find member 'connectionName'` | Used `connectionName` instead of `connectionDetails` | Use `connectionDetails` with nested `connectorName` + `connectionName` |
| `Could not find member 'callbackUrl'` | Put `callbackUrl` at properties level | Wrap in `notificationDetails` |
| `Could not find member 'parameterName'` | Used `parameterName` in parameter array | Use `name` |
| `Cannot deserialize... into ConnectorGatewayOperationsParameter[]` | Parameters as object, not array | Use `[{"name":"...","value":"..."}]` array |
| `missing required property 'folderId'` | Required trigger parameter not provided | Add to parameters array |
| Trigger provisions but never fires callback | Missing `notificationDetails` or empty `notificationDetails.callbackUrl` | Add `notificationDetails` with a non-empty `callbackUrl` and `httpMethod` |
| `az rest` PUT returns no output/error | `az rest` swallows non-2xx responses silently | Use `Invoke-WebRequest` instead for PUT operations |

## Handling Trigger Payloads in Code

### Binary Content Triggers (e.g., OnNewFileV2)

```csharp
using var document = JsonDocument.Parse(body);
var root = document.RootElement;

if (root.TryGetProperty("body", out var bodyElement) &&
    bodyElement.ValueKind == JsonValueKind.String)
{
    // Binary trigger — body is base64-encoded file content.
    // NOTE: The base64 string may be wrapped in extra quotes
    // from the Logic Apps expression engine. Strip them.
    var base64Content = bodyElement.GetString()?.Trim('"') ?? string.Empty;

    if (!string.IsNullOrWhiteSpace(base64Content))
    {
        var maximumDecodedLength = (base64Content.Length / 4) * 3;
        var fileBytesBuffer = new byte[maximumDecodedLength];

        if (Convert.TryFromBase64String(base64Content, fileBytesBuffer, out var bytesWritten))
        {
            var fileBytes = fileBytesBuffer.AsSpan(0, bytesWritten).ToArray();

            // fileBytes contains the actual file content
        }
        else
        {
            // Invalid base64 payload — handle gracefully
        }
    }
}
```

### Metadata Triggers (e.g., OnNewFilesV2)

```csharp
// Callback JSON shape: {"body":{"value":[{...BlobMetadata...}]}}
// Deserialize using the SDK's typed trigger payload class.
var payload = JsonSerializer.Deserialize<OnedriveforbusinessOnNewFilesTriggerPayload>(
    body, jsonOptions);

var files = payload?.Body?.Value;
```

### Detecting the Variant at Runtime

```csharp
using var document = JsonDocument.Parse(body);
if (document.RootElement.TryGetProperty("body", out var bodyElement))
{
    if (bodyElement.ValueKind == JsonValueKind.String)
    {
        // Binary content trigger (OnNewFileV2, OnUpdatedFileV2)
    }
    else if (bodyElement.ValueKind == JsonValueKind.Object)
    {
        // Metadata trigger (OnNewFilesV2, OnUpdatedFilesV2)
    }
}
```
