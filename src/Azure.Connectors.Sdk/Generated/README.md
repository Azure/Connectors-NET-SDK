# Generated Connector Clients

This folder contains pre-generated typed connector clients included in the SDK NuGet package.

> **Do not hand-edit generated files.** If the generated code has issues, fix the
> [CodefulSdkGenerator](../../../GENERATION.md) in the BPM repo and regenerate.

## Usage

```csharp
using Azure.Connectors.Sdk.Office365;

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
& $generator "<path-to-Connectors-NET-SDK>\src\Azure.Connectors.Sdk\Generated" unused --directClient --connectors=arm,azureblob,azuremonitorlogs,kusto,mq,msgraphgroupsanduser,office365,office365users,onedriveforbusiness,sharepointonline,smtp,teams
```

See [GENERATION.md](../../../GENERATION.md) for complete documentation including prerequisites and build steps.
