## What's Changed

### Unreleased

#### Added

- **Webhook callback models** - regenerated 25 existing connector clients from their notification schemas, including Teams chat messages, reactions, recordings, transcripts, and extensible dynamic webhook payloads. Registration models remain available. See [webhook callback schemas](https://github.com/Azure/Connectors-NET-SDK/blob/main/docs/triggers.md#webhook-callback-schemas).

#### Fixed

- **Array callback bodies** - `TriggerCallbackPayload<T>` accepts `{"body":[...]}` notifications alongside single-object and `value`-wrapped batches.

- **DataFlows SMS retry safety** - both `SendSmsAsync` and `SendSmsGetAsync` remain callable. The standard retry pipeline blocks retries for the GET send route on transient responses and transport failures, even with `RetryUnsafeHttpMethods` enabled. Ordinary GET reads retain retries; caller-supplied retry policies remain authoritative.

- **Connector error diagnostics** - operation labels use the HTTP method and resolved path without query strings, keeping SMS query values out of the exception operation and generated activity status. Outgoing requests are unchanged.

- **Azure IoT Central callback naming** - use `WebhookTimestamp` and the model factory parameter `webhookTimestamp`. The JSON field remains `timestamp`.

#### Breaking Changes

- **Shifts and Dynamics AX lists now page automatically** - all twelve Shifts list methods and `DynamicsAXClient.GetItemsAsync` return `AsyncPageable<T>` instead of `Task<TPage>`. Replace single-page `await` calls with `await foreach`, or use `AsPages()` for page boundaries and continuation-token resumption. Shifts page-model collections now use `Value`; page-model factories also change to expose the paging contract, including the Dynamics AX `nextLink` parameter. JSON `value` and `@odata.nextLink` fields are unchanged. These clients no longer stop after the first page.

- **PDF.co public API casing** - rename `PDFFromXLSXLSXAsync` to `PdfFromXlsXlsxAsync`, `PDFUnSearchableAsync` to `PdfUnsearchableAsync`, and `XLStoCSVAsync`, `XLStoHTMLAsync`, `XLStoJSONAsync`, `XLStoTXTAsync`, and `XLStoXMLAsync` to `XlsToCsvAsync`, `XlsToHtmlAsync`, `XlsToJsonAsync`, `XlsToTxtAsync`, and `XlsToXmlAsync`. Corresponding `Input` and `Response` types and model-factory members use the same corrected stems, including `PdfFromXlsXlsxResponse`. HTTP routes and JSON fields are unchanged.

- **Teams AI insight API casing** - rename `GetAiInsightAsync` to `GetAIInsightAsync` and `ListAiInsightsAsync` to `ListAIInsightsAsync`. `AiInsightResponse` and `AiInsightCollectionResponse` become `AIInsightResponse` and `AIInsightCollectionResponse`, including their `TeamsModelFactory` members. Update method calls, explicit model references, and factory calls; JSON fields and HTTP routes are unchanged.

### 0.15.0-preview.1 (2026-10-29)

#### Added

- **Zoho ZeptoMail generated client** — adds five public mail-agent, send-mail, template-mail, and analytics actions. `SendTemplateMailInput.MergeInfo` remains `List<JsonElement?>` so fixed string fields and arbitrary non-string merge values stay representable. The malformed Swagger type `ReplyToAddresss` is exposed as `ReplyToAddress` without changing the `reply_to` wire property.

- **DataFlows SMS and ServiceNow connector clients** - added `DataflowsSmsClient` (`dataflowssms`) for SMS messaging and account actions, and `ServiceNowClient` (`service-now`) for records, catalog items, knowledge articles, and binary attachment uploads. DataFlows SMS is distinct from Power Query Dataflows. Both clients include mocked request and serialization tests.

- **Azure DevOps (VSTS) and Power Query Dataflows connector clients** - added `VisualStudioTeamServicesClient` (`visualstudioteamservices`) and `DataflowsClient` (`dataflows`), with models, model factories, discovery operations, trigger registration metadata, and mocked HTTP and serialization tests. Azure DevOps also includes typed trigger callbacks. See [connector usage](https://github.com/Azure/Connectors-NET-SDK/blob/v0.15.0-preview.1/README.md#azure-devops-and-dataflows).

#### Fixed

- **Azure DevOps public naming** - use `VisualStudioTeamServices` for namespaces and client, factory, trigger, and enum type prefixes. Acronyms use .NET casing, including `Url`, `AccountUri`, `MemberUri`, `XmlForm`, and `HtmlLink`, without changing their JSON keys.

- **DataFlows SMS public naming** - use PascalCase namespaces, clients, models, model factories, and SMS methods, including `DataflowsSmsClient`, `SendSmsInput`, and `SendSmsAsync`. Wire identifiers and JSON property names are unchanged.

- **Azure DevOps callback and search binding** - search results preserve arbitrary JSON field values, pull-request commit truncation flags bind as booleans, and merge strategies bind as strings. Work-item detail lookup exposes only the production revision.

- **DataFlows SMS retry safety** - sending uses the POST action only; the GET send alternative is excluded to avoid automatic retries duplicating accepted messages.

- **ServiceNow attachment reads return their response bodies** - attachment listing and metadata retrieval now return JSON-preserving models, and `RetrieveAttachmentContentAsync` returns the downloaded bytes instead of discarding them.

- Add generator support and generated-client acceptance coverage for explicit scalar-only union map schemas, including mixed scalar serialization, invalid nested values, and independent nullability. Existing live connector contracts remain unchanged; open SQL query and Fin & Ops payloads are not reinterpreted as scalar-only. ([#280](https://github.com/Azure/Connectors-NET-SDK/issues/280))

- **Seismic Planner localization values are strongly typed** — `CustomPropertyValues.Localizations` is now `Dictionary<string, CustomPropertyDataDisplay>` instead of `JsonElement?`, preserving the Swagger `additionalProperties` value schema. ([#262](https://github.com/Azure/Connectors-NET-SDK/issues/262))

- **Microsoft Dataverse list rows now traverses every result page** — `GetItemsAsync` preserves the connector's `@odata.nextLink` continuation and returns an `AsyncPageable<Item>`. Live validation against a synthetic 5,001-row table retrieved all rows across ten connector pages without overlap. ([#208](https://github.com/Azure/Connectors-NET-SDK/issues/208))

- **Extensible enum member collisions now preserve every wire value** — distinct values that normalize to one C# identifier are allocated stable unique names instead of dropping later claimants. Sign-and-number collisions use readable names, such as `Etc/GMT+4` → `EtcGmtPlus4` and `Etc/GMT-4` → `EtcGmtMinus4`; ordinary delimiter hyphens still use the general numeric fallback. Values normalized to mandatory struct members (`Equals`, `GetHashCode`, `ToString`) use an available `Value`-suffixed name. ([#181](https://github.com/Azure/Connectors-NET-SDK/issues/181))

- **Existing connector clients refreshed from current Swagger** — updated Azure Monitor Logs, Google Drive, Jira, Key Vault, Plumsail, SigningHub, and Teams. See the contract and migration details below.

#### Changed

- **Azure Monitor Logs: new `PartialQueryError` model exposes partial-query error details** — `Table.Error` and `VisualizeResults.Error` now carry a typed `PartialQueryError` object with a `code` property (wire name `error`), available when the service returns partial results. Previously these properties were absent from the model.

- **Teams: new operations and models from Swagger expansion** — `ArchiveChannelAsync` archives a Teams channel and accepts an `ArchiveChannelInput`; `GetSubscriptionScopeSchemaAsync` returns the subscription-scope schema used by dynamic discovery. New response model `AsyncOperationResponse` carries an extensible `Status` enum for async operation tracking. New trigger-schema models `DynamicTranscriptTriggerRequest` and `DynamicRecordingTriggerRequest` support transcript and recording trigger subscription registration.

- **Google Drive: `BlobMetadata` gains `FolderId` and `FolderPath` properties** — newly added properties (wire names `FolderId`, `FolderPath`) expose the parent folder's identifier and path on file and folder metadata responses.

- **Jira: issue search uses current JQL and token pagination contracts** — `ListIssuesAsync` now accepts optional `jQLQuery` and `nextPageToken` inputs. `ListIssuesResponse` replaces `MaximumNumberOfItems` with `NextPageToken` and `IsLastPage`, matching the current search response.

- **SigningHub: over 60 previously opaque `JsonElement?` properties now have typed models** — properties such as `CertifyPolicyResponse.Certify`, `GetDocumentDetailsResponse.Certify`, `GetDocumentDetailsResponse.Template`, and many authentication, permission, access-duration, and signature-field objects are now strongly typed with dedicated model classes. Consumer code that read these properties as raw `JsonElement` and called `.GetProperty()` must switch to the typed accessors; see the Breaking Changes section.

#### Breaking Changes

- **Seismic Planner `CustomPropertyValues.Localizations` now exposes typed values** — callers that previously inspected the nullable raw `JsonElement` must use the `Dictionary<string, CustomPropertyDataDisplay>` entries directly. For example, replace `Localizations.Value.GetProperty("en-US").GetProperty("name").GetString()` with `Localizations["en-US"].Name`. ([#262](https://github.com/Azure/Connectors-NET-SDK/issues/262))

- **Automatic retries now default to methods that are safe to replay** — `GET`, `HEAD`, `OPTIONS`, and `TRACE` retain the configured Azure.Core retry behavior. `POST`, `PUT`, `PATCH`, `DELETE`, and all other methods make one attempt by default for both transient responses and ambiguous transport failures. Set `ConnectorClientOptions.RetryUnsafeHttpMethods` to `true` only when replaying side effects is acceptable. A caller-supplied `RetryPolicy` remains authoritative, and callers that construct a standalone `ConnectorHttpClient` pipeline own its retry semantics. See [Retry safety](https://github.com/Azure/Connectors-NET-SDK/blob/v0.15.0-preview.1/docs/retry-safety.md). ([#269](https://github.com/Azure/Connectors-NET-SDK/issues/269))

- **Microsoft Dataverse `GetItemsAsync` now returns `AsyncPageable<Item>`** — callers that previously awaited one `ItemsList` page must enumerate the pageable with `await foreach`, or call `AsPages()` when page boundaries and continuation tokens are required. This prevents the connector's `@odata.nextLink` value from being discarded after the first page. ([#208](https://github.com/Azure/Connectors-NET-SDK/issues/208))

- **Google Drive `CreateFileAsync`: parameter renamed, route updated to v2, query key changed** — the `folderPath` parameter is now named `folder`, and the method calls `/datasets/default/v2/files` with the query key `folderId` instead of the previous `/datasets/default/files` with `folderPath`. Callers using the named argument `folderPath:` must rename it to `folder:`. Callers using positional arguments compile cleanly but will route to the v2 endpoint automatically.

  Migration: rename `folderPath:` to `folder:` — the underlying wire semantics changed (folder path → folder ID), so pass the folder's unique identifier rather than its path string.

- **Jira `ListIssuesResponse` pagination changed** — `MaximumNumberOfItems` is removed. Use `NextPageToken` and `IsLastPage` to continue paging. `ListIssuesAsync` callers can now pass `jQLQuery` and `nextPageToken`; the connector no longer injects the previous fixed ten-year JQL filter.

- **Plumsail Timezone enum: signed Etc/GMT members now have semantic Plus/Minus names** — existing members are renamed according to the wire value they already represented: for example, `EtcGMT4` (`Etc/GMT-4`) becomes `EtcGmtMinus4`, while `EtcGMT1` (`Etc/GMT+1`) becomes `EtcGmtPlus1`. Previously dropped counterparts are added under the opposite semantic name, including `EtcGmtPlus4` for `Etc/GMT+4` and `EtcGmtMinus1` for `Etc/GMT-1`. For the `Etc/GMT` three-way zero group, existing `EtcGMT0` (`Etc/GMT+0`) becomes `EtcGmtPlus0`, with new `EtcGmtMinus0` and `EtcGmt0` members. The plain `GMT` group changes similarly: existing `Gmt0` (`GMT+0`) becomes `GmtPlus0`, while new `GmtMinus0` and `Gmt0` members represent `GMT-0` and `GMT0`. Singleton entries without a colliding counterpart, such as `EtcGMT13` and `EtcGMT14`, are unchanged. All underlying wire strings are preserved. ([#181](https://github.com/Azure/Connectors-NET-SDK/issues/181))

- **Google Tasks and PDF.co operation name typos corrected** — `CraeteTaskAsync` is now `CreateTaskAsync`, and `PDFSerarchTextAsync` is now `PDFSearchTextAsync`. PDF.co callers must also rename `PDFSerarchTextInput` and the corresponding model-factory method to `PDFSearchTextInput`. Connector routes and wire payload names are unchanged.

- **SigningHub: over 60 properties changed from `JsonElement?` to typed model classes** — properties including `CertifyPolicyResponse.Certify` (→ `CertifyPermissionResponse`), `GetDocumentDetailsResponse.Certify` (→ `DocumentCertifyResponse`), `GetDocumentDetailsResponse.Template` (→ `DocumentTemplateResponse`), and many authentication, permission, access-duration, and signature-field properties are now strongly typed. Source code that called `.GetProperty(...)` on these `JsonElement?` properties no longer compiles; switch to the typed model accessors.

- **Uppercase extensible-enum values now retain PascalCase word boundaries** — Key Vault members `Rsaoaep` and `Rsaoaep256` become `RsaOaep` and `RsaOaep256`. Plumsail members `Wsu` and `Nzchat` become `WSu` and `NzChat`. SigningHub corrects 20 members, including `Authenticationpassword` → `AuthenticationPassword`, `AUTHENTICATIONOIdC` → `AuthenticationOidc`, `Companylogo` → `CompanyLogo`, and `VALId` → `Valid`. Exact wire values are unchanged.

### 0.14.0-preview.1 (2026-08-12)

Condensed historical notes; see the [full changelog](https://github.com/Azure/Connectors-NET-SDK/blob/v0.15.0-preview.1/CHANGELOG.md) for detailed API, model, and generator references.

#### Breaking Changes

- **Wire-property collisions fixed in seven clients** - Etsy, GitHub, Office 365 Outlook, Plumsail Documents, SigningHub, Twitter, and WordPress retain previously lost fields. GitHub `PullRequest.DiffUrl` binds `diff_url`; `PullRequest.PullRequestDiffUrl` binds `comments_url`. Office 365 `MailTipsClientReceive.ExternalMemberCount` binds `externalMemberCount`, and `IsModerated` is `bool?`, not `int?`. Plumsail `AddPowerAutomateWebhookData.ProcessId` binds `processId`; `ProcessName` binds `hookUrl`. Migrate values from the three previously misbound properties. Added Etsy `BuyerTotalAdjustmentAmount`, SigningHub `DocumentId2` for `document_id`, Twitter `CreatedAtIso` on both tweet models, and WordPress `Id2` for `guid` on both post models.
- **DocuSign embedded-signing discovery updated** - rename `StaticResponseForEmbeddedSigningSchemaV2Async` to `StaticResponseForEmbeddedSigningSchemaAsync`; it requires `isThisAnPersonSigner` and calls `/embeddedSigning_schema_v2`. The old `/embeddedSigning_schema` route is no longer retained.
- **Unreferenced discovery helpers removed** - removed `ExcelOnlineBusiness.GetRawAndFormattedTableAsync`; Google Drive `GetTableAsync` and `GetTablesAsync`; `PowerBI.GetPowerBiButtonClickedOutputsAsync`; and Teams `GetAdaptiveCardInputMetadataAsync`, `GetFlowContinuationSubscriptionOutputMetadataAsync`, `GetNotificationInputMetadataAsync`, `GetCardResponseTriggerOutputsMetadataAsync`, `GetComposeMessageTriggerOutputsMetadataAsync`, `GetSelectedMessageTriggerOutputsMetadataAsync`, and `GetSubscriptionScopeSchemaAsync`. Their unreferenced models and model-factory methods were removed too; see the full changelog for the type inventory.
- **Versioned model identities preserved** - distinct retained wire shapes keep their version affixes instead of collapsing onto one model. Update affected Azure IoT Central, Azure AD, Office 365, Pipedrive, and Planner type references. For example, Azure IoT Central `Device` became `DeviceV1` and `DeviceV2`, exposing `organizations`. Other regenerated clients incorporate earlier generator fixes.
- **Purview parameter spelling corrected** - SQL Server and Azure Blob Storage rename `purviewAcccountName` to `purviewAccountName` (12 SQL and 20 Blob occurrences). Update named arguments; positional calls and the wire query key are unchanged.
- **SQL procedure metadata routes separated** - for scoped calls, rename `GetProceduresAsync` / `GetProcedureAsync` to `GetProceduresV2Async` / `GetProcedureV2Async`; the unsuffixed names now address the default dataset. `GetTableAsync` and `GetPassThroughNativeQueryMetadataAsync` retain their scoped V2 routes. No compatibility overloads are provided. ([#222](https://github.com/Azure/Connectors-NET-SDK/issues/222))
- **Cloudmersive Convert multipart operations removed** - removed 75 unsupported `formData` operations and their models; 53 non-multipart operations remain. Examples include `ConvertDocumentAutodetectGetInfoAsync`, `ConvertDocumentAutodetectToPdfAsync`, `ConvertDocumentDocxToPdfAsync`, `ConvertDocumentPdfToDocxAsync`, `ConvertImageGetImageInfoAsync`, `MergeDocumentPdfAsync`, and `SplitDocumentPdfByPageAsync`.
- **DocuSign deprecated operations removed** - removed `CreateEnvelopeFromTemplateAsync`, `ListEnvelopesAsync`, `SalesCopilotListEnvelopesAsync`, `ScpGetEmailSummaryAsync`, `ScpGetKeySalesAsync`, `ScpGetRelatedActivitiesAsync`, and `ScpGetRelatedRecordsAsync`. `CreateEnvelopeFromTemplateNoRecipientsAsync` remains available.
- **Zoho Sign multipart upload removed** - `CreateDocumentAsync` requires an unsupported multipart/form-data `file` parameter and is no longer generated.
- **SigningHub uploads require raw bytes** - pass `byte[] input` to `AttachmentUploadAttachmentAsync` and `DocumentsUploadStreamAsync`. Requests send unchanged bytes with `Content-Type: application/octet-stream`, not JSON/base64.
- **Generated parameter names follow Swagger metadata** - update named arguments where necessary; DocuSign `GetDocgenFormFieldsAsync` now uses `accountId` and `envelopeId`. Positional calls are unaffected.
- **Docuware multipart operations removed** - removed `StoreToFileCabinetAsync`, `ImportToDocumentTrayAsync`, `AppendFileAsync`, `DeleteFileAsync`, and `ReplaceFileAsync`, plus their response models/factories. Search, metadata, download, field, and stamp operations remain available.
- **Teams webhook contracts removed** - current Swagger no longer exposes `DynamicTranscriptTriggerRequest`, `DynamicRecordingTriggerRequest`, `TeamsTriggerOperations.OnTranscriptTrigger`, or `TeamsTriggerOperations.OnRecordingTrigger`.
- **Azure Queues response shape corrected** - `Messages.QueueMessagesList` changes from `List<QueueMessage>` to a wrapper; read messages through `response.QueueMessagesList.QueueMessage`. Rename `QueueMessage.TimeNextVisible` to `NextVisibleTime`. `DequeueCount` remains available through the connector policy.
- **DocumentDB synthetic envelope removed** - `QueryDocumentsResponse.AdditionalProperties` and its factory parameter were removed. Read dynamic fields from each item's `ObjectWithoutType.AdditionalProperties` in `QueryDocumentsResponse.Documents`.
- **Event Hubs required parameters enforced** - supply non-null `partitionKey` to `SendEventsAsync` and `contentType` to `GenerateEventSchemaAsync`.
- **Microsoft Forms discovery return type corrected** - `GetQuestionsAsync` returns `Task`, not `Task<List<JsonElement?>>`, because Swagger declares no success response schema.
- **Word Online Business drive discovery simplified** - `GetDrivesAsync` no longer exposes `source`; it sends the declared default `source=me`.

#### Added

- **CloudConvert typed client** - four public conversion operations and five internal discovery helpers retained through `x-ms-dynamic-properties`.
- **MailChimp A/B split wait fields preserved** - `ABSplitOpts.WaitUnits` and `WaitTime` bind the distinct `wait_units` and `wait_time` fields. ([#231](https://github.com/Azure/Connectors-NET-SDK/issues/231))
- **Nested discovery references retained** - Azure Automation, Azure Monitor Logs, Elfsquad Data, Formstack Forms, Monday, Typeform, and Microsoft Defender ATP gain referenced helpers: `GetRunbookAsync`, `GetTimeRangeSelectionControlAsync`, `GetTriggerSchemaAsync`, `GetFormSchemaAsync`, three Monday column-schema helpers, `GetSchemaAsync`, and `AdvancedHuntingSchemaAsync`, plus five models and four factory methods. Elfsquad and Formstack helpers return `Task` where Swagger has no response schema; Azure Automation's single-runbook route retains the owner-declared `RunbookListResults` type.
- **Zendesk public operations restored** - restored `GetTablesAsync`, `GetItemsAsync`, `GetItemAsync`, `GetTableAsync`, and the typed `GetOnNewItems` trigger alongside `GetOnUpdatedItemsV2`. Internal V2 siblings no longer displace the public operations or leave dangling dynamic-value references. ([#220](https://github.com/Azure/Connectors-NET-SDK/issues/220))
- **SQL default-dataset procedures restored** - `GetProceduresAsync` and `GetProcedureAsync` address `/datasets/default/...`; `GetProceduresV2Async` and `GetProcedureV2Async` remain scoped. `GetTableAsync`, `GetPassThroughNativeQueryMetadataAsync`, and `GetTablesAsync` retain versioned routes because default siblings are unreachable or deprecated. ([#222](https://github.com/Azure/Connectors-NET-SDK/issues/222))
- **New connector operations** - DocuSign adds `GetDocGenTemplateTabsAsync` and `GetOrganizationsAsync`; Teams adds `RemoveMemberFromChatAsync` and `UpdateChannelPropertiesAsync`; SharePoint adds `GetDayOfWeekOptionsAsync`.

#### Changed

- **Public methods precede discovery helpers** - customer-facing operations are emitted first. Pipedrive and Plumsail change ordering only; their operation/model sets are unchanged.
- **Generated summaries use each type's own definition** - Zendesk `Table`, `Item`, and three metadata types gain meaningful descriptions; 17 SQL Server summaries improve. Documentation only; API and wire shapes are unchanged.
- **Existing clients regenerated** - 97 clients use the updated generator, which replaces legacy ARMClient authentication with Azure Identity, preserves valid ARM JSON before malformed-response fallbacks, excludes deprecated operations, and handles connectors advertising JSON and multipart content types. Office 365, Docuware, and SigningHub regenerate from current ARM exports.
- **Generated documentation spelling corrected** - fixed `seperated` / `Comma-seperated` and the malformed plural possessive in Swagger-derived XML documentation.
- **Connector owner contracts preserved** - Azure Queues retains nested response shapes; Event Hubs required parameters, Microsoft Forms and Word Online Business discovery methods, and DocumentDB dynamic fields follow owner Swagger.

#### Fixed

- **Invariant-culture wire formatting** - 98 clients now format path/query values independently of `CurrentCulture` before URI escaping: `3.14` remains `3.14` under `de-DE`, not `3%2C14`. The ISO DateTime converter also uses `InvariantCulture`; CA1305 is build-enforced. ([#200](https://github.com/Azure/Connectors-NET-SDK/issues/200))

### 0.13.0-preview.1 (2026-07-09)

#### Added

- **Typed and binary trigger callbacks** - `ConnectorTriggerPayload.Read<TPayload>` / `ReadAsync<TPayload>` parse `string` or `Stream` metadata bodies case-insensitively (for example, OneDrive `OnNewFilesV2`). `TryReadBinaryContent` / `ReadBinaryContentAsync` decode base64 JSON-string bodies such as `OnNewFileV2`; `TryReadBinaryContent` returns `false` for malformed JSON. Stream readers leave caller-owned input open and apply an overridable 100 MB body limit (`DefaultMaxBodySizeBytes`). ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))
- **Dataverse client** - `CommondataserviceClient` (`commondataservice`) supports legacy CDM environment/table discovery, row CRUD, attachments, relationships, choices/metadata, and `GetNextPageAsync` through Connector Namespace. Dataset values are full environment URLs (for example, `https://contoso.crm.dynamics.com`), double URL-encoded per `x-ms-url-encoding: "double"`. Modern Web API routes (`/api/data`, `/getorgs`) return HTTP 404 through this runtime and are excluded, along with redundant OData key-syntax routes.

#### Changed

- **Actionable binary-callback errors** - `TriggerCallbackBodyConverter<T>` now directs callers using a metadata payload type for a binary JSON-string body to `ConnectorTriggerPayload.TryReadBinaryContent` / `ReadBinaryContentAsync`. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))

### 0.12.0-preview.1 (2026-06-02)

- Breaking: Renamed 1,460 `ConnectorNames.*` constants to display-name PascalCase: `Googledrive` -> `GoogleDrive`, `Microsoftteams` -> `MicrosoftTeams`, `Office365` -> `Office365Outlook`. Update references. (#170)
- Breaking: Optional value-type parameters use `int?`, `bool?`, and `double?`; `null` means unspecified. Update overriding subclass signatures. (#180)
- Breaking: Dynamic model properties use `JsonElement?`, not `object`; pre-serialize arbitrary values. (#157)
- Breaking: Output-only properties use `{ get; init; }`; construct with object initializers or `*ModelFactory` classes. (#161)
- Breaking: Removed `IConnectorClient`; `ConnectorClientBase` implements `IDisposable` directly. (#183)
- Breaking: Removed `Teams.OnGroupMemberChangeResponseItem`; membership callbacks use `TriggerCallbackPayload<object>`. (#170)
- Added `TeamsOnNewChannelMessageTriggerPayload`, `TeamsOnNewChannelMessageMentioningMeTriggerPayload`, `TeamsOnTeamMemberRemovedTriggerPayload`, `TeamsOnTeamMemberAddedTriggerPayload`, and `TeamsTriggers.Operations`. (#170)
- Added per-connector OpenTelemetry `ConnectorActivitySource` tracing (for example, `Azure.Connectors.Sdk.teams`); subscribe to `Azure.Connectors.Sdk.*`. (#183)
- `ConnectorException` parses service error codes into `RequestFailedException.ErrorCode`. (#180)
- Hide inherited `Object` methods with `[EditorBrowsable(EditorBrowsableState.Never)]`. (#160)
- Regenerated 96 clients with copyright headers, base-chained mock constructors, and null guards. (#158, #159, #175)

### 0.11.0-preview.1 (2026-05-15)

- Fixed: `TriggerCallbackBody<T>` handles batch and single-item callbacks, avoiding zero-item processing with splitOn. (#149)
- Breaking: `TriggerCallbackPayload<T>.Body` is init-only; `TriggerCallbackBody<T>.Value` is `IReadOnlyList<T>?` with an internal setter. Use `ConnectorModelFactory` in tests.
- Breaking: Removed CamelCase JSON policy; properties without `[JsonPropertyName]` serialize as PascalCase. (#84, #85)
- Breaking: `AzuremonitorlogsClient` -> `AzureMonitorLogsClient`; `Office365usersClient` -> `Office365UsersClient`, including namespaces, DI, factories, and `ConnectorNames`. (#126)
- Breaking: `IPageable<T>` and JSON converters are internal; `ConnectorClientBase.CreatePageable` is private protected. (#124, #127)
- Added `(Uri, TokenCredential)` constructors without `ClientOptions`, plus `ConnectorHttpClient` mocking. (#123, #125)
- Added 36 clients in batches 5-6, including Azure AD, Azure IoT Central, Outlook, Service Bus, Box, DocuSign, GitHub, Google Drive, Jira, Salesforce, SQL, and Trello.

### 0.10.0-preview.1 (2026-05-11)

- Breaking: Removed CamelCase policy from `ConnectorClientBase.JsonOptions`; properties without `[JsonPropertyName]` serialize as PascalCase.
- Breaking: `AzuremonitorlogsClient` -> `AzureMonitorLogsClient`; `Office365usersClient` -> `Office365UsersClient`, including namespaces, DI, and factories.
- Breaking: `IPageable<T>` and JSON converters are internal; `ConnectorClientBase.CreatePageable` is private protected.
- Added `(Uri, TokenCredential)` constructors without `ClientOptions` to the base class and generated clients.
- Added `ConnectorHttpClient` mocking: protected parameterless constructor and virtual `SendAsync`.
- Added 48 clients in four batches, including ExcelOnline, AzureEventGrid, Yammer, WdatpClient, AzureAutomation, AzureDataFactory, KeyVault, and PowerBI.
- Regenerated the original 12 clients with PascalCase name overrides.

### 0.9.0-preview.1 (2026-05-08)

- Breaking: Constructors use `Uri`; the default credential is `ManagedIdentityCredential(SystemAssigned)`, not `DefaultAzureCredential`. Explicit credential parameters are required.
- Breaking: Output-only properties have internal setters; use per-connector model factories in tests.
- Breaking: Removed public `ExceptionExtensions`, `HttpExtensions`, `RetryPolicy`, and `ConnectorResponse<T>`.
- Breaking: `Microsoft.Azure.Connectors.*` namespaces become `Azure.Connectors.Sdk.*`.
- Breaking: `ConnectorClientOptions` derives from `Azure.Core.ClientOptions`; removed Polly and constructor `HttpClient` parameters.
- Added extensible Swagger enums, DI extensions, and per-connector model factories.
- Added Azure Monitor Logs; removed deprecated Azure Log Analytics.

### 0.8.0-preview.1 (2026-04-30)

- Added Office 365 Users, Azure Log Analytics, SMTP, Azure Blob Storage, and IBM MQ clients.
- Added OpenTelemetry `ActivitySource` tracing.

### 0.7.0-preview.1 (2026-04-30)

- Added `IAsyncEnumerable<T>` auto-pagination.
- Breaking: Paginated methods return `ConnectorPageable<TPage, TItem>`, not `Task<TPage>`.
- Updated `ManagedIdentityCredential` to the `ManagedIdentityId` API.

### 0.6.0-preview.1

- First NuGet.org preview.

### 0.5.0-preview.1 (2026-04-15)

- Added MS Graph Groups and Users: 7 actions.
- Added Teams constructor, disposal, mocked-API, error, and serialization tests.

### 0.4.0-preview.1 (2026-04-09)

- Added OneDrive for Business: 22 actions and 4 triggers.

### 0.3.0-preview.1 (2026-04-09)

- Breaking: Removed generated operation version suffixes (V2/V3/V4).
- Breaking: Triggers use natural-English names with an On prefix.
- Breaking: Types use per-connector aliases.
- Added trigger operation constants and definition pruning.

### 0.2.0-preview.1 (2026-04-07)

- Added Azure Data Explorer (Kusto).
- Added a PR template, governance docs, CI coverage, and standard Microsoft OSS files.
- Updated Microsoft.Extensions.Http, test SDK, coverlet, and GitHub Actions.

### 0.1.0-preview.1 (2025-12-19)

- Initial SDK: `ConnectorClientBase`, `IConnectorClient`, and `ConnectorClientOptions`.
- Added `ManagedIdentityTokenProvider` and `ConnectionStringTokenProvider`.
- Added configurable HTTP retry policies.
- Added the generated Office 365 client.
