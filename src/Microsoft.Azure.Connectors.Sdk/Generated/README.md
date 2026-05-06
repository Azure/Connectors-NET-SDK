# Generated Connector Clients

This folder contains pre-generated typed connector clients included in the SDK NuGet package.

> **Do not hand-edit generated files.** If the generated code has issues, fix the
> [CodefulSdkGenerator](../../../GENERATION.md) in the BPM repo and regenerate.

## Included Connectors

| Connector | File | Client Class | Description |
|-----------|------|-------------|-------------|
| Azure Resource Manager | `ArmExtensions.cs` | `ArmClient` | Azure Resource Manager (subscriptions, resource groups, resources, deployments, tags) |
| Kusto | `KustoExtensions.cs` | `KustoClient` | Azure Data Explorer (Kusto) queries and commands |
| MS Graph Groups & Users | `MsgraphgroupsanduserExtensions.cs` | `MsgraphgroupsanduserClient` | Microsoft Graph groups and user operations |
| Office 365 | `Office365Extensions.cs` | `Office365Client` | Office 365 Outlook (email, calendar, contacts) |
| OneDrive for Business | `OnedriveforbusinessExtensions.cs` | `OnedriveforbusinessClient` | OneDrive for Business file operations |
| SharePoint Online | `SharepointonlineExtensions.cs` | `SharepointonlineClient` | SharePoint Online lists, items, and files |
| Teams | `TeamsExtensions.cs` | `TeamsClient` | Microsoft Teams messaging and channel operations |

## Usage

```csharp
using Microsoft.Azure.Connectors.Sdk.Office365;

var connectionRuntimeUrl = "https://...";  // From Azure Portal
using var client = new Office365Client(connectionRuntimeUrl);

await client.SendEmailV2Async(new SendEmailV2Input
{
    To = "recipient@example.com",
    Subject = "Hello",
    Body = "<p>Email body</p>"
});
```

## Regenerating

From the BPM repo root, after building the generator:

```powershell
$generator = ".\src\tools\CodefulSdkGenerator\LogicAppsCompiler.Cli\bin\Release\net8.0\LogicAppsCompiler.exe"
& $generator "<path-to-Connectors-NET-SDK>\src\Microsoft.Azure.Connectors.Sdk\Generated" unused --directClient --connectors=kusto,msgraphgroupsanduser,office365,onedriveforbusiness,sharepointonline,teams
```

See [GENERATION.md](../../../GENERATION.md) for complete documentation including prerequisites and build steps.
