# Migrate generated .NET connector names

This preview update corrects product casing and .NET identifier casing across the
shipped generated clients and the `ConnectorNames` catalog. These public API
renames are source-breaking: update `using` directives, DI registrations, client
constructors, model factories, trigger payload types, and named arguments, then
rebuild your app.

## Renamed connector namespaces, clients, model factories, and DI helpers

| Connector (`ConnectorName`) | Namespace & Models `using` (Before → After) | Client & Model Factory (Before → After) | DI registration (Before → After) |
| --- | --- | --- | --- |
| `azurequeues` | `Azure.Connectors.Sdk.Azurequeues(.Models)` → `Azure.Connectors.Sdk.AzureQueues(.Models)` | `AzureQueuesClient` / `AzureQueuesModelFactory` *(unchanged)* | `AddAzureQueuesClient` *(unchanged)* |
| `azuretables` | `Azure.Connectors.Sdk.Azuretables(.Models)` → `Azure.Connectors.Sdk.AzureTables(.Models)` | `AzureTablesClient` / `AzureTablesModelFactory` *(unchanged)* | `AddAzureTablesClient` *(unchanged)* |
| `commondataservice` | `Azure.Connectors.Sdk.Commondataservice(.Models)` → `Azure.Connectors.Sdk.CommonDataService(.Models)` | `CommondataserviceClient` / `CommondataserviceModelFactory` → `CommonDataServiceClient` / `CommonDataServiceModelFactory` | *(none)* |
| `dataflowssms` | `Azure.Connectors.Sdk.DataflowsSms(.Models)` → `Azure.Connectors.Sdk.DataFlowsSms(.Models)` | `DataflowsSmsClient` / `DataflowsSmsModelFactory` → `DataFlowsSmsClient` / `DataFlowsSmsModelFactory` | *(none)* |
| `documentdb` | `Azure.Connectors.Sdk.Documentdb(.Models)` → `Azure.Connectors.Sdk.DocumentDB(.Models)` | `DocumentDbClient` / `DocumentDbModelFactory` → `DocumentDBClient` / `DocumentDBModelFactory` | `AddDocumentDbClient` → `AddDocumentDBClient` |
| `docusign` | `Azure.Connectors.Sdk.DocuSign(.Models)` → `Azure.Connectors.Sdk.Docusign(.Models)` | `DocuSignClient` / `DocuSignModelFactory` → `DocusignClient` / `DocusignModelFactory` | *(none)* |
| `eventhubs` | `Azure.Connectors.Sdk.Eventhubs(.Models)` → `Azure.Connectors.Sdk.EventHubs(.Models)` | `EventHubsClient` / `EventHubsModelFactory` *(unchanged)* | `AddEventHubsClient` *(unchanged)* |
| `freshservice` | `Azure.Connectors.Sdk.FreshService(.Models)` → `Azure.Connectors.Sdk.Freshservice(.Models)` | `FreshServiceClient` / `FreshServiceModelFactory` → `FreshserviceClient` / `FreshserviceModelFactory` | `AddFreshServiceClient` → `AddFreshserviceClient` |
| `jedoxodatahub` | `Azure.Connectors.Sdk.JedoxOdataHub(.Models)` → `Azure.Connectors.Sdk.JedoxODataHub(.Models)` | `JedoxOdataHubClient` / `JedoxOdataHubModelFactory` → `JedoxODataHubClient` / `JedoxODataHubModelFactory` | `AddJedoxOdataHubClient` → `AddJedoxODataHubClient` |
| `mailchimp` | `Azure.Connectors.Sdk.MailChimp(.Models)` → `Azure.Connectors.Sdk.Mailchimp(.Models)` | `MailChimpClient` / `MailChimpModelFactory` → `MailchimpClient` / `MailchimpModelFactory` | *(none)* |
| `mq` | `Azure.Connectors.Sdk.Mq(.Models)` → `Azure.Connectors.Sdk.MQ(.Models)` | `MqClient` / `MqModelFactory` → `MQClient` / `MQModelFactory` | `AddMqClient` → `AddMQClient` |
| `msgraphgroupsanduser` | `Azure.Connectors.Sdk.MsGraphGroupsAndUsers(.Models)` → `Azure.Connectors.Sdk.MSGraphGroupsAndUsers(.Models)` | `MsGraphGroupsAndUsersClient` / `MsGraphGroupsAndUsersModelFactory` → `MSGraphGroupsAndUsersClient` / `MSGraphGroupsAndUsersModelFactory` | `AddMsGraphGroupsAndUsersClient` → `AddMSGraphGroupsAndUsersClient` |
| `revai` | `Azure.Connectors.Sdk.Revai(.Models)` → `Azure.Connectors.Sdk.RevAI(.Models)` | `RevaiClient` / `RevaiModelFactory` → `RevAIClient` / `RevAIModelFactory` | `AddRevaiClient` → `AddRevAIClient` |
| `servicebus` | `Azure.Connectors.Sdk.Servicebus(.Models)` → `Azure.Connectors.Sdk.ServiceBus(.Models)` | `ServiceBusConnectorClient` / `ServiceBusModelFactory` *(unchanged)* | `AddServiceBusConnectorClient` *(unchanged)* |
| `starrezrestv1` | `Azure.Connectors.Sdk.StarrezRestV1(.Models)` → `Azure.Connectors.Sdk.StarRezRestV1(.Models)` | `StarrezRestV1Client` / `StarrezRestV1ModelFactory` → `StarRezRestV1Client` / `StarRezRestV1ModelFactory` | `AddStarrezRestV1Client` → `AddStarRezRestV1Client` |
| `zeptomail` | `Azure.Connectors.Sdk.Zeptomail(.Models)` → `Azure.Connectors.Sdk.ZeptoMail(.Models)` | `ZeptomailClient` / `ZeptomailModelFactory` → `ZeptoMailClient` / `ZeptoMailModelFactory` | `AddZeptomailClient` → `AddZeptoMailClient` |

