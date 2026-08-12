## What's Changed

### Unreleased

#### Breaking Changes

- **Seven clients preserve wire properties that previously collided after C# name generation** — regenerated Etsy, GitHub, Office 365 Outlook, Plumsail Documents, SigningHub, Twitter, and WordPress from the merged collision resolver in AzureUX-BPM PR 16763267. GitHub `PullRequest.DiffUrl` now represents `diff_url`, while `PullRequest.PullRequestDiffUrl` represents the previously dropped `comments_url`. Office 365 `MailTipsClientReceive.ExternalMemberCount` now represents `externalMemberCount`, and `IsModerated` is corrected from `int?` to `bool?` for `isModerated`. Plumsail `AddPowerAutomateWebhookData.ProcessId` now represents `processId`, while `ProcessName` represents the previously dropped `hookUrl`. Etsy adds `BuyerTotalAdjustmentAmount`; SigningHub adds `DocumentId2` for `document_id`; Twitter adds `CreatedAtIso` to both tweet models; and WordPress adds `Id2` for `guid` to both post models. Callers using the three previously misbound GitHub, Office 365, or Plumsail properties must move values to the corrected properties.

- **Unreferenced discovery helpers and their models are no longer retained** — discovery reachability now starts from the selected public and trigger surface and continues transitively through retained helpers. This removes methods that survived only because the generator scanned every Swagger definition: `ExcelOnlineBusiness.GetRawAndFormattedTableAsync`; `GoogleDrive.GetTableAsync` and `GetTablesAsync`; `PowerBI.GetPowerBiButtonClickedOutputsAsync`; and Teams' `GetAdaptiveCardInputMetadataAsync`, `GetFlowContinuationSubscriptionOutputMetadataAsync`, `GetNotificationInputMetadataAsync`, `GetCardResponseTriggerOutputsMetadataAsync`, `GetComposeMessageTriggerOutputsMetadataAsync`, `GetSelectedMessageTriggerOutputsMetadataAsync`, and `GetSubscriptionScopeSchemaAsync`.

  The removed public models are `GoogleDrive.Models.TableMetadata`, `TableCapabilitiesMetadata`, `TableSortRestrictionsMetadata`, `TableFilterRestrictionsMetadata`, `TableSelectRestrictionsMetadata`, `ObjectEntity`, `TablesList`, and `Table`; `PowerBI.Models.PowerBiButtonClickedOutputs`; and `Teams.Models.SelectedMessageTriggerMetadata`, `ComposeMessageTriggerMetadata`, and `CardResponseTriggerMetadata`. Their model-factory methods are also removed. (AzureUX-BPM PR 16671915)

- **All connector clients regenerated; version-affixed models are no longer collapsed onto one name** — the generator stripped a version affix from a definition name without knowing which definitions a connector retains, so `FooV1` and `FooV2` both claimed `Foo` and only one type was emitted. The dropped one produced a model that compiled and looked complete while omitting fields the service returns. Names are now decided against the retained definition set: a single claimant still strips the affix, several claimants describing the same wire shape still collapse to one type, and claimants with differing shapes keep their affixes so no shape is lost.

  `AzureIoTCentral` is the clearest case and shipped separately in #225: `Device` became `DeviceV1` and `DeviceV2`, making the `organizations` property reachable. In this regeneration the same fix reaches `AzureAD`, `Office365`, `Pipedrive` and `Planner`. Callers referencing a previously collapsed model name on those clients must move to the affixed name.

  The remaining 71 regenerated clients carry accumulated output from generator fixes that merged after their last regeneration, rather than new behaviour introduced here. (AzureUX-BPM PR 16618864)

- **`purviewAcccountName` parameter corrected to `purviewAccountName` on SQL Server and Azure Blob Storage** — the shared Purview query parameter declares its `name` correctly but carries a triple-c typo in its `x-ms-summary`, and the generator derives the C# identifier from the summary. The misspelling therefore reached the public API surface while the wire query key was always correct, so this changes the identifier only, not request behaviour. Callers passing the parameter positionally are unaffected; callers using a named argument must rename it. Affects 12 occurrences in `SqlExtensions` and 20 in `AzureBlobExtensions`. (AzureUX-BPM PR 16639935)

