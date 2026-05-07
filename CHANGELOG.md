# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Breaking Changes

- **Constructor overhaul: `Uri` primary + `string` convenience + `ManagedIdentityCredential` default** (#111)
  - `Uri` is now the primary constructor parameter type for all generated clients and `ConnectorClientBase`
  - `string` convenience overload delegates to `Uri` constructor with `Uri.TryCreate` validation (throws `ArgumentException` for invalid/relative URLs instead of `UriFormatException`)
  - Default credential changed from `DefaultAzureCredential` to `ManagedIdentityCredential(SystemAssigned)` — deterministic, fails fast on dev machines (CodeQL SM05137)
  - `credential` parameter is no longer optional — pass an explicit `TokenCredential` or omit for `ManagedIdentityCredential` default
  - Removed `managedIdentityClientId` constructor overload — construct `ManagedIdentityCredential` directly and pass it as the `credential` parameter
- **Output-only model properties now have `internal set`** — service-generated properties (ETag, LastModified, *DateTime timestamps) are no longer publicly settable. Use the new per-connector model factory classes to construct instances with these properties in tests (#106)
- **Made `ExceptionExtensions` internal** — `IsFatal()` is only used internally in `ConnectorClientBase` and was never intended as a public API (#108)
- **Made `HttpExtensions` internal** — `ToJsonContent`, `ReadAsAsync`, `AddCorrelationId`, `AddClientRequestId` are internal HTTP utilities, not consumer-facing (#108)
- **Removed `RetryPolicy` class** — dead code; retry configuration moved to `ClientOptions.Retry` in PR #94 (#108)
- **Removed `ConnectorResponse<T>` class** and `ConnectorHttpClient.GetAsync<T>`, `PostAsync<TRequest, TResponse>`, `ParseResponseAsync<T>` methods — all generated clients use `ConnectorClientBase.CallConnectorAsync<T>` which returns `Task<T>` directly; no callers referenced these APIs (#99)
- Renamed all namespaces from `Microsoft.Azure.Connectors.*` to `Azure.Connectors.Sdk.*`, dropping the `Microsoft.` prefix for consistency with modern Azure SDK conventions and cross-language SDKs (#87)
  - e.g., `using Microsoft.Azure.Connectors.Sdk.Office365;` → `using Azure.Connectors.Sdk.Office365;`
  - NuGet package renamed from `Microsoft.Azure.Connectors.Sdk` to `Azure.Connectors.Sdk`
  - Project/assembly renamed from `Microsoft.Azure.Connectors.Sdk` to `Azure.Connectors.Sdk`
- **`ConnectorClientOptions` now inherits from `Azure.Core.ClientOptions`** — retry, transport, and diagnostics are configured via the inherited `Retry`, `Transport`, and `Diagnostics` properties instead of custom properties (#88)
  - Removed `MaxRetryAttempts`, `Timeout`, `UseExponentialBackoff`, `InitialRetryDelay` — use `options.Retry.MaxRetries`, `options.Retry.NetworkTimeout`, `options.Retry.Mode`, `options.Retry.Delay` instead
  - Added `ServiceVersion` enum for API versioning
- **Removed `ITokenProvider` interface and all implementations** — `Azure.Core.TokenCredential` is now the only authentication path. Use `DefaultAzureCredential`, `ManagedIdentityCredential`, or any other `TokenCredential` subclass directly (#95)
  - Removed `ILogger` parameter and `Logger` property from `ConnectorClientBase` — logging and diagnostics are now handled by the Azure.Core `HttpPipeline` (configure via `ConnectorClientOptions.Diagnostics`; subscribe via `AzureEventSourceListener`) (#95)
  - Removed `Microsoft.Extensions.Logging.Abstractions` package dependency (#95)
- **Removed `HttpClient` parameter from all generated client constructors** — inject custom HTTP transport via `options.Transport = new HttpClientTransport(httpClient)` instead (#88)
- **Replaced Polly retry with Azure.Core `HttpPipeline`** — retry, authentication, and diagnostics now use the standard Azure SDK pipeline (#88)
- **Removed `Polly` and `Microsoft.Extensions.Http` package dependencies** (#88)

### Changed

- **Breaking:** Generated connector clients now inherit from `ConnectorClientBase` instead of implementing `IDisposable` directly (#88)
- **Breaking:** Per-connector exception types (e.g., `Office365ConnectorException`, `TeamsConnectorException`) replaced with unified `ConnectorException` base type with `ConnectorName`, `Operation`, `StatusCode`, and `ResponseBody` properties (#88)
- **Breaking:** Generated client constructors accept a new optional `ConnectorClientOptions` parameter for configuring retry policy, timeout, and exponential backoff — the `HttpClient` parameter moved from position 3 to position 4 (#88)
- Generated clients now use SDK infrastructure (`ConnectorHttpClient`) for authentication, retry with exponential backoff, OpenTelemetry instrumentation, and SSRF-protected URL resolution (#88)

### Added

- **Per-connector model factory classes** (`Office365ModelFactory`, `TeamsModelFactory`, etc.) — static factory methods for constructing model instances with output-only properties, following the [Azure SDK mocking guidelines](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-mocking-factory) (#106)
- Azure Monitor Logs (`azuremonitorlogs`) generated typed client for querying Log Analytics workspaces and Application Insights — includes QueryData, QueryDataV2, VisualizeQuery, VisualizeQueryV2 operations with dynamic schema support for query results
- `ConnectorException` — unified exception type for all connector API failures (#88)
- `ConnectorClientBase` now provides `CallConnectorAsync`, `ResolveUrl`, shared JSON options, and convenience constructors accepting `connectionRuntimeUrl` + `TokenCredential` (#88)

### Removed

- **`ITokenProvider`** interface — replaced by `Azure.Core.TokenCredential` (#95)
- **`ConnectionStringTokenProvider`** — no longer needed; was unused outside docs (#95)
- **`ManagedIdentityTokenProvider`** — use `ManagedIdentityCredential` from `Azure.Identity` directly (#95)
- **`TokenCredentialTokenProvider`** adapter — no longer needed without `ITokenProvider` (#95)
- **`TokenProviderCredential`** adapter — no longer needed without `ITokenProvider` (#95)
- **`ConnectorClientBase(ITokenProvider, ...)` constructor** — use `ConnectorClientBase(string connectionRuntimeUrl, TokenCredential?, ...)` instead (#95)
- **`ConnectorHttpClient(ITokenProvider, ...)` constructor** — use the `HttpPipeline`-based constructor instead (#95)
- Azure Log Analytics (`azureloganalytics`) connector removed — the connector and all its user-facing operations are deprecated by Microsoft (see [connector docs](https://learn.microsoft.com/en-us/connectors/azureloganalytics/)). Microsoft recommends the [Azure Monitor Logs](https://learn.microsoft.com/en-us/connectors/azuremonitorlogs/) connector as a replacement.

## [0.8.0-preview.1] - 2026-04-30

### Added

- Office 365 Users (`office365users`) generated typed client for user profile lookups, manager/reports chain, user search, and trending documents (#75)
- Azure Log Analytics (`azureloganalytics`) generated typed client for workspace discovery and query schema operations (#74) *(removed in next release — connector deprecated by Microsoft)*
- SMTP (`smtp`) generated typed client for sending email via SMTP connectors (#76)
- Azure Blob Storage (`azureblob`) generated typed client with file and container operations (#80)
- IBM MQ (`mq`) generated typed client for messaging queue operations (#81)
- OpenTelemetry `ActivitySource` instrumentation in `ConnectorHttpClient` for distributed tracing of connector API calls (#73)

## [0.7.0-preview.1] - 2026-04-30

### Added

- `IAsyncEnumerable<T>` auto-pagination support for paginated connector operations (#58)
- `IPageable<T>` interface for page types with `Value` + `NextLink` properties
- `ConnectorPageable<TPage, TItem>` implementing `IAsyncEnumerable<TItem>` with automatic NextLink following and `AsPages()` for page-level access
- Paginated methods: `OnedriveforbusinessClient.ListFolderAsync`, `TeamsClient.GetMessagesFromChannelAsync`, `TeamsClient.GetMessagesFromChatAsync`

### Changed

- Paginated methods now return `ConnectorPageable<TPage, TItem>` instead of `Task<TPage>` (breaking change)
- `CallConnectorAsync` supports absolute NextLink URLs via `ResolveUrl` with SSRF protection (scheme + host + port validation)
- `ManagedIdentityCredential` updated from deprecated constructor to `ManagedIdentityId` API
- SDK `using` directive conditionally emitted only when needed by generated code

## [0.6.0-preview.1] - YYYY-MM-DD

### Added

- Initial NuGet.org release of the Azure Connectors .NET SDK

## [0.5.0-preview.1] - 2026-04-15

### Added

- MS Graph Groups & Users (`msgraphgroupsanduser`) generated typed client with 7 action operations: ListUsers, ListGroupsByDisplayNameSearch, ListSubscribedSkus, ListDirectGroupMembers, GetMemberLicenseDetails, GetGroupProperties, GetMemberGroups
- Teams unit tests (constructor, dispose, mocked API call, error handling, serialization round-trips)

## [0.4.0-preview.1] - 2026-04-09

### Added

- OneDrive for Business generated typed client with 22 action operations and 4 trigger operations (#39)
- OneDrive file operations: get/update/delete metadata, get/create file content, copy, move, convert, extract archive (#39)
- OneDrive sharing: create share links by file ID or path (#39)
- OneDrive folder operations: list root folder, list files in folder, find files by search (#39)
- OneDrive trigger payloads and operation constants for file created/modified events (#39)

## [0.3.0-preview.1] - 2026-04-09

### Breaking Changes

- Simplified all generated operation names by stripping version suffixes (V2, V3, V4) — e.g., `SendEmailV2Async` → `SendEmailAsync` (#44)
- Simplified trigger names to start with `On` prefix and use natural English — e.g., `CalendarGetOnUpdatedItemsV3` → `OnCalendarUpdatedItems` (#44)
- Simplified type names with per-connector aliases — e.g., `ClientSendHtmlMessage` → `SendEmailInput` (#44)
- Dropped `OnFlaggedEmailV3` trigger (superseded by V4, identical parameters) (#44)
- Pruned unreferenced swagger definition types from generated output (#44)
- Removed `samples/SampleConnectorUsage/` project (use [Connectors-NET-Samples](https://github.com/Azure/Connectors-NET-Samples) instead) (#44)

### Added

- Trigger operation constants for all triggers, including those without response types (e.g., `OnWebhookMessageReactionTrigger`) (#44)
- Definition type pruning: generator now only emits types transitively reachable from operations (#44)

### Changed

- Wire values (operationId strings, JSON property names) remain unchanged — only the C# API surface is simplified (#44)
- README Quick Start and validated-connectors table updated for new names (#44)
- Documentation link updated to point to Connectors-NET-Samples repo (#44)

## [0.2.0-preview.1] - 2026-04-07

### Added

- Azure Data Explorer (Kusto) generated typed client (#37)
- PR template, governance doc, and CI code coverage (#36)
- Standard Microsoft OSS community files (#27)
- Dependabot version updates for NuGet and GitHub Actions (#26)
- Release instructions in README and copilot-instructions (#25)

### Changed

- Bump Microsoft.Extensions.Http and Microsoft.Extensions.Logging.Abstractions (#33)
- Bump Microsoft.NET.Test.Sdk from 17.14.1 to 18.3.0 (#35)
- Bump coverlet.collector from 6.0.4 to 8.0.1 (#32)
- Bump NuGet minor/patch dependencies (#31)
- Bump GitHub Actions: checkout v6.0.2, setup-dotnet v5.2.0, upload-artifact v7.0.0 (#28, #29, #30)
- Update cross-references to public Connectors-NET-Samples and LSP repos (#40)

## [0.1.0-preview.1] - 2025-12-19

### Added

- Initial SDK release with core abstractions (`ConnectorClientBase`, `IConnectorClient`, `ConnectorClientOptions`)
- Token providers: `ManagedIdentityTokenProvider`, `ConnectionStringTokenProvider`
- HTTP pipeline with configurable retry policies
- Office 365 connector client (generated)
- SharePoint connector client (generated)
- Teams connector client (generated)

[Unreleased]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.8.0-preview.1...HEAD
[0.8.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.7.0-preview.1...v0.8.0-preview.1
[0.7.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.6.0-preview.1...v0.7.0-preview.1
[0.6.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.5.0-preview.1...v0.6.0-preview.1
[0.5.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.4.0-preview.1...v0.5.0-preview.1
[0.4.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.3.0-preview.1...v0.4.0-preview.1
[0.3.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.2.0-preview.1...v0.3.0-preview.1
[0.2.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.1.0-preview.1...v0.2.0-preview.1
[0.1.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/releases/tag/v0.1.0-preview.1
