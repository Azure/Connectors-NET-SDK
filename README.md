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
│   Microsoft.Azure.Connectors.Sdk    │
│                                     │
│                                     │
│  • ManagedIdentityTokenProvider     │
│  • ConnectorHttpClient + retry      │
│  • ConnectorJsonSerializer          │
└─────────────────────────────────────┘
```

## Installation

### From NuGet.org (coming soon)

```bash
dotnet add package Microsoft.Azure.Connectors.Sdk
```

### From GitHub Packages (private feed)

While the repo is private, packages are published to the GitHub Packages feed on each tagged release.

1. Authenticate with a GitHub PAT that can access packages from the private repository:
   - For a classic PAT, grant `repo` and `read:packages` scopes.
   - For a fine-grained PAT, grant the repository access and `read` permission for packages.

   ```shell
   dotnet nuget add source https://nuget.pkg.github.com/Azure/index.json \
     --name github-azure \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_GITHUB_PAT
   ```

2. Install the package:

   ```shell
   dotnet add package Microsoft.Azure.Connectors.Sdk
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
await client.SendEmailAsync(new SendEmailInput
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
- [Azure/Connectors-NET-Samples](https://github.com/Azure/Connectors-NET-Samples) - Full working samples (Azure Functions, triggers, etc.)

### AI Agent Skills

- [Connection Setup Skill](.github/skills/connection-setup/SKILL.md) - AI agent workflow for creating AI Gateway connections, OAuth consent, access policies, and configuring app settings — all from within VS Code

## Validated Connectors

| Connector | Status | Validated Operations |
|-----------|--------|----------------------|
| Office365 | ✅ Validated | SendEmail, GetOutlookCategoryNames, ExportEmail, CalendarPostItem |
| OneDrive for Business | ✅ Validated | ListRootFolder, ListFolder, GetFileContentByPath, CreateFile, FindFilesByPath, CreateShareLink |
| SharePoint | ✅ Validated | GetAllTables, ListRootFolder, ListFolder, GetFileContentByPath, CreateFile |
| MS Graph Groups & Users | ✅ Validated | ListUsers, ListGroupsByDisplayNameSearch, GetGroupProperties |
| Teams | ✅ Validated | GetAllTeams, GetChannelsForGroup, PostMessageToConversation |

## Related Projects

| Project | Description |
|---------|-------------|
| [Connector SDK Samples](https://github.com/Azure/Connectors-NET-Samples) | Sample Azure Functions demonstrating SDK usage with Office 365, SharePoint, Teams, and more |
| [Connector SDK LSP](https://github.com/Azure/Connectors-NET-LSP) | Language Server Protocol server and VS Code extension for intelligent code assistance when developing with the SDK |

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