- **SQL Server procedure metadata methods now expose distinct default and scoped routes** — the unversioned procedure operations own the plain method names, matching the Python SDK. Existing callers to the scoped procedure routes must rename:

  | Before | After |
  | --- | --- |
  | `GetProceduresAsync(serverName, databaseName)` | `GetProceduresV2Async(serverName, databaseName)` |
  | `GetProcedureAsync(serverName, databaseName, procedureName)` | `GetProcedureV2Async(serverName, databaseName, procedureName)` |

  `GetTableAsync` and `GetPassThroughNativeQueryMetadataAsync` continue to bind to the server- and database-scoped V2 routes because the default-dataset siblings are not reachable from the selected public surface. Compatibility overloads are deliberately not provided: generated code is never hand-edited, and unreachable internal Swagger routes are not retained. ([#222](https://github.com/Azure/Connectors-NET-SDK/issues/222), AzureUX-BPM PRs 16576205 and 16671915)

- **Cloudmersive Convert multipart operations removed** — 75 current managed-connector operations use Swagger 2.0 `formData`/multipart payloads that the SDK transport cannot compose, so the regenerated client no longer exposes those methods or their models. This includes `ConvertDocumentAutodetectGetInfoAsync`, `ConvertDocumentAutodetectToPdfAsync`, `ConvertDocumentDocxToPdfAsync`, `ConvertDocumentPdfToDocxAsync`, `ConvertImageGetImageInfoAsync`, `MergeDocumentPdfAsync`, and `SplitDocumentPdfByPageAsync`. The 53 non-multipart operations remain generated. (#210)
- **DocuSign deprecated and removed operations are no longer generated** — removed `CreateEnvelopeFromTemplateAsync`, `ListEnvelopesAsync`, `SalesCopilotListEnvelopesAsync`, `ScpGetEmailSummaryAsync`, `ScpGetKeySalesAsync`, `ScpGetRelatedActivitiesAsync`, and `ScpGetRelatedRecordsAsync`. `CreateEnvelopeFromTemplateNoRecipientsAsync` remains available. (#210)
- **Zoho Sign `CreateDocumentAsync` removed** — the operation remains in current managed-connector Swagger but requires a multipart/form-data `file` parameter, which the SDK transport cannot compose. (#210)
- **SigningHub upload methods now require file bytes** — `AttachmentUploadAttachmentAsync` and `DocumentsUploadStreamAsync` now require a `byte[] input` argument. Binary Swagger bodies use the SDK's raw request path, which sends the bytes unchanged with `Content-Type: application/octet-stream` instead of JSON/base64 serialization. Existing callers must provide the attachment or document bytes. (#210)
- **Generated method parameter names now follow current Swagger metadata** — named-argument callers may need updates; for example, DocuSign `GetDocgenFormFieldsAsync` now uses `accountId` and `envelopeId`. Positional calls are unaffected. (#210)
- **Docuware multipart-only file operations removed** — `StoreToFileCabinetAsync`, `ImportToDocumentTrayAsync`, `AppendFileAsync`, `DeleteFileAsync`, and `ReplaceFileAsync`, together with their response models/factories, are no longer generated because the SDK transport cannot compose the required Swagger 2.0 `formData` payloads. Other Docuware search, metadata, download, field, and stamp operations remain available. (#210)
- **Teams transcript and recording webhook trigger contracts removed** — current connector Swagger no longer exposes `DynamicTranscriptTriggerRequest`, `DynamicRecordingTriggerRequest`, `TeamsTriggerOperations.OnTranscriptTrigger`, or `TeamsTriggerOperations.OnRecordingTrigger`. (#210)
- **Azure Queues message response now matches the connector wire shape** — `Messages.QueueMessagesList` changed from `List<QueueMessage>` to a `QueueMessagesList` wrapper whose `QueueMessage` property contains the list. `QueueMessage.TimeNextVisible` was renamed to `NextVisibleTime` from the owner Swagger summary. `DequeueCount` remains available because Azure Storage returns it and the connector policy passes it through, although AAPT Swagger omits it. Access messages through `response.QueueMessagesList.QueueMessage`. (#210)
- **DocumentDB query response no longer exposes a synthetic envelope property** — `QueryDocumentsResponse.AdditionalProperties` and the corresponding model-factory parameter were removed. Dynamic document fields are captured by `ObjectWithoutType.AdditionalProperties` on each item in `QueryDocumentsResponse.Documents`, matching the V5 response contract. (#210)
- **Event Hubs owner-required parameters are now required** — `SendEventsAsync` requires `partitionKey`, and `GenerateEventSchemaAsync` requires `contentType`. Callers must pass non-null values declared as required by the connector owner Swagger. (#210)
- **Microsoft Forms schema helper no longer returns a fabricated response** — internal discovery method `GetQuestionsAsync` now returns `Task` instead of `Task<List<JsonElement?>>` because owner Swagger declares no success response schema. (#210)
- **Word Online Business drive discovery hides its internal source parameter** — internal discovery method `GetDrivesAsync` no longer exposes `source`; it sends the owner-declared default `source=me`. (#210)

#### Added

- **Discovery helpers retained from nested trigger and response schemas** — regenerated Azure Automation, Azure Monitor Logs, Elfsquad Data, Formstack Forms, Monday, Typeform, and Microsoft Defender ATP clients now include helpers pinned by nested trigger and response schemas in connector Swagger. Added `GetRunbookAsync`, `GetTimeRangeSelectionControlAsync`, `GetTriggerSchemaAsync`, `GetFormSchemaAsync`, three Monday column-schema helpers, Typeform's current `GetSchemaAsync`, and `AdvancedHuntingSchemaAsync`, plus five response/request models and four model-factory methods. Elfsquad Data's `GetTriggerSchemaAsync` and Formstack Forms' `GetFormSchemaAsync` return `Task` because owner Swagger declares no success response schema. Azure Automation's single-runbook route is likewise typed as `RunbookListResults` because that is the response definition declared by owner Swagger for both runbook routes. (AzureUX-BPM PR 16671915)

- **Zendesk regains four operations lost to internal versioned siblings** — `GetTablesAsync`, `GetItemsAsync`, `GetItemAsync` and the `GetTableAsync` discovery method are generated again, and the public `GetOnNewItems` trigger is registered alongside `GetOnUpdatedItemsV2` with its own typed callback payload. Version-collision resolution previously ran across the whole operation set, so each `x-ms-visibility: internal` `V2` sibling displaced its public counterpart and was then dropped as unreferenced. Surviving methods also carried a `[DynamicValues("GetTables")]` reference to an operation no longer present in the client. ([#220](https://github.com/Azure/Connectors-NET-SDK/issues/220), AzureUX-BPM PR 16576205)
- **SQL Server regains two default-dataset procedure discovery routes** — `GetProceduresAsync` and `GetProcedureAsync` now address `/datasets/default/...` and are joined by `GetProceduresV2Async` and `GetProcedureV2Async` for the server- and database-scoped routes. Each pair is a distinct reachable contract that previously collapsed onto the V2 route alone. `GetTableAsync`, `GetPassThroughNativeQueryMetadataAsync`, and `GetTablesAsync` deliberately continue to target versioned routes because their unversioned siblings are not reachable or are deprecated. ([#222](https://github.com/Azure/Connectors-NET-SDK/issues/222), AzureUX-BPM PRs 16576205 and 16671915)
- **New operations from current managed connector Swagger** — DocuSign adds `GetDocGenTemplateTabsAsync` and `GetOrganizationsAsync`; Teams adds `RemoveMemberFromChatAsync` and `UpdateChannelPropertiesAsync`; SharePoint adds `GetDayOfWeekOptionsAsync`. (#210)

#### Changed

- **Discovery methods are now emitted after the public surface** — generated clients list customer-facing operations first, then the internal discovery methods retained for dynamic values and dynamic schema. Pipedrive and Plumsail change by this ordering alone; their operation and model sets are unchanged. (AzureUX-BPM PR 16576205)

- **Generated types are documented from their own definition instead of from whatever reached them** — a type's summary was derived from the property or operation that happened to reach it first, which produced meaningless text and made the documentation depend on declaration order. A type reached through a bare envelope property such as `value` was summarized as `Item in value`, naming the wrapper rather than the element, and raw Swagger keys such as `x-ms-capabilities` and `sortRestrictions` were emitted as public API documentation. Zendesk `Table` and `Item` now carry their own names, three Zendesk metadata types gain real descriptions, and seventeen SQL Server summaries improve, for example `Item in List of datasets` becomes `Represents a database.` and `Response for GetTablesForDeleteItem` becomes `Represents a list of tables.` Documentation only; no operation, model, or wire change. (AzureUX-BPM PR 16600581)

- **Regenerated all 97 existing connector clients** using AzureUX-BPM PR 16421737. Generation now uses Azure Identity instead of the legacy ARMClient executable, preserves valid ARM JSON before applying malformed-response fallbacks, filters operations marked `deprecated`, and correctly handles connectors that advertise both JSON and multipart content types. Office 365, Docuware, and SigningHub now regenerate successfully from current ARM exports. (#210)
- **Corrected generated Swagger documentation text** — fixed `seperated`/`Comma-seperated` and `lists’s` in generated XML documentation. (#210)
- **Connector-specific corrections now preserve owner contracts** — Azure Queues receives named types for its nested message response without flattening the wire shape. Current AAPT connector Swagger remains authoritative for Event Hubs required parameters, Microsoft Forms and Word Online Business internal discovery methods, and DocumentDB per-document dynamic fields. (#210)

#### Fixed

- **Connector path and query values now use invariant-culture wire formatting** — regenerated all 98 shipped clients from AzureUX-BPM PR 16770557 so numeric and other formattable parameters no longer depend on `CurrentCulture` before URI escaping. For example, a `double` value of `3.14` now remains `3.14` under `de-DE` instead of becoming `3%2C14`. The hand-written ISO DateTime converter now parses and formats with `InvariantCulture`, and CA1305 is build-enforced to prevent regressions. ([#200](https://github.com/Azure/Connectors-NET-SDK/issues/200))

### 0.13.0-preview.1 (2026-07-09)

#### Added

- **`ConnectorTriggerPayload` helper to read trigger callbacks** — turns a raw Connector Namespace trigger callback (`string` or `Stream`) into a typed payload or decoded file bytes without per-function boilerplate. `Read<TPayload>` / `ReadAsync<TPayload>` deserialize metadata triggers (e.g. OneDrive `OnNewFilesV2`) with case-insensitive property matching, so camelCase wire fields bind correctly instead of silently yielding all-`null` items. `TryReadBinaryContent` / `ReadBinaryContentAsync` decode binary-content triggers (e.g. OneDrive `OnNewFileV2`), whose body is a base64 string. The `Stream` overloads read the caller-owned stream without closing it and enforce a generous, overridable body-size limit (`DefaultMaxBodySizeBytes`, 100 MB); `TryReadBinaryContent` returns `false` (rather than throwing) on malformed JSON. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))
- **Microsoft Dataverse client (`commondataservice`)** — generated typed `CommondataserviceClient` exposing the legacy Common Data Model (CDM) REST surface, which is the surface reachable through the Connector Namespace runtime. Covers environment/table discovery (`GetDataSetsAsync`, `GetTablesAsync`, `GetTableAsync`), row operations (`GetItemsAsync`, `GetItemAsync`, `PostItemAsync`, `PatchItemAsync`, `DeleteItemAsync`), attachments (`CreateAttachmentAsync`, `GetItemAttachmentsAsync`, `GetAttachmentContentAsync`, `DeleteAttachmentAsync`), relationships (`AssociateRecordsPatchItemAsync`, `DisassociateRecordsPostItemAsync`, `DisassociateSingleValueRecordDeleteItemAsync`, `GetCollectionRelationshipsAsync`), choice/metadata discovery, and pagination (`GetNextPageAsync`). Dataset values are full environment URLs (e.g., `https://contoso.crm.dynamics.com`) and are double URL-encoded per the connector's `x-ms-url-encoding: "double"` contract. The modern Dataverse Web API routes (`/api/data`, `/getorgs`, ...) return HTTP 404 through Connector Namespace and are excluded by the generator's deterministic per-connector route-selection policy, along with the redundant OData-style key-syntax routes.

#### Changed

- **`TriggerCallbackBodyConverter<T>` now throws an actionable error for binary-content bodies** — when a binary-content trigger callback (a JSON string body, e.g. OneDrive `OnNewFileV2`) is deserialized into a metadata payload type, the error now explains the cause and points to `ConnectorTriggerPayload.TryReadBinaryContent` / `ReadBinaryContentAsync` instead of failing with a generic token-mismatch message. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))

### 0.12.0-preview.1 (2026-06-02)

- Breaking: All 1,460 `ConnectorNames.*` constants renamed to PascalCase derived from ARM display names (e.g. `Googledrive` → `GoogleDrive`, `Microsoftteams` → `MicrosoftTeams`, `Office365` → `Office365Outlook`). Update any references by name. (#170)
- Breaking: Optional value-type parameters across all generated clients are now nullable (`int?`, `bool?`, `double?`) so `null` means "unspecified"; overriding subclasses must update signatures. (#180)
- Breaking: Dynamic model properties changed from `object` to `JsonElement?`; pre-serialize arbitrary values to `JsonElement`. (#157)
- Breaking: Output-only model properties changed to `{ get; init; }`; use object initializers or the generated `*ModelFactory` classes. (#161)
- Breaking: `IConnectorClient` marker interface removed — `ConnectorClientBase` now implements `IDisposable` directly. (#183)
- Breaking: `Teams.OnGroupMemberChangeResponseItem` removed; membership trigger payloads are now `TriggerCallbackPayload<object>`. (#170)
- Added Teams trigger payload types — `TeamsOnNewChannelMessageTriggerPayload`, `TeamsOnNewChannelMessageMentioningMeTriggerPayload`, `TeamsOnTeamMemberRemovedTriggerPayload`, `TeamsOnTeamMemberAddedTriggerPayload`, plus the `TeamsTriggers.Operations` registry. (#170)
- Added OpenTelemetry distributed tracing — each generated client has a per-connector `ConnectorActivitySource` (e.g., `Azure.Connectors.Sdk.teams`); subscribe to `Azure.Connectors.Sdk.*` to capture all connector operations. (#183)
- Added `ConnectorException` error-code parsing to populate `RequestFailedException.ErrorCode`. (#180)
- Added `[EditorBrowsable(EditorBrowsableState.Never)]` on inherited `Object` methods. (#160)
- Regenerated all 96 connector clients with copyright headers (#158), base-chained mock constructors (#159), and null-guard hardening (#175).

### 0.11.0-preview.1 (2026-05-15)

- Fixed: `TriggerCallbackBody<T>` now handles both batch and single-item callback shapes, preventing silent zero-item processing when splitOn is enabled. (#149)
- Breaking: `TriggerCallbackPayload<T>.Body` is now init-only and `TriggerCallbackBody<T>.Value` is `IReadOnlyList<T>?` with an internal setter; use `ConnectorModelFactory` to construct in tests.
- Breaking: Removed CamelCase JSON naming policy; properties without `[JsonPropertyName]` now serialize as PascalCase. (#84, #85)
- Breaking: Renamed `AzuremonitorlogsClient` → `AzureMonitorLogsClient` and `Office365usersClient` → `Office365UsersClient` (namespaces, DI methods, model factories, and `ConnectorNames` updated). (#126)
- Breaking: `IPageable<T>` is now internal; `ConnectorClientBase.CreatePageable` is private protected; JSON converter types are internal. (#124, #127)
- Added constructor overload `(Uri, TokenCredential)` without `ClientOptions`, and `ConnectorHttpClient` mocking support. (#123, #125)
- 36 new connector clients across batches 5–6 (Azure AD, Azure IoT Central, Outlook, Service Bus, Box, DocuSign, GitHub, Google Drive, Jira, Salesforce, SQL, Trello, and more).

### 0.10.0-preview.1 (2026-05-11)

- Breaking: Removed CamelCase JSON naming policy from ConnectorClientBase.JsonOptions; properties without `[JsonPropertyName]` now serialize as PascalCase.
- Breaking: Renamed AzuremonitorlogsClient to AzureMonitorLogsClient; Office365usersClient to Office365UsersClient (namespaces, DI methods, and model factories updated accordingly).
- Breaking: `IPageable<T>` is now internal; `ConnectorClientBase.CreatePageable` is private protected; JSON converter types are internal.
- Added constructor overload (Uri, TokenCredential) without ClientOptions on ConnectorClientBase and all generated clients.
- ConnectorHttpClient now supports mocking (protected parameterless constructor, virtual SendAsync).
- 48 new connector clients across 4 batches, including ExcelOnline, AzureEventGrid, Yammer, WdatpClient, AzureAutomation, AzureDataFactory, KeyVault, PowerBI, and many more.
- Regenerated all 12 previously shipped connector clients with PascalCase name overrides.

### 0.9.0-preview.1 (2026-05-08)

- Breaking: Constructor overhaul — Uri is now the primary parameter type; default credential changed from DefaultAzureCredential to ManagedIdentityCredential(SystemAssigned); credential parameter is no longer optional.
- Breaking: Output-only model properties now have internal set; use per-connector model factory classes for testing.
- Breaking: `ExceptionExtensions`, `HttpExtensions`, `RetryPolicy`, `ConnectorResponse<T>` removed as public API.
- Breaking: All namespaces renamed from `Microsoft.Azure.Connectors.*` to `Azure.Connectors.Sdk.*`.
- Breaking: ConnectorClientOptions now inherits from Azure.Core.ClientOptions; Polly dependency removed; HttpClient parameter removed from constructors.
- Added extensible enum types for Swagger enum properties, DI integration extension methods, per-connector model factory classes.
- Added Azure Monitor Logs typed client; removed deprecated Azure Log Analytics connector.

### 0.8.0-preview.1 (2026-04-30)

- Added Office 365 Users, Azure Log Analytics, SMTP, Azure Blob Storage, and IBM MQ typed clients.
- Added OpenTelemetry ActivitySource instrumentation for distributed tracing.

### 0.7.0-preview.1 (2026-04-30)

- Added `IAsyncEnumerable<T>` auto-pagination support for paginated connector operations.
- Paginated methods now return `ConnectorPageable<TPage, TItem>` instead of `Task<TPage>` (breaking).
- ManagedIdentityCredential updated to ManagedIdentityId API.

### 0.6.0-preview.1

- Initial Preview NuGet.org release of the Azure Connectors .NET SDK.

### 0.5.0-preview.1 (2026-04-15)

- Added MS Graph Groups and Users typed client with 7 action operations.
- Added Teams unit tests (constructor, dispose, mocked API, error handling, serialization).

### 0.4.0-preview.1 (2026-04-09)

- Added OneDrive for Business typed client with 22 action and 4 trigger operations.

### 0.3.0-preview.1 (2026-04-09)

- Breaking: Simplified all generated operation names by stripping version suffixes (V2/V3/V4).
- Breaking: Simplified trigger names to use On prefix with natural English.
- Breaking: Simplified type names with per-connector aliases.
- Added trigger operation constants and definition type pruning in the generator.

### 0.2.0-preview.1 (2026-04-07)

- Added Azure Data Explorer (Kusto) typed client.
- Added PR template, governance doc, CI code coverage, standard Microsoft OSS community files.
- Dependency bumps for Microsoft.Extensions.Http, test SDK, coverlet, and GitHub Actions.

### 0.1.0-preview.1 (2025-12-19)

- Initial SDK release with core abstractions (ConnectorClientBase, IConnectorClient, ConnectorClientOptions).
- Token providers: ManagedIdentityTokenProvider, ConnectionStringTokenProvider.
- HTTP pipeline with configurable retry policies.
- Office 365 connector client (generated).
