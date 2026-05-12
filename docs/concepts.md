# Concepts & Terminology

This page explains the key concepts, components, and terminology used across the Azure Connectors SDK ecosystem.

## Big Picture

```text
 ┌──────────────────────────────────────────────────────────────────────┐
 │                        YOUR APPLICATION                             │
 │                   (Azure Functions, App Service, …)                 │
 │                                                                     │
 │  var client = new Office365Client(connectionRuntimeUrl);            │
 │  await client.SendEmailAsync(…);                                    │
 └─────────────────────────┬────────────────────────────────────────────┘
                           │  NuGet reference
                           ▼
 ┌──────────────────────────────────────────────────────────────────────┐
 │               CONNECTORS .NET SDK  (client library)                 │
 │                                                                     │
 │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
 │  │ Generated Client │  │ ConnectorHttp-   │  │ TokenCredential  │   │
 │  │ Office365Client  │  │ Client (retry,   │  │ (Azure.Core /    │   │
 │  │ TeamsClient      │  │  auth headers)   │  │  Azure.Identity)  │   │
 │  │ SharePointClient │  │                  │  │                  │   │
 │  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘   │
 │           └──────────────┬──────┘                      │            │
 │                          │ Bearer token                │            │
 └──────────────────────────┼─────────────────────────────┘            │
                            │ HTTPS (connection runtime URL)           │
                            ▼                                          │
 ┌──────────────────────────────────────────────────────────────────────┘
 │          AZURE  (ARM Resource Provider + Runtime)
 │
 │  ┌─────────────────────────────────────────────────────────────┐
 │  │          Connector Namespaces RP  (Microsoft.Web)            │
 │  │                                                             │
 │  │  ┌─────────────────────────────────────────────────────┐    │
 │  │  │        Connector Namespace  (ARM resource)            │    │
 │  │  │        /connectorGateways/{gatewayName}             │    │
 │  │  │                                                     │    │
 │  │  │  ┌───────────────┐  ┌───────────────┐               │    │
 │  │  │  │  Connection   │  │  Connection   │  …            │    │
 │  │  │  │  (office365)  │  │  (teams)      │               │    │
 │  │  │  │               │  │               │               │    │
 │  │  │  │ • Auth state  │  │ • Auth state  │               │    │
 │  │  │  │ • Runtime URL │  │ • Runtime URL │               │    │
 │  │  │  │ • Triggers[]  │  │ • Access      │               │    │
 │  │  │  │ • Access      │  │   Policies[]  │               │    │
 │  │  │  │   Policies[]  │  │               │               │    │
 │  │  │  └───────┬───────┘  └───────┬───────┘               │    │
 │  │  └──────────┼──────────────────┼───────────────────────┘    │
 │  └─────────────┼──────────────────┼───────────────────────────┘
 │                │                  │
 │                ▼                  ▼
 │  ┌─────────────────────────────────────────────────────────────┐
 │  │             Connectors  (shared REST services)             │
 │  │                                                             │
 │  │    Office 365 API    Teams API    SharePoint API   …       │
 │  │    (send email,      (post msg,   (list files,             │
 │  │     calendar,         channels)    create item)            │
 │  │     contacts)                                              │
 │  └─────────────────────────────────────────────────────────────┘
```

---

## Glossary

### Connector

A **connector** is a shared REST service hosted by Azure that wraps a specific SaaS or PaaS product's API (Office 365, SharePoint, Teams, Dataverse, etc.). Each connector exposes a set of **actions** (request/response operations like "Send Email") and, optionally, **triggers** (event-driven callbacks like "When a new email arrives").

Connectors were originally built for Azure Logic Apps and Power Automate. The SDK makes them callable from any .NET application.

```text
Connector (e.g. "office365")
  ├── Actions
  │     ├── SendEmailV2
  │     ├── GetOutlookCategories
  │     └── ExportEmail
  └── Triggers
        └── OnNewEmail  (polling trigger)
```

> **Connector vs. API:** A connector is not the same as the underlying SaaS API. The connector is a managed proxy layer that normalizes authentication, pagination, throttling, and schema across hundreds of backend services.

### Connector Namespaces (ARM Resource Type)

**Connector Namespaces** refers to the Azure Resource Manager (ARM) resource type `connectorGateways` under the `Microsoft.Web` resource provider. This resource type manages the lifecycle of gateways, connections, access policies, and trigger registrations. It is the control plane that you interact with via the Azure CLI, ARM templates, or the Azure Portal.

ARM namespace: `Microsoft.Web`

Key API operations:

| Operation | ARM Path |
|-----------|----------|
| List gateways | `GET .../connectorGateways` |
| Create/update gateway | `PUT .../connectorGateways/{name}` |
| Create connection | `PUT .../connectorGateways/{name}/connections/{name}` |
| Add access policy | `PUT .../connections/{name}/accessPolicies/{name}` |
| Register trigger | `PUT .../connections/{name}/triggerConfigs/{name}` |

### Connector Namespace (ARM Resource)