For example, replace:

```csharp
using Azure.Connectors.Sdk.Documentdb;
using Azure.Connectors.Sdk.Documentdb.Models;

using var client = new DocumentDbClient(connectionRuntimeUrl);
```

with:

```csharp
using Azure.Connectors.Sdk.DocumentDB;
using Azure.Connectors.Sdk.DocumentDB.Models;

using var client = new DocumentDBClient(connectionRuntimeUrl);
```

## `ConnectorNames` constant renames and additions

`ConnectorNames` constants now apply the same .NET acronym and closed-word rules
as generated types (3+ letter acronyms use PascalCase such as `Http`, `Json`,
`Xml`, `Pdf`, `Url`, `Uri`, `Sql`, `Api`, `Rest`, `Sms`, `Smtp`, `Rss`, `Ocr`,
`Dns`, `Tls`, `Tcp`, `Csv`, `Xls`, `Xlsx`, and `Id`, while 2-letter initialisms
such as `AI`, `AD`, `DB`, `IO`, `IP`, `MQ`, and `MS` remain uppercase) and
include `ConnectorNames.Docusign` (`"docusign"`) and `ConnectorNames.Jira`
(`"jira"`). Key renames for shipped clients include:

| Before | After |
| --- | --- |
| `ConnectorNames.DataFlowsSMS` | `ConnectorNames.DataFlowsSms` |
| `ConnectorNames.MailChimp` | `ConnectorNames.Mailchimp` |
| `ConnectorNames.MsGraphGroupsAndUsers` | `ConnectorNames.MSGraphGroupsAndUsers` |
| `ConnectorNames.ClickSendSMS` | `ConnectorNames.ClickSendSms` |
| `ConnectorNames.PlivoSMS` | `ConnectorNames.PlivoSms` |
| `ConnectorNames.PDFco` | `ConnectorNames.Pdfco` |
| `ConnectorNames.RSS` | `ConnectorNames.Rss` |
| `ConnectorNames.SMTP` | `ConnectorNames.Smtp` |
| `ConnectorNames.SQLServer` | `ConnectorNames.SqlServer` |
| `ConnectorNames.StarRezRESTV1` | `ConnectorNames.StarRezRestV1` |
| `ConnectorNames.MicrosoftDefenderATP` | `ConnectorNames.MicrosoftDefenderAtp` |

## Trigger callback payload and trigger metadata renames

Trigger operation and parameter container classes follow the updated connector
prefixes (`CommonDataServiceTriggerOperations`, `DataFlowsSmsTriggerOperations`,
`DocusignTriggerOperations`, `FreshserviceTriggerOperations`,
`ServiceBusTriggerOperations`, and `ServiceBusTriggerParameters`), and trigger
callback payload types strip a redundant trailing `Trigger` token before
`TriggerPayload`:

