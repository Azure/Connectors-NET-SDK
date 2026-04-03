# Generated Connector Clients

This folder is for pre-generated connector clients to be included in the SDK package.

## ⚠️ Generator Bugs

The DirectClientGenerator in the BPM repo currently has bugs that produce non-compiling code:

1. **Missing types** - Some types like `MeetingTimeSuggestionsV2`, `AssignCategoryBulkInput` are referenced but not defined
2. **Escaped braces** - Properties generate `{{ get; }}` instead of `{ get; }` 
3. **Method group vs invocation** - `queryParams.Count` instead of `queryParams.Count()`
4. **Collection literal vs List** - Uses `[]` collection literal then calls `.Add()` on it

These bugs need to be fixed in the BPM PR before generated code can be included.

**Current status:** Generated files are excluded from compilation via csproj.

## After Generator Fixes

Once the generator is fixed, this folder will contain:

| Connector | File | Description |
|-----------|------|-------------|
| Office365 | `Office365Extensions.cs` | Office 365 Outlook (email, calendar, contacts) |

## Usage (After Fix)

```csharp
using Microsoft.Azure.Connectors.DirectClient.Office365;

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

```powershell
$generator = ".\src\tools\CodefulSdkGenerator\LogicAppsCompiler.Cli\bin\Release\LogicAppsCompiler.exe"
& $generator ".\src\Microsoft.Azure.Workflows.Connectors.Sdk\Generated" unused --directClient --connectors=office365
```

See [GENERATION.md](../../../GENERATION.md) for complete documentation.
