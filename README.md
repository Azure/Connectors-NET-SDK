[![Build Status](https://dev.azure.com/azfunc/public/_apis/build/status%2Fazure%2Fconnectors-sdk%2Fconnectors-sdk.public?repoName=Azure%2FConnectors-NET-SDK&branchName=main)](https://dev.azure.com/azfunc/public/_build/latest?definitionId=1716&repoName=Azure%2FConnectors-NET-SDK&branchName=main)
[![NuGet](https://img.shields.io/nuget/v/Azure.Connectors.Sdk.svg)](https://www.nuget.org/packages/Azure.Connectors.Sdk)

# Azure Connectors .NET SDK

Type-safe .NET clients for [Azure connectors](https://learn.microsoft.com/connectors/connector-reference/) — call Office 365, SharePoint, Teams, Dataverse, and 1,000+ connectors directly from Azure Functions and other .NET apps.

> [!CAUTION]
> **Early Preview — Not for Production Use**
>
> This SDK is currently in early preview and is under active development. It is intended for evaluation, experimentation, and feedback purposes only.
>
> - **Do not use this SDK in production environments.**
> - **Breaking changes should be expected** across APIs, data models, and behavior in future releases.
> - Features may be added, modified, or removed without prior notice.
>
> We welcome feedback and contributions — please [open an issue](https://github.com/Azure/Connectors-NET-SDK/issues) with questions, suggestions, or bug reports.

## Why This SDK?

Azure provides a rich ecosystem of [managed connectors](https://learn.microsoft.com/connectors/connector-reference/) that bridge your code to SaaS services, PaaS resources, and on-premises systems. Originally powering Azure Logic Apps and Power Automate, these connectors are now available as **standalone, strongly-typed C# clients** for any .NET application — no workflow service required.

- **Type-safe operations** — Generated async methods with full IntelliSense and XML documentation
- **Built-in authentication** — Managed identity and connection string token providers for API Hub
- **Resilient HTTP** — Configurable retry policies for transient failures
- **1,000+ connectors** — Any Azure managed connector available via API Hub can be generated

> **Note:** This is the .NET SDK. Python, Node.js, and Java SDKs are planned in collaboration with the Azure Functions team.

## How It Works

```text
┌─────────────────────────────────────┐
│       Your Azure Function / App     │
│                                     │
│  using Office365Client;             │
│  await client.SendEmailAsync(...);  │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Generated Connector Clients       │
│   (Office365Extensions.cs, etc.)    │
│                                     │
│  • Typed async methods per action   │
│  • Input/output models from Swagger │
│  • XML docs from connector metadata │
└──────────────┬──────────────────────┘
               │ depends on
               ▼
┌─────────────────────────────────────┐
│   Azure Connectors .NET SDK         │
│   Azure.Connectors.Sdk              │
│                                     │
│  • Azure.Core TokenCredential auth  │
│  • ConnectorHttpClient + retry      │
│  • ConnectorJsonSerializer          │
└─────────────────────────────────────┘
```

## Installation

### From NuGet.org

```bash
dotnet add package Azure.Connectors.Sdk --prerelease
```

## Quick Start

### 1. Generate Connector Code

Use the [`LogicAppsCompiler`](GENERATION.md) CLI to generate typed clients from connector Swagger definitions:

```powershell
LogicAppsCompiler.exe ./generated unused --directClient --connectors=office365
```

See [GENERATION.md](GENERATION.md) for detailed generation instructions.

### 2. Add Generated Code to Your Project

Copy the generated `*Extensions.cs` files to your project.

### 3. Use the Typed Client

```csharp
using System;
using Azure.Connectors.Sdk;
using Azure.Connectors.Sdk.Office365;
using Azure.Identity;

// Get connection runtime URL from Azure Portal
var connectionRuntimeUrl = "https://...";

// Create client — defaults to ManagedIdentityCredential (system-assigned)
using var client = new Office365Client(connectionRuntimeUrl);

// For local development, pass AzureCliCredential explicitly
using var localClient = new Office365Client(
    new Uri(connectionRuntimeUrl),
    new AzureCliCredential());

// Or configure retry and diagnostics via ConnectorClientOptions
using var clientWithOptions = new Office365Client(
    new Uri(connectionRuntimeUrl),
    new AzureCliCredential(),
    new ConnectorClientOptions());

// Call typed operations
await client.SendEmailAsync(new SendEmailInput
{
    To = "recipient@example.com",
    Subject = "Hello from SDK",
    Body = "<p>Email body</p>"
});

var categories = await client.GetOutlookCategoryNamesAsync();
```

## SDK Components

### Client Base

| Component | Description |
|-----------|-------------|
| `ConnectorClientBase` | Abstract base class for all generated clients — provides authentication, retry, OTel tracing, JSON serialization, and SSRF-protected URL resolution |
| `ConnectorClientOptions` | Configuration for retry count, timeout, exponential backoff, and initial retry delay |
| `ConnectorException` | Unified exception for connector API failures with `ConnectorName`, `Operation`, `StatusCode`, and `ResponseBody` |

### Authentication

Authentication uses Azure.Core `TokenCredential` directly — any credential from `Azure.Identity` works:

| Credential | Use Case |
|----------|----------|
| `ManagedIdentityCredential` | Azure-hosted apps — system-assigned by default when no credential is specified |
| `AzureCliCredential` | Local development — pass explicitly (default no longer probes CLI) |
| `ClientSecretCredential` | Service principal authentication |

### HTTP

| Component | Description |
|-----------|-------------|
| `ConnectorHttpClient` | HTTP client with built-in authentication |

### Serialization

| Component | Description |
|-----------|-------------|
| `ConnectorJsonSerializer` | JSON serialization with connector conventions |
| `JsonConverters` | Custom converters for connector types |

## Documentation

- [docs/concepts.md](docs/concepts.md) - Key concepts, terminology, and architecture
- [GENERATION.md](GENERATION.md) - How to generate connector code
- [docs/connection-setup.md](docs/connection-setup.md) - Setting up connections for local testing
- [ROADMAP.md](ROADMAP.md) - Connector generation progress and lessons learned
- [Azure/Connectors-NET-Samples](https://github.com/Azure/Connectors-NET-Samples) - Full working samples (Azure Functions, triggers, etc.)

### AI Agent Skills

- [Connection Setup Skill](.github/skills/connection-setup/SKILL.md) - AI agent workflow for creating Connector Gateway connections, OAuth consent, access policies, and configuring app settings — all from within VS Code

## Validated Connectors

| Connector | Status | Validated Operations |
|-----------|--------|----------------------|
| Azure Monitor Logs | 🔄 SDK Generated | QueryData, QueryDataV2, VisualizeQuery, VisualizeQueryV2 |
| IBM MQ | 🔄 SDK Generated | SendAsync, ReadAsync, ReadAllAsync, ReceiveAsync, ReceiveAllAsync, DeleteAsync, DeleteAllAsync |
| Office365 | ✅ Validated | SendEmail, GetOutlookCategoryNames, ExportEmail, CalendarPostItem |
| Office365 Users | ✅ Validated | MyProfile, UserProfile, Manager, DirectReports, SearchUser |
| OneDrive for Business | ✅ Validated | ListRootFolder, ListFolder, GetFileContentByPath, CreateFile, FindFilesByPath, CreateShareLink |
| SharePoint | ✅ Validated | GetAllTables, ListRootFolder, ListFolder, GetFileContentByPath, CreateFile |
| MS Graph Groups & Users | ✅ Validated | ListUsers, ListGroupsByDisplayNameSearch, GetGroupProperties |
| Teams | ✅ Validated | GetAllTeams, GetChannelsForGroup, PostMessageToConversation |
| SMTP | ✅ Validated | SendEmail |
| Azure Event Grid | 🔄 SDK Generated | TopicTypesList, SubscriptionsList (trigger: CreateSubscription) |
| Excel Online | 🔄 SDK Generated | GetItems, GetItem, CreateTable, AddRow, GetAllWorksheets, GetTables |
| Universal Print | ✅ E2E Validated | ListRecentShares, PrintFile |
| Microsoft Defender ATP | ✅ E2E Validated | AdvancedHunting, GetAlerts, CreateAlertByReference |
| Yammer (Viva Engage) | ✅ E2E Validated | GetNetworks, GetGroups, GetUserDetailsById, GetAllMessages |

## Related Projects

| Project | Description |
|---------|-------------|
| [Connector SDK Samples](https://github.com/Azure/Connectors-NET-Samples) | Sample Azure Functions demonstrating SDK usage with Office 365, SharePoint, Teams, and more |
| [Connector SDK LSP](https://github.com/Azure/Connectors-NET-LSP) | Language Server Protocol server and VS Code extension for intelligent code assistance when developing with the SDK |
| [Azure Functions Connector Extension](https://github.com/Azure/azure-functions-connector-extension) | Azure Functions host and worker extensions for managed connectors|

## Regenerating Connectors

Connectors should be regenerated periodically to incorporate Swagger updates:

```powershell
LogicAppsCompiler.exe ./generated unused --directClient --connectors=office365,servicebus,teams
```

## Releasing a New Version

The version comes from the git tag — there is no version file to update.

**Standard release:**

```shell
git checkout main && git pull origin main
git tag v1.2.3
git push origin v1.2.3
```

**Pre-release:**

```shell
git tag v1.2.3-preview.1
git push origin v1.2.3-preview.1
```

The release workflow will build, test, pack, publish to GitHub Packages, and create a GitHub Release with the `.nupkg` attached.

See the [Releasing a New Version](.github/copilot-instructions.md#releasing-a-new-version) section in the copilot instructions for re-release and manual dispatch options.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
