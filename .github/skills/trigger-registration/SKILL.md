---
name: trigger-registration
description: 'Register AI Gateway trigger configs for SDK-supported connectors. USE WHEN: setting up polling triggers (e.g., OnNewEmail, OnNewFile, OnUpdatedFile) that call back to an Azure Function when events occur. Covers trigger config creation, callback URL wiring, parameter discovery, and binary vs metadata payload handling. NOT FOR: connection setup (use connection-setup skill), general SDK usage, or code generation.'
---

# AI Gateway Trigger Registration

Registers polling trigger configs on an AI Gateway so that connector events (new email, new file, etc.) call back to your Azure Function endpoint.

## When to Use

- Developer needs a connector trigger (e.g., "when a new file is created in OneDrive")
- Developer has an existing AI Gateway connection (use the `connection-setup` skill first if not)
- Developer needs to wire up the callback URL from a deployed Azure Function
- Developer needs to understand binary vs metadata trigger payload shapes

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- AI Gateway with a connected connector (see `connection-setup` skill)
- Deployed Azure Function with an HTTP-triggered callback endpoint
- Function key for the callback endpoint

## Key Concepts

### Trigger Config vs Connection

Connections (managed by the `connection-setup` skill) authenticate your app to the connector API. Trigger configs tell the AI Gateway to **poll** the connector for events and **POST callbacks** to your function when events occur.

```text
AI Gateway
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

## Procedure

### Step 1: Get the Callback URL

Build the callback URL from your deployed Function App:

```powershell
$resourceGroup = "<resource-group>"
$functionAppName = "<function-app-name>"
$functionName = "<trigger-callback-function-name>"

$keys = az functionapp function keys list -g $resourceGroup -n $functionAppName --function-name $functionName -o json | ConvertFrom-Json
$functionKey = $keys.default
$callbackUrl = "https://$functionAppName.azurewebsites.net/api/$functionName?code=$functionKey"
```

### Step 2: Get Trigger Parameters

Each trigger operation requires specific parameters. Find them in the SDK's generated `*TriggerParameters` class:

```csharp
// Example: OneDrive OnNewFileV2 parameters
OnedriveforbusinessTriggerParameters.OnNewFile.FolderId      // Required — folder to watch
OnedriveforbusinessTriggerParameters.OnNewFile.IncludeSubfolders  // Optional, default: false
OnedriveforbusinessTriggerParameters.OnNewFile.InferContentType   // Optional, default: true
```

For folder IDs, use the connector's list operations to discover them:

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
$gwId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/aigateways/$gatewayName"

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

$uri = "https://management.azure.com${gwId}/triggerConfigs/${triggerName}?api-version=2026-03-01-preview"
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

Expected: HTTP 201 Created.

### Step 4: Verify Trigger Config

```powershell
az rest --method GET `
    --uri "https://management.azure.com${gwId}/triggerConfigs/${triggerName}?api-version=2026-03-01-preview" `
    --query "properties.{operation:operationName, state:state, hasCallback:notificationDetails.callbackUrl!=null}" `
    -o table
```

Expected: `state = Enabled`, `hasCallback = True`.

### Step 5: List All Trigger Configs

```powershell
az rest --method GET `
    --uri "https://management.azure.com${gwId}/triggerConfigs?api-version=2026-03-01-preview" `
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

The AI Gateway polls the connector every 1-5 minutes. After polling detects the new content, it POSTs the trigger payload to your callback URL.

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
      "callbackUrl": "https://my-func.azurewebsites.net/api/callback?code=...",
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
| `Cannot deserialize... into AIGatewayOperationsParameter[]` | Parameters as object, not array | Use `[{"name":"...","value":"..."}]` array |
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
