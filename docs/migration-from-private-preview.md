# Migrating from Azure Connectors Private Preview

> This guide is for developers who used the original `Azure/Connectors` private preview
> (circa 2021–2022). It explains what changed, what was renamed, and how to update your code.
>
> The private `Azure/Connectors` repo is no longer the active development surface.
> All SDK work has moved to the public repos listed in [What Changed at a Glance](#what-changed-at-a-glance).

---

## What Changed at a Glance

| Old (Private Preview) | New (Current SDK) |
|-----------------------|-------------------|
| "Azure API Connections" | "Connector Gateway" |
| VS Code extension creates connection → copies connection string | `az rest` commands create Connector Gateway + connection → OAuth consent → runtime URL |
| `MicrosoftTeamsConnector.Create("<connectionKey>")` | `new TeamsClient(new Uri(connectionRuntimeUrl))` |
| `createMicrosoftTeamsConnector("<connectionKey>")` | `new TeamsClient(connectionRuntimeUrl, tokenProvider)` |
| Per-connector NuGet packages (`Azure.Connectors.MicrosoftTeams`) | Single package: `Azure.Connectors.Sdk` |
| Per-connector npm packages (`@azure/microsoftteams-connector`) | Single package: `@azure/connectors` |
| GitHub Package Registry (private, PAT required) | NuGet.org / PyPI / npm (public) |
| AutoRest V2 from `sdk/swaggers/` | Internal `CodefulSdkGenerator` (not user-facing) |
| `vscode-azureAPIConnections` VS Code extension | `Connectors-NET-LSP` language server |
| `ITokenProvider` interface | `Azure.Core.TokenCredential` |
| `Microsoft.Azure.Connectors.Sdk` namespace | `Azure.Connectors.Sdk` namespace |

---

## Infrastructure Rename

The old private preview called the backend service **Azure API Connections**. The service exposed a
per-connector REST runtime that accepted a "connection key" as an opaque string — the VS Code
extension was the primary tool for provisioning those keys.

The service has been rebranded and re-architected as the **Connector Gateway** — a first-class ARM
resource type (`Microsoft.Web/connectorGateways`). Each Connector Gateway is a resource in your
subscription that acts as a namespace for one or more named connections. You provision and manage
it through the Azure CLI (`az rest`) rather than through a VS Code extension.

```text
Old: "Azure API Connections"  →  New: "Connector Gateway"
              (opaque connection key)         (ARM resource + connection runtime URL)
```

See [docs/concepts.md](concepts.md) for a full architecture diagram.

---

## Connection Setup

### Old workflow

The VS Code extension `vscode-azureAPIConnections` was the entry point for creating connections.
After completing the OAuth flow inside the extension, you received a connection string (an opaque
key), which you pasted into your code or configuration:

```json
// local.settings.json (old)
{
  "Values": {
    "TeamsConnectionKey": "<opaque-key-from-vs-code-extension>"
  }
}
```

### New workflow

Connections are now ARM resources provisioned via `az rest`. The general sequence is:

1. **Create a Connector Gateway** (`Microsoft.Web/connectorGateways`)
2. **Create a connection** (`/connections/{name}`) inside the gateway
3. **Complete OAuth consent** (browser flow or portal)
4. **Retrieve the connection runtime URL** from the ARM resource
5. **Set environment variables** in `local.settings.json`

```powershell
# Create gateway
az rest --method PUT \
  --uri "https://management.azure.com/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/connectorGateways/$gw?api-version=2026-05-01-preview" \
  --body '{"location":"eastus","identity":{"type":"SystemAssigned"},"properties":{}}'

# Create connection
az rest --method PUT \
  --uri "https://management.azure.com/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/connectorGateways/$gw/connections/teams-test?api-version=2026-05-01-preview" \
  --body '{"properties":{"connectorName":"teams"}}'
```

After OAuth consent, read the runtime URL:

```powershell
az rest --method GET \
  --uri "https://management.azure.com/subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.Web/connectorGateways/$gw/connections/teams-test?api-version=2026-05-01-preview" \
  --query "properties.connectionRuntimeUrl" -o tsv
```

```json
// local.settings.json (new) — Connector Gateway format
{
  "Values": {
    "TeamsConnection__connectorGatewayName": "my-gateway",
    "TeamsConnection__connectionName": "teams-test"
  }
}
```

Or pass the runtime URL directly:

```json
// local.settings.json (new) — direct format
{
  "Values": {
    "TeamsConnection__connectionRuntimeUrl": "https://..."
  }
}
```

The `ConnectorConnectionResolver.Resolve("TeamsConnection")` helper reads these env vars and
returns a `ConnectorConnectionOptions` that your client construction code can consume.

For the step-by-step procedure including access policies and trigger setup, see
[docs/connection-setup.md](connection-setup.md) and the
[Connection Setup Skill](.github/skills/connection-setup/SKILL.md).

---

## Client Instantiation

### Old C# pattern

```csharp
// Old: per-connector static factory, connection key as plain string
using Azure.Connectors.MicrosoftTeams;
using Azure.Connectors.TextAnalytics;

var teamsConnector = MicrosoftTeamsConnector.Create("<connectionKeyFromVSCodeExtension>");
var teams = await teamsConnector.GetAllTeamsAsync();

var textAnalytics = TextAnalyticsConnector.Create("<connectionKey>");
```

### New C# pattern

```csharp
// New: constructor injection, connection runtime URL + TokenCredential
using Azure.Connectors.Sdk.Teams;
using Azure.Identity;

// Defaults to ManagedIdentityCredential (system-assigned) — correct for Azure-hosted apps
using var teamsClient = new TeamsClient(new Uri(connectionRuntimeUrl));

// For local development, pass AzureCliCredential explicitly
using var teamsClient = new TeamsClient(
    new Uri(connectionRuntimeUrl),
    new AzureCliCredential());

var teams = await teamsClient.GetAllTeamsAsync();
```

Use `ConnectorConnectionResolver` to read the runtime URL from configuration:

```csharp
var opts = ConnectorConnectionResolver.Resolve("TeamsConnection");
using var teamsClient = new TeamsClient(
    new Uri(opts.ConnectionRuntimeUrl!),
    new AzureCliCredential());
```

Or, with the DI extension methods:

```csharp
// Program.cs
builder.Services.AddTeamsClient(builder.Configuration, "TeamsConnection");

// Function class — resolved from DI
public class MyFunction(TeamsClient teamsClient) { … }
```

### Old TypeScript pattern

```typescript
// Old: per-connector factory function, connection string from VS Code extension
import { createMicrosoftTeamsConnector } from "@azure/microsoftteams-connector";

const teamsClient = await createMicrosoftTeamsConnector("<ConnectionStringFromVSCodeExtension>");
const myTeams = await teamsClient.getAllTeams();
```

### New TypeScript/Node.js pattern

```typescript
// New: class constructor, connection runtime URL + TokenProvider
import { TeamsClient } from "@azure/connectors/generated/TeamsExtensions";
import { ManagedIdentityTokenProvider } from "@azure/connectors";

const tokenProvider = new ManagedIdentityTokenProvider();
const teamsClient = new TeamsClient(connectionRuntimeUrl, tokenProvider);

const teams = await teamsClient.getAllTeamsAsync();
```

---

## Authentication Model

### Old model

Authentication was handled entirely by the VS Code extension during connection setup. The resulting
connection key (an opaque string) was all the client needed. No Azure Identity credential was
required in application code.

```csharp
// Old: opaque key, no credential object
var connector = MicrosoftTeamsConnector.Create("<opaqueKey>");
```

### New model

Authentication uses **Azure.Core `TokenCredential`** (the same identity model as the Azure SDK).
The credential is passed to the client constructor and used to acquire bearer tokens for the
`https://apihub.azure.com/.default` scope on every request.

| Scenario | Credential |
|----------|-----------|
| Azure-hosted (Function App, App Service) | `ManagedIdentityCredential` (default when none specified) |
| Local development | `AzureCliCredential` (pass explicitly) |
| Service principal / CI | `ClientSecretCredential` |

> **Why the change?** The old opaque key embedded both the identity and the service
> endpoint, which made credential rotation difficult. The new model separates concerns:
> the connection runtime URL identifies the endpoint, and the `TokenCredential`
> provides a freshly-minted access token at request time.

---

## Code Generation

### Old approach

The private preview used **AutoRest V2** to generate per-connector client code from OpenAPI
(Swagger) files stored in the `sdk/swaggers/` directory of the private `Azure/Connectors` repo.
Developers could, in principle, run AutoRest locally to regenerate clients.

```yaml
# AutoRest V2 configuration (sdk/autorest/readme.md excerpt)
version: V2
azure-arm: true
# … per-connector swagger inputs …
```

### New approach

Generation is handled by an internal Microsoft tool called **`CodefulSdkGenerator`**
(part of the BPM/Logic Apps Compiler toolchain). It is **not user-facing** — you do not
run it yourself. The output (`*Extensions.cs` files) is checked into the SDK repo and
updated by the SDK team when connector Swagger definitions change.

If you need a connector that isn't yet included, open an issue in
[Azure/Connectors-NET-SDK](https://github.com/Azure/Connectors-NET-SDK/issues).

---

## Package Distribution

### Old distribution

Per-connector packages were published to the **GitHub Package Registry** (private). Each
connector shipped as a separate package, and consumers had to add the private registry as a NuGet
source and authenticate with a Personal Access Token (PAT).

```powershell
# One-time setup (old)
dotnet nuget add source https://nuget.pkg.github.com/Azure/index.json \
  --name AzureGPR \
  --username <GitHubUserName> \
  --password <PAT> \
  --store-password-in-clear-text

# Per-connector install (old)
dotnet add package Azure.Connectors.MicrosoftTeams --version 0.0.2-alpha
dotnet add package Azure.Connectors.TextAnalytics  --version 0.0.2-alpha
```

### New distribution

All connectors ship in a **single public package** on NuGet.org — no PAT, no private registry.

```bash
# .NET
dotnet add package Azure.Connectors.Sdk --prerelease

# Python
pip install azure-connectors

# Node.js / TypeScript
npm install @azure/connectors
```

---

## TypeScript / JavaScript Support

### Old approach

TypeScript support shipped as per-connector npm packages (`@azure/microsoftteams-connector`,
etc.) on the GitHub Package Registry. The packages required a private registry login and a
`postinstall` script workaround:

```json
// package.json (old)
{
  "scripts": {
    "postinstall": "npm i @azure/microsoftteams-connector --registry https://npm.pkg.github.com/Azure --save-optional"
  },
  "dependencies": {
    "@azure/identity": "^1.1.0",
    "@azure/ms-rest-js": "^2.0.8",
    "@azure/ms-rest-azure-js": "2.0.1"
  }
}
```

### New approach

All connectors ship in a single public npm package `@azure/connectors`, available from the
standard npm registry. The SDK is a modern ESM/CJS dual package targeting Node.js ≥ 18.

```bash
npm install @azure/connectors
```

The Node.js SDK lives in [Azure/Connectors-Node-SDK](https://github.com/Azure/Connectors-Node-SDK).
Generated client files (e.g., `TeamsExtensions.ts`, `Office365Extensions.ts`) follow the same
naming conventions as the .NET SDK.

---

## VS Code Extension

### Old extension

The old workflow centered on the **`vscode-azureAPIConnections`** VS Code extension. Its primary
purpose was connection management: authenticate to Azure, create connector connections, and copy
the resulting connection strings into your project.

### New tooling

The `vscode-azureAPIConnections` extension is no longer maintained. Two distinct tools now
serve different purposes:

| Tool | Purpose |
|------|---------|
| **[Connectors-NET-LSP](https://github.com/Azure/Connectors-NET-LSP)** | Language Server Protocol server providing IntelliSense, hover docs, and code navigation for SDK development |
| **AI agent skill** ([connection-setup](../.github/skills/connection-setup/SKILL.md)) | Automates the end-to-end connection lifecycle from within VS Code Copilot Chat |

Connection management (what the old extension did) is now handled via the Azure CLI (`az rest`)
or the AI agent skill — both described in [Connection Setup](#connection-setup) above.

---

## Namespace and Type Renames

If you were using an early release of the new SDK (before v0.9.0), you may encounter these
additional renames from the `CHANGELOG`:

| Old | New |
|-----|-----|
| `Microsoft.Azure.Connectors.Sdk.*` namespace | `Azure.Connectors.Sdk.*` namespace |
| `Microsoft.Azure.Connectors.Sdk` NuGet package | `Azure.Connectors.Sdk` NuGet package |
| `ITokenProvider` interface | `Azure.Core.TokenCredential` |
| `DefaultAzureCredential` (old default) | `ManagedIdentityCredential` (new default) |
| `ConnectorClientOptions.MaxRetryAttempts` | `options.Retry.MaxRetries` (Azure.Core) |
| `ConnectorClientOptions.Timeout` | `options.Retry.NetworkTimeout` (Azure.Core) |
| Per-connector exception types (`Office365ConnectorException`) | `ConnectorException` (unified) |

---

## Samples

Working end-to-end samples for the current SDK are in
[Azure/Connectors-NET-Samples](https://github.com/Azure/Connectors-NET-Samples). Each sample
is an Azure Functions project that demonstrates a real connector operation (send email,
list SharePoint files, post a Teams message, etc.).

The old `samples/` directory from the private preview repo has no equivalent mapping — the
connection setup and instantiation patterns are different enough that the old samples cannot
be ported by a simple find-and-replace.

---

## Further Reading

- [README.md](../README.md) — SDK overview, quick start, and validated connectors list
- [docs/concepts.md](concepts.md) — Architecture, glossary, Connector Gateway topology
- [docs/connection-setup.md](connection-setup.md) — Full connection provisioning walkthrough
- [CHANGELOG.md](../CHANGELOG.md) — Version-by-version breaking changes
- [GENERATION.md](../GENERATION.md) — How connector clients are generated
