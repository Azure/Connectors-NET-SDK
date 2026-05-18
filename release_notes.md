## What's Changed

### 0.10.0-preview.1 (2026-05-11)

- Breaking: Removed CamelCase JSON naming policy from ConnectorClientBase.JsonOptions; properties without [JsonPropertyName] now serialize as PascalCase.
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
- Breaking: All namespaces renamed from Microsoft.Azure.Connectors.* to Azure.Connectors.Sdk.*.
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
