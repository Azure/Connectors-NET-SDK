## What's Changed

### 0.13.0-preview.1 (2026-07-09)

#### Added

- **`ConnectorTriggerPayload` helper to read trigger callbacks** — turns a raw Connector Namespace trigger callback (`string` or `Stream`) into a typed payload or decoded file bytes without per-function boilerplate. `Read<TPayload>` / `ReadAsync<TPayload>` deserialize metadata triggers (e.g. OneDrive `OnNewFilesV2`) with case-insensitive property matching, so camelCase wire fields bind correctly instead of silently yielding all-`null` items. `TryReadBinaryContent` / `ReadBinaryContentAsync` decode binary-content triggers (e.g. OneDrive `OnNewFileV2`), whose body is a base64 string. The `Stream` overloads read the caller-owned stream without closing it and enforce a generous, overridable body-size limit (`DefaultMaxBodySizeBytes`, 100 MB); `TryReadBinaryContent` returns `false` (rather than throwing) on malformed JSON. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))

#### Changed

- **`TriggerCallbackBodyConverter<T>` now throws an actionable error for binary-content bodies** — when a binary-content trigger callback (a JSON string body, e.g. OneDrive `OnNewFileV2`) is deserialized into a metadata payload type, the error now explains the cause and points to `ConnectorTriggerPayload.TryReadBinaryContent` / `ReadBinaryContentAsync` instead of failing with a generic token-mismatch message. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))
- **Microsoft Dataverse client (`commondataservice`)** — generated typed `CommondataserviceClient` exposing the legacy Common Data Model (CDM) REST surface, which is the surface reachable through the Connector Namespace runtime. Covers environment/table discovery (`GetDataSetsAsync`, `GetTablesAsync`, `GetTableAsync`), row operations (`GetItemsAsync`, `GetItemAsync`, `PostItemAsync`, `PatchItemAsync`, `DeleteItemAsync`), attachments (`CreateAttachmentAsync`, `GetItemAttachmentsAsync`, `GetAttachmentContentAsync`, `DeleteAttachmentAsync`), relationships (`AssociateRecordsPatchItemAsync`, `DisassociateRecordsPostItemAsync`, `DisassociateSingleValueRecordDeleteItemAsync`, `GetCollectionRelationshipsAsync`), choice/metadata discovery, and pagination (`GetNextPageAsync`). Dataset values are full environment URLs (e.g., `https://contoso.crm.dynamics.com`) and are double URL-encoded per the connector's `x-ms-url-encoding: "double"` contract. The modern Dataverse Web API routes (`/api/data`, `/getorgs`, ...) return HTTP 404 through Connector Namespace and are excluded by the generator's deterministic per-connector route-selection policy, along with the redundant OData-style key-syntax routes.

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