A **Connector Namespace** is a specific ARM resource (`Microsoft.Web/connectorGateways/{gatewayName}`) deployed in a resource group. It acts as a container for one or more **connections** and provides:

- **Managed identity** — A system-assigned identity used by the gateway to authenticate trigger callbacks to your compute host.
- **Regional endpoint** — The gateway must be located in a [supported region](connection-setup.md).
- **Connection grouping** — Multiple connections for different connectors can live under one gateway.

```text
Resource Group: "my-rg"
 └── Connector Namespace: "my-gateway"  (Microsoft.Web/connectorGateways)
       ├── Connection: "office365-conn"
       ├── Connection: "teams-conn"
       └── Connection: "sharepoint-conn"
```

### Connection

A **connection** is a child resource of a Connector Namespace (`Microsoft.Web/connectorGateways/{gateway}/connections/{name}`). It represents an authenticated session to a specific connector and contains:

| Property | Description |
|----------|-------------|
| **Connector name** | Which connector this connection targets (e.g. `office365`) |
| **Authentication state** | OAuth token/consent status (`Connected`, `Error`, `Unauthenticated`) |
| **Connection runtime URL** | The HTTPS endpoint your code calls to invoke connector operations |
| **Access policies** | Which Azure AD identities (users, managed identities) can use this connection |
| **Trigger registrations** | Optional polling trigger configurations attached to this connection |

A connection starts in an **unauthenticated** state. You complete OAuth consent (via browser or the Connector Namespace Manager Portal) to move it to `Connected`.

```text
Connection lifecycle:

  PUT (create)          OAuth consent          Ready to use
  ─────────────►  Error  ────────────►  Connected  ────────►  API calls
                  (no auth)              (token stored)        via runtime URL
```

> **Note:** There are also standalone ARM connections (`Microsoft.Web/connections`), called **API Connection** in the Azure Portal. These can be created through Logic Apps (any SKU). They support actions but not Connector Namespace triggers.

### Connection Runtime URL

The **connection runtime URL** is the HTTPS endpoint exposed by a connection. All connector operations flow through this URL. It encodes the connection identity, region, and routing information.

Format:

```text
https://{instance}.{region}.common.logic-{env}.azure-apihub.net/apim/{connector}/{connection-id}
```

Your application code passes this URL to the SDK client constructor:

```csharp
using var client = new Office365Client(connectionRuntimeUrl);
```

### Access Policy

An **access policy** grants an Azure AD identity permission to call a connection's runtime URL. Without an access policy, API calls return `403 Forbidden`.

Typical identities:

- **Local development** — Your Azure CLI user identity
- **Deployed app** — The Azure Function or App Service managed identity

> Access policies apply to **actions** (outbound API calls your code makes). Trigger callbacks flow from the Connector Namespace to your compute host and do not require access policies on the connection.

### Trigger Registration (Trigger Config)

A **trigger registration** (or **trigger config**) is a child resource of a connection that tells the Connector Namespace to poll the connector for new events and deliver them to your compute host.

```text
Connection: "office365-conn"
 └── TriggerConfig: "on-new-email"
       ├── Trigger operation: "OnNewEmail"
       ├── Callback URL: https://my-func.azurewebsites.net/api/trigger
       └── Parameters: { folderId: "Inbox" }
```

The Connector Namespace polls the trigger endpoint on a schedule. When new items are found, it POSTs the payload to your callback URL using the gateway's managed identity for authentication.

### Trigger Callback Payload

The **trigger callback payload** is the JSON envelope the Connector Namespace sends to your function when a trigger fires. The SDK provides `TriggerCallbackPayload<T>` for deserialization:

```text
{
  "body": {
    "value": [         ◄── Array of items (e.g., multiple new emails)
      { item1 },
      { item2 }
    ]
  }
}
```

Your Azure Function receives this as an HTTP POST body.

---

## SDK Components

### Connectors .NET SDK (NuGet Package)

The **Azure Connectors .NET SDK** (`Azure.Connectors.Sdk`) is a client-side .NET library that provides the infrastructure for calling connectors:

| Component | Responsibility |
|-----------|---------------|
| **ConnectorClientBase** | Abstract base class all generated clients inherit from |
| **ConnectorHttpClient** | HTTP client with Azure.Core `HttpPipeline` for retry, authentication (when pipeline includes `BearerTokenAuthenticationPolicy`), and diagnostics |
| **TokenCredential** | Azure.Core authentication (DefaultAzureCredential, ManagedIdentityCredential, etc.) |
| **ConnectorJsonSerializer** | JSON serialization with connector conventions |
| **ConnectorConnectionResolver** | Resolves connection settings from Azure Functions app configuration |
| **TriggerCallbackPayload\<T\>** | Deserializer for trigger callback envelopes |

### Generated Connector Clients

**Generated clients** are typed C# classes produced by the `CodefulSdkGenerator` from connector Swagger definitions. Each client:

- Inherits from `ConnectorClientBase`
- Exposes one async method per connector action (e.g., `SendEmailAsync`, `GetChannelsAsync`)
- Includes strongly typed input/output models with JSON serialization attributes
- Provides XML documentation from connector metadata

```text
Swagger definition (ARM)
         │
         ▼
┌────────────────────────┐
│  CodefulSdkGenerator   │     (build-time, from BPM repo)
│  (LogicAppsCompiler)   │
└────────┬───────────────┘
         │ generates
         ▼
┌────────────────────────┐
│ Office365Extensions.cs │     (checked into SDK repo)
│  ├── Office365Client   │
│  ├── SendEmailInput    │
│  ├── CalendarEvent     │
│  └── …                 │
└────────────────────────┘
```

See [GENERATION.md](../GENERATION.md) for generation instructions.

### Authentication (Azure.Core TokenCredential)

The SDK authenticates using `Azure.Core.TokenCredential`, the standard Azure SDK authentication abstraction. Any credential from `Azure.Identity` works:

| Credential | Use Case |
|----------|----------|
| `ManagedIdentityCredential` | Azure-hosted apps — system-assigned by default when no credential is specified |
| `AzureCliCredential` | Local development — pass explicitly (default no longer probes CLI) |
| `ClientSecretCredential` | Service principal authentication |
| Any `TokenCredential` | Any authentication scheme supported by Azure.Identity |

```text
  Your App
    │
    ▼
 Office365Client
    │
    ▼
 ConnectorClientBase (builds HttpPipeline with auth policy)
    │                          │
    │                          ▼
    │                   BearerTokenAuthenticationPolicy
    │                     calls TokenCredential.GetTokenAsync()
    │                     ├── ManagedIdentityCredential (default)
    │                     └── AzureCliCredential (for local dev)
    │
    ▼
 HTTP request with "Authorization: Bearer {token}"
    │
    ▼
 Connection Runtime URL
```

---

## Request Flow (End to End)

Here is the complete path of an SDK call from your code to the SaaS service and back:

```text
  ┌──────────────────────────────────────────────────────┐
  │  1. Your code calls a typed method                   │
  │     await client.SendEmailAsync(input);              │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  2. Generated client builds HTTP request             │
  │     POST {connectionRuntimeUrl}/SendMailV2           │
  │     Body: serialized SendEmailInput                  │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  3. HttpPipeline acquires Bearer token via           │
  │     BearerTokenAuthenticationPolicy + TokenCredential │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  4. HTTP request sent to connection runtime URL      │
  │     (with retry policy for transient failures)       │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  5. API Hub routes request to connector service      │
  │     using stored OAuth token from the connection     │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  6. Connector calls the underlying SaaS API          │
  │     (e.g., Microsoft Graph for Office 365)           │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  7. Response flows back: SaaS → Connector → API Hub  │
  │     → ConnectorHttpClient → deserialized response    │
  └──────────────────────────────────────────────────────┘
```

---

## Trigger Flow (Event-Driven)

Triggers follow a different pattern — the Connector Namespace polls for events server-side and pushes results to your compute host:

```text
  ┌──────────────────────────────────────────────────────┐
  │  1. You register a trigger config on the connection  │
  │     (operation, callback URL, parameters)            │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  2. Connector Namespace polls the connector trigger    │
  │     endpoint on a schedule (server-side)             │
  └──────────────────────┬───────────────────────────────┘
                         │ new items found
  ┌──────────────────────▼───────────────────────────────┐
  │  3. Gateway POSTs TriggerCallbackPayload<T> to your  │
  │     callback URL (authenticated with gateway MSI)    │
  └──────────────────────┬───────────────────────────────┘
                         │
  ┌──────────────────────▼───────────────────────────────┐
  │  4. Your Azure Function receives the HTTP POST       │
  │     and deserializes the payload using the SDK       │
  └──────────────────────────────────────────────────────┘
```

---

## Connection Configuration

The SDK resolves connection settings from Azure Functions app configuration using two formats:

### Format A — Connector Namespace (triggers + actions)

```json
{
  "office365__connectorGatewayName": "my-gateway",
  "office365__connectionName": "office365-conn"
}
```

The SDK resolves the runtime URL at startup by querying the Connector Namespace ARM API.

### Format B — Direct URL (actions only)

```json
{
  "office365__connectionRuntimeUrl": "https://..."
}
```

You provide the runtime URL directly (obtained from the Azure Portal or CLI).

> `__` (double underscore) is the Azure Functions convention for nested configuration. The prefix (e.g., `office365`) is the connection setting name passed to `ConnectorConnectionResolver.Resolve()`.

---

## See Also

- [Connection Setup Guide](connection-setup.md) — Step-by-step connection creation and authorization
- [Code Generation Guide](../GENERATION.md) — Generating typed connector clients from Swagger
- [Connector SDK Samples](https://github.com/Azure/Connectors-NET-Samples) — Working Azure Functions examples
- [ROADMAP.md](../ROADMAP.md) — Connector generation progress
