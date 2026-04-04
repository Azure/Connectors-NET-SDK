[![CI](https://github.com/Azure/Connectors-NET-SDK/actions/workflows/ci.yml/badge.svg)](https://github.com/Azure/Connectors-NET-SDK/actions/workflows/ci.yml)

# Azure Connectors .NET SDK

Type-safe .NET clients for [Azure connectors](https://learn.microsoft.com/connectors/connector-reference/) — call Office 365, SharePoint, Teams, Dataverse, and 1,000+ connectors directly from Azure Functions and other .NET apps.

## Why This SDK?

Azure provides a rich ecosystem of [managed connectors](https://learn.microsoft.com/connectors/connector-reference/) that bridge your code to SaaS services, PaaS resources, and on-premises systems. Originally powering Azure Logic Apps and Power Automate, these connectors are now available as **standalone, strongly-typed C# clients** for any .NET application — no workflow service required.

- **Type-safe operations** — Generated async methods with full IntelliSense and XML documentation
- **Built-in authentication** — Managed identity and connection string token providers for API Hub
- **Resilient HTTP** — Configurable retry policies for transient failures
- **1,000+ connectors** — Any Azure managed connector available via API Hub can be generated

> **Note:** This is the .NET SDK. Python, Node.js, and Java SDKs are planned in collaboration with the Azure Functions team.

## How It Works

```
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
│   Microsoft.Azure.Workflows        │
│   .Connectors.Sdk                   │
│                                     │
│  • ManagedIdentityTokenProvider     │
│  • ConnectorHttpClient + retry      │
│  • ConnectorJsonSerializer          │
└─────────────────────────────────────┘
```

## Installation

```bash
dotnet add package Microsoft.Azure.Workflows.Connectors.Sdk
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
using Microsoft.Azure.Connectors.DirectClient.Office365;

// Get connection runtime URL from Azure Portal
var connectionRuntimeUrl = "https://...";

// Create client (uses DefaultAzureCredential by default)
using var client = new Office365Client(connectionRuntimeUrl);

// Call typed operations
await client.SendEmailV2Async(new SendEmailV2Input
{
    To = "recipient@example.com",
    Subject = "Hello from SDK",
    Body = "<p>Email body</p>"
});

var categories = await client.GetOutlookCategoryNamesAsync();
```

## SDK Components

### Authentication

| Provider | Use Case |
|----------|----------|
| `ManagedIdentityTokenProvider` | Azure-hosted apps (App Service, Functions) |
| `ConnectionStringTokenProvider` | Local development with API keys |

### HTTP

| Component | Description |
|-----------|-------------|
| `ConnectorHttpClient` | HTTP client with built-in authentication |
| `RetryPolicy` | Configurable retry behavior for transient failures |
| `HttpExtensions` | Helper methods for HTTP operations |

### Serialization

| Component | Description |
|-----------|-------------|
| `ConnectorJsonSerializer` | JSON serialization with connector conventions |
| `JsonConverters` | Custom converters for connector types |

## Documentation

- [GENERATION.md](GENERATION.md) - How to generate connector code
- [docs/connection-setup.md](docs/connection-setup.md) - Setting up connections for local testing
- [ROADMAP.md](ROADMAP.md) - Connector generation progress and lessons learned
- [samples/](samples/) - Sample usage projects

### AI Agent Skills

- [Connection Setup Skill](.github/skills/connection-setup/SKILL.md) - AI agent workflow for creating AI Gateway connections, OAuth consent, access policies, and configuring app settings — all from within VS Code

## Validated Connectors

| Connector | Status | Validated Operations |
|-----------|--------|----------------------|
| Office365 | ✅ Validated | SendEmailV2, GetOutlookCategoryNames |
| SharePoint | ✅ Validated | GetAllTables (list libraries) |

## Related Projects

| Project | Description |
|---------|-------------|
| Connector SDK Samples (planned) | Sample Azure Functions demonstrating SDK usage with Office 365, SharePoint, and more |
| Connector SDK LSP (planned) | Language Server Protocol server and VS Code extension for intelligent code assistance when developing with the SDK |

## Regenerating Connectors

Connectors should be regenerated periodically to incorporate Swagger updates:

```powershell
LogicAppsCompiler.exe ./generated unused --directClient --connectors=office365,servicebus,teams
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