| Before | After |
| --- | --- |
| `AzureIoTCentralOnWorkflowCreateTriggerTriggerPayload` | `AzureIoTCentralOnWorkflowCreateTriggerPayload` |
| `CommondataserviceOnDeletedItemsTriggerPayload` / `OnNewItems` / `OnUpdatedItems` | `CommonDataServiceOnDeletedItemsTriggerPayload` / `OnNewItems` / `OnUpdatedItems` |
| `DataflowsSmsOnRegisterWebhookTriggerPayload` | `DataFlowsSmsOnRegisterWebhookTriggerPayload` |
| `DocuSignOnCreateHookEnvelopeTriggerPayload` / `OnCreateOrgHookEnvelope` | `DocusignOnCreateHookEnvelopeTriggerPayload` / `OnCreateOrgHookEnvelope` |
| `ElfsquadDataOnCreateTriggerTriggerPayload` | `ElfsquadDataOnCreateTriggerPayload` |
| `FreshServiceOnTicketCreatedTriggerPayload` | `FreshserviceOnTicketCreatedTriggerPayload` |
| `GitHubOnWebhookPullRequestTriggerTriggerPayload` | `GitHubOnWebhookPullRequestTriggerPayload` |
| `JiraOnNewIssueJQLTriggerPayload` / `OnNewIssueJQLDatacenterTriggerPayload` | `JiraOnNewIssueJqlTriggerPayload` / `OnNewIssueJqlDatacenterTriggerPayload` |
| `PlumsailOnFlowV1ProcessesFlowTriggersPostTriggerPayload` | `PlumsailOnFlowV1ProcessesFlowTriggersV2PostTriggerPayload` *(refreshed to non-deprecated V2 trigger)* |
| `RepliconOnWebhookSubscriptionsRestAPITriggerPayload` | `RepliconOnWebhookSubscriptionsRestApiTriggerPayload` |
| `ServicebusOnGetMessagesFromQueueTriggerPayload` / `FromTopic` / `WithPeekLock` | `ServiceBusOnGetMessagesFromQueueTriggerPayload` / `FromTopic` / `WithPeekLock` |
| `TeamsOnRecordingTriggerTriggerPayload` / `OnTranscriptTriggerTriggerPayload` | `TeamsOnRecordingTriggerPayload` / `OnTranscriptTriggerPayload` |
| `TeamsOnWebhookAtMentionTriggerTriggerPayload` / `ChatMessage` / `Keyword` / `MessageReaction` / `NewMessage` | `TeamsOnWebhookAtMentionTriggerPayload` / `ChatMessage` / `Keyword` / `MessageReaction` / `NewMessage` |
| `WaywedoOnGenerateAcceptancePDFWebHookTriggerPayload` | `WaywedoOnGenerateAcceptancePdfWebHookTriggerPayload` |

## Model property, `Id` → `Guid`, and parameter renames

Model properties and factory parameter names also change when the old spelling
was inconsistent or when a wire `guid` field was previously rewritten to `Id`:

- **`Id` / `Id2` → `Guid`**: Wire fields named `guid` now preserve `Guid` on the
  generated model instead of collapsing to `Id`:
  - `Docuware.Models.PlaceAStampResponse.Id` → `PlaceAStampResponse.Guid` (wire key `"Guid"`)
  - `WordPress.Models.PostResponse.Id2` → `PostResponse.Guid` (wire key `"guid"`, alongside `PostResponse.Id`)
  - `WordPress.Models.PostModel.Id2` → `PostModel.Guid` (wire key `"guid"`, alongside `PostModel.Id`)
- **Acronym & plural-acronym casing**: Teams `RecordingContentURL` becomes
  `RecordingContentUrl`, Docusign `GetAllWorkflowIDsAsync` / `WorkflowIDs`
  becomes `GetAllWorkflowIdsAsync` / `WorkflowIds`, and Jira
  `ListIssuesAsync(jQLQuery: ...)` becomes `ListIssuesAsync(jqlQuery: ...)`.
- **Page-envelope collection property**: DocumentDB
  `QueryDocumentsResponse.Documents` becomes `Value` (wire key `"value"`).

Raw connector API IDs (for example `azureiotcentral` and `dataflowssms`),
operation IDs, HTTP routes, JSON/XML field names, and enum wire values are
independent of public identifier spelling. Do not change connection names or
serialized payload keys to match the new C# names. `ServiceBusConnectorClient`
retains its suffix to avoid a dependency type collision. The SDK remains
async-only and does not change `ETag` or URI-valued properties to framework
value types as part of this migration.

## Automatic pagination (`AsyncPageable<T>`) and page-envelope metadata

In alignment with the [Azure SDK Design Guidelines for .NET](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-pagination),
32 additional list/query operations across 14 connectors now return
`Azure.AsyncPageable<TItem>` instead of `Task<TPage>`:

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
