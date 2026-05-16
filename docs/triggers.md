# Connector Triggers

This page documents the trigger architecture for the Azure Connectors SDK — how connector triggers are delivered to Azure Functions, how to consume typed trigger payloads, and the annotation pattern used to distinguish binary from metadata triggers.

> **Status:** Phase 5 (Active Design → Implementation Planning). See [ROADMAP.md](../ROADMAP.md) for full planning status.

## Architecture Overview: Webhook Model (Event Grid Analogy)

Connector triggers follow the **Event Grid webhook pattern**. The connector infrastructure handles all event monitoring and delivers events to Functions via HTTP push:

```text
┌─────────────────────────────────────────────────────────────────────┐
│                  CONNECTOR INFRASTRUCTURE                           │
│            (Logic Apps multi-tenant, Connector Namespace)           │
│                                                                     │
│  Polls/subscribes to external service (Office 365, SharePoint, …)  │
│  Detects event (new email, file change, calendar update, …)         │
└──────────────────────────────┬──────────────────────────────────────┘
                               │  HTTP POST (callback push)
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     AZURE FUNCTIONS                                  │
│                                                                     │
│  [ConnectorTrigger("office365", "OnNewEmailV3")]                     │
│  public async Task Run(TriggerCallbackPayload<GraphClientReceiveMessage> payload) │
└─────────────────────────────────────────────────────────────────────┘
```

### Key properties of this model

1. **Function app registers** a callback URL with the connector service at deployment time (cloud) or F5 startup (local dev).
2. **Connector service monitors** for events on its own compute — Azure Functions never polls.
3. **Events push to the function** via HTTP callback. Functions scales on HTTP push naturally; no scale controller changes are needed.
4. **Webhook lifecycle is connector-owned.** The connector infrastructure handles webhook expiry and automatic re-registration. The Functions extension does not manage webhook renewal.

### Local development (F5 experience)

Webhook triggers require a publicly reachable URL. Local development requires:

1. **Dev tunnels or ngrok** to expose a local port as a public endpoint.
2. **Webhook registration** at F5 startup pointing to the tunnel URL.
3. **Separate dev/prod connector connections** configured via `local.settings.json` (dev) vs app settings (prod).

### Connector Resource Access

Connections and trigger registration use the **Connector Namespace API** (`Microsoft.Web/connectorGateways`, version `2026-05-01-preview`), which exposes connections and triggers as data-plane resources without requiring Logic Apps.

## Trigger Payload Types

### `TriggerCallbackPayload<T>` and `TriggerCallbackBody<T>`

All connector trigger callbacks are deserialized using the `TriggerCallbackPayload<T>` envelope type:

```csharp
// Defined in Azure.Connectors.Sdk namespace
public class TriggerCallbackPayload<T>
{
    [JsonPropertyName("body")]
    public TriggerCallbackBody<T>? Body { get; init; }
}

public class TriggerCallbackBody<T>
{
    // Always a list — normalized from both batch and single-item shapes
    [JsonPropertyName("value")]
    public IReadOnlyList<T>? Value { get; internal set; }
}
```

#### Dual-shape normalization (v0.11.0+)

The Connector Namespace delivers callbacks in two shapes depending on the trigger's `splitOn` setting:

| Shape | JSON | When used |
|-------|------|-----------|
| **Batch** | `{"body":{"value":[...items...]}}` | `splitOn` disabled — multiple items per call |
| **Single-item** | `{"body":{...item...}}` | `splitOn` enabled — one item per call |

Both shapes are transparently normalized to `Body.Value` as an `IReadOnlyList<T>` by `TriggerCallbackBodyConverterFactory`. Consumer code always iterates `payload.Body?.Value` regardless of which shape the connector delivers.

```csharp
// Works for both batch and single-item callbacks
foreach (var email in payload.Body?.Value ?? [])
{
    Console.WriteLine(email.Subject);
}
```

### Typed payload subclasses

Each connector provides typed subclasses for its trigger operations:

```csharp
// Office 365 — each trigger has its own typed payload class
public class Office365OnNewEmailTriggerPayload : TriggerCallbackPayload<GraphClientReceiveMessage> { }
public class Office365OnCalendarChangedItemsTriggerPayload : TriggerCallbackPayload<GraphCalendarEventClientReceive> { }

// OneDrive for Business
public class OneDriveForBusinessOnNewFilesTriggerPayload : TriggerCallbackPayload<BlobMetadata> { }
```

Use the typed class directly to deserialize callbacks:

```csharp
var payload = JsonSerializer.Deserialize<Office365OnNewEmailTriggerPayload>(callbackJson);
```

## Trigger Operation Constants

Each connector exposes a `{Connector}TriggerOperations` static class with the operation name strings:

```csharp
// Office 365
Office365TriggerOperations.OnNewEmail          // "OnNewEmailV3"
Office365TriggerOperations.OnCalendarNewItems  // "CalendarGetOnNewItemsV3"

// OneDrive for Business
OneDriveForBusinessTriggerOperations.OnNewFiles    // "OnNewFilesV2"
OneDriveForBusinessTriggerOperations.OnNewFile     // "OnNewFileV2"  (binary — see below)
```

## Binary vs. Metadata Trigger Annotation Pattern

Not all trigger operations deliver JSON-deserializable payloads. Some triggers return **raw binary content** (e.g., file download triggers that return file bytes). These binary triggers have **no typed payload class** and cannot be deserialized via `TriggerCallbackPayload<T>`.

### How to distinguish them

The XML doc on a trigger operation constant indicates whether a typed payload is available:

**Metadata trigger** — has `Payload type:` annotation:
```csharp
/// <summary>
/// When a file is created (properties only).
/// Payload type: <see cref="OneDriveForBusinessOnNewFilesTriggerPayload"/>.
/// </summary>
public const string OnNewFiles = "OnNewFilesV2";
```

**Binary trigger** — no `Payload type:` annotation:
```csharp
/// <summary>
/// When a file is created.
/// </summary>
public const string OnNewFile = "OnNewFileV2";
```

### Why binary triggers exist

For large files (SharePoint documents, OneDrive files, Outlook attachments), the trigger delivers **metadata only** — the file path, size, and modification time. User code then calls the appropriate action client to fetch the actual file content:

```csharp
// Event-notification model: trigger delivers metadata, action fetches content
var filePath = triggerPayload.Body?.Value?[0]?.Path;
var fileContent = await oneDriveClient.GetFileContentAsync(file: filePath, ...);
```

This avoids large binary payloads flowing through the Functions runtime and allows selective content fetching.

### `{Connector}Triggers` typed registry

A separate `{Connector}Triggers.Operations` dictionary maps operation names to their typed payload types (metadata triggers only):

```csharp
// OneDriveForBusinessTriggers.Operations contains:
// "OnNewFilesV2"   → typeof(OneDriveForBusinessOnNewFilesTriggerPayload)
// "OnUpdatedFilesV2" → typeof(OneDriveForBusinessOnUpdatedFilesTriggerPayload)
//
// OnNewFileV2 and OnUpdatedFileV2 are NOT in the registry — they are binary triggers
```

## Team Roles

| Team | Responsibility |
|------|----------------|
| **Connectors team** | SDK typed trigger data types (payload classes, operation constants). Connector-side trigger notification delivery. |
| **Functions team** | Functions extension(s) for trigger bindings — `[ConnectorTrigger]` attribute, strongly-typed and generic approaches, all supported languages (C#, Python, Node.js, Java). |

## Open Questions

| Question | Status |
|----------|--------|
| **VNet-locked Function Apps** — pure webhooks break with Private Endpoints / IP restrictions. Need pull delivery or polling fallback? | Open — Event Grid hybrid model (webhook + pull) proposed. |
| **Local F5 tunnel setup** — How to auto-configure dev tunnels for webhook registration across all languages? | Open — Functions team improving local CLI tooling. |
| **RBAC roles** — What Azure role(s) does the Function App identity need for `Microsoft.Web/connections`? | Open — minimum RBAC not yet documented. |
| **Multi-language trigger data types** — Python, Java, Node.js equivalents for typed trigger payload models. | Open — strategy for non-C# type generation TBD. |

## Known Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Webhook subscribe 404** — `ApiConnectionNotification` triggers return 404 in LA Standard (found in M365 Agent POC). | Webhook model may not work for all connectors. | Validate from Functions extension path. Design polling fallback if connector-level issue. |
| **Teams connector gaps** — LA Standard APIM gateway restricts Graph API endpoints; chat/DM paths return 404. | Teams trigger may be limited to channel-level events. | Prioritize Office 365 email trigger (confirmed working) for Phase 5a POC. |
| **Catch-up storm on polling fallback** — queued events process all at once after connector downtime. | Relevant only if polling fallback is needed. | Batch-size limits and backpressure handling in the Functions extension. |
| **VNet inbound restrictions** — webhook-only model fails for locked-down Function Apps. | Enterprise customers with Private Endpoints cannot receive callbacks. | Design polling fallback similar to Event Grid pull delivery model. |

## Priority Triggers (90-Day Execution Volume)

| Connector | Trigger Type | 90-Day Executions | Status |
|-----------|--------------|-------------------|--------|
| Office 365 | Email arrival | 25.7B | Design |
| SharePoint Online | File/list changes | 22.9B | Design |
| Service Bus | Message trigger | 10.3B | Evaluate overlap with native Functions binding |
| OneDrive for Business | File trigger | 5.3B | Design |
| SQL | Row trigger | 2.6B | Design |

Office 365 email and Teams are the most popular SaaS connectors for triggers; Service Bus for PaaS.
