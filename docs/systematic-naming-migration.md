# Migrate generated .NET connector names

This preview update corrects product casing and .NET identifier casing across the
shipped generated clients and the `ConnectorNames` catalog. These public API
renames are source-breaking: update `using` directives, DI registrations, client
constructors, model factories, and named arguments, then rebuild your app.

| Before | After |
| --- | --- |
| `using Azure.Connectors.Sdk.AzureIoTCentral;` | `using Azure.Connectors.Sdk.AzureIotCentral;` |
| `using Azure.Connectors.Sdk.AzureIoTCentral.Models;` | `using Azure.Connectors.Sdk.AzureIotCentral.Models;` |
| `AzureIoTCentralClient` / `AzureIoTCentralModelFactory` | `AzureIotCentralClient` / `AzureIotCentralModelFactory` |
| `services.AddAzureIoTCentralClient(configuration)` | `services.AddAzureIotCentralClient(configuration)` |
| `DocumentDbClient` / `DocumentDbModelFactory` | `DocumentDBClient` / `DocumentDBModelFactory` |
| `DocuSignClient` / `DocuSignModelFactory` | `DocusignClient` / `DocusignModelFactory` |
| `MailChimpClient` / `MailChimpModelFactory` | `MailchimpClient` / `MailchimpModelFactory` |
| `MsGraphGroupsAndUsersClient` | `MSGraphGroupsAndUsersClient` |
| `StarrezRestV1Client` | `StarRezRestV1Client` |
| `ConnectorNames.DataFlowsSMS` | `ConnectorNames.DataFlowsSms` |
| `ConnectorNames.MailChimp` | `ConnectorNames.Mailchimp` |
| `ConnectorNames.AzureIoTCentralV3` | `ConnectorNames.AzureIotCentralV3` |

For example, replace:

```csharp
using Azure.Connectors.Sdk.AzureIoTCentral;
using Azure.Connectors.Sdk.AzureIoTCentral.Models;

using var client = new AzureIoTCentralClient(connectionRuntimeUrl);
```

with:

```csharp
using Azure.Connectors.Sdk.AzureIotCentral;
using Azure.Connectors.Sdk.AzureIotCentral.Models;

using var client = new AzureIotCentralClient(connectionRuntimeUrl);
```

Model properties and factory parameter names also change when the old spelling
was inconsistent. For example, Teams `RecordingContentURL` becomes
`RecordingContentUrl`, while its JSON key stays `recordingContentUrl`.
DocumentDB `QueryDocumentsResponse.Documents` becomes `Value`; the serialized
key remains `value`. WordPress's second `Id2` property becomes `Guid`; the
`guid` wire key is unchanged. Check named-argument calls after upgrading;
Jira `ListIssuesAsync(jQLQuery: ...)` becomes `ListIssuesAsync(jqlQuery: ...)`.

Raw connector API IDs (for example `azureiotcentral` and `dataflowssms`),
operation IDs, HTTP routes, JSON/XML field names, and enum wire values are
independent of public identifier spelling. Do not change connection names or
serialized payload keys to match the new C# names. `ServiceBusConnectorClient`
retains its suffix to avoid a dependency type collision. The SDK remains
async-only and does not change `ETag` or URI-valued properties to framework
value types as part of this migration.


## Automatic pagination (`AsyncPageable<T>`) and page-envelope metadata

In alignment with the [Azure SDK Design Guidelines for .NET](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-pagination),
32 additional list/query operations across 13 connectors now return
`Azure.Core.AsyncPageable<TItem>` instead of `Task<TPage>`:

| Connector | Operations converted from `Task<TPage>` to `AsyncPageable<TItem>` | Page-model collection property |
| --- | --- | --- |
| `AzureBlob` | `ListFolderAsync` (`AsyncPageable<DataWithSensitivityLabelInfo>`) | `ListOfBlobsWithSensitivityLabels.Value` |
| `AzureTables` | `GetEntitiesAsync` (`AsyncPageable<EntityItem>`) | `GetEntitiesResponse.Value` (previously `ListOfEntities`) |
| `DocumentDB` | `QueryDocumentsAsync` (`AsyncPageable<ObjectWithoutType>`) | `QueryDocumentsResponse.Value` (previously `Documents`) |
| `ExcelOnline` | `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |
| `ExcelOnlineBusiness` | `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |
| `Insightly` | `ListProjectsAsync` (`AsyncPageable<Project>`) | `ListProjectsResponse.Value` |
| `Jira` | `ListFiltersAsync`, `ListProjectsAsync` (`AsyncPageable<JsonElement?>`) | `ListFiltersResponse.Value`, `ListProjectsResponse.Value` |
| `Office365` | `ContactGetItemsAsync` (`AsyncPageable<ContactResponse>`) | `EntityListResponseContactResponse.Value` |
| `Onenote` | `GetPagesInSectionAsync` (`AsyncPageable<JsonElement?>`) | `GetPagesInSectionResponse.Value` (previously `PagesInSectionValueObject`) |
| `Salesforce` | `GetTablesAsync`, `GetItemsAsync`, `GetItemsTableAccountAsync`, `GetItemsTableUserAsync`, `GetItemsTableCaseAsync`, `GetItemsTableOpportunityAsync`, `GetItemsTableProduct2Async`, `GetItemsTableContactAsync` | `TablesList.Value`, `ItemsList.Value` |
| `SharePointOnline` | `GetFileItemsAsync`, `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |
| `Sql` | `GetItemsAsync` (`AsyncPageable<SqlItem>`), `GetTablesForDeleteItemAsync`, `GetTablesForGetItemAsync`, `GetTablesForGetOnNewItemsAsync`, `GetTablesForGetOnUpdatedItemsAsync`, `GetTablesForPatchItemAsync`, `GetTablesForPostItemAsync` (`AsyncPageable<Table>`) | `GetItemsResponse.Value`, `TablesList.Value` |
| `Yammer` | `GetAllMessagesAsync`, `GetMessagesFollowingAsync`, `GetMessagesInGroupAsync`, `GetMessagesInThreadAsync` (`AsyncPageable<Message>`) | `PageableMessageList.Value` |
| `Zendesk` | `GetItemsAsync` (`AsyncPageable<Item>`) | `ItemsList.Value` |

Replace single-page `await client.GetItemsAsync(...)` calls with `await foreach`:

```csharp
await foreach (var item in client.GetItemsAsync("OneDrive", "book.xlsx", "Table1", "me"))
{
    // Process each row across all pages
}
```

### Accessing page-level sibling properties (`SensitivityLabelInfo`, `RequestCharge`, `TableMetadataLocation`)

When a paginated response envelope carries sibling properties alongside `Value`
and `NextLink` (such as `ExcelOnlineBusiness.Models.ItemsList.SensitivityLabelInfo`,
`DocumentDB.Models.QueryDocumentsResponse.RequestCharge`, or
`AzureTables.Models.GetEntitiesResponse.TableMetadataLocation`), enumerate pages
with `AsPages()`. Each yielded `Page<TItem>` is a
`ConnectorPage<TPage, TItem>` that exposes both the strongly typed
`PageModel` and a buffered `GetRawResponse()`:

```csharp
await foreach (var page in client.GetItemsAsync(
    "OneDrive",
    "book.xlsx",
    "Table1",
    "me",
    extractSensitivityLabel: true,
    sensitivityLabelMetadata: true).AsPages())
{
    if (page is ConnectorPage<ItemsList, Item> connectorPage)
    {
        IReadOnlyList<SensitivityLabelMetadata> labels = connectorPage.PageModel.SensitivityLabelInfo;
    }

    // Or via the standard Azure Core raw response:
    ItemsList? envelope = page.GetRawResponse().Content.ToObjectFromJson<ItemsList>();
}
```

## Typed dictionary properties (`Dictionary<string, TValue>`)

Properties whose Swagger schema declares an object map (`additionalProperties`
with a value schema and no `x-ms-dynamic-schema` marker) are now strongly typed
as `Dictionary<string, string>` or `Dictionary<string, ObjectEntity>` instead of
`JsonElement?`:

| Connector | Affected properties | New type |
| --- | --- | --- |
| `Arm` | `Properties`, `Tags` | `Dictionary<string, string>` |
| `AzureAD` | `CustomProperties` | `Dictionary<string, string>` |
| `AzureDataFactory` | `Tags` | `Dictionary<string, string>` |
| `CommonDataService` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` / `Dictionary<string, string>` |
| `EventHubs` | `Properties` | `Dictionary<string, ObjectEntity>` |
| `ExcelOnline` / `ExcelOnlineBusiness` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` |
| `Outlook` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` |
| `Salesforce` | `DynamicProperties`, `QueryParameters` | `Dictionary<string, ObjectEntity>`, `Dictionary<string, string>` |
| `ServiceBus` | `Properties` | `Dictionary<string, string>` |
| `SharePointOnline` | `DynamicProperties`, `Headers` | `Dictionary<string, ObjectEntity>`, `Dictionary<string, string>` |
| `Sql` | `DynamicProperties`, `FormalParameters` | `Dictionary<string, ObjectEntity>`, `Dictionary<string, string>` |
| `Zendesk` | `DynamicProperties` | `Dictionary<string, ObjectEntity>` |
