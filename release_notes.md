## What's Changed

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
