## What's Changed

### 0.15.0-preview.1 (2026-10-29)

#### Added

- **Zoho ZeptoMail generated client** — adds five public mail-agent, send-mail, template-mail, and analytics actions. `SendTemplateMailInput.MergeInfo` remains `List<JsonElement?>` so fixed string fields and arbitrary non-string merge values stay representable. The malformed Swagger type `ReplyToAddresss` is exposed as `ReplyToAddress` without changing the `reply_to` wire property.

- **DataFlows SMS and ServiceNow connector clients** - added `DataflowsSmsClient` (`dataflowssms`) for SMS messaging and account actions, and `ServiceNowClient` (`service-now`) for records, catalog items, knowledge articles, and binary attachment uploads. DataFlows SMS is distinct from Power Query Dataflows. Both clients include mocked request and serialization tests.

- **Azure DevOps (VSTS) and Power Query Dataflows connector clients** - added `VisualStudioTeamServicesClient` (`visualstudioteamservices`) and `DataflowsClient` (`dataflows`), with models, model factories, discovery operations, trigger registration metadata, and mocked HTTP and serialization tests. Azure DevOps also includes typed trigger callbacks. See [connector usage](README.md#azure-devops-and-dataflows).

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

- **Automatic retries now default to methods that are safe to replay** — `GET`, `HEAD`, `OPTIONS`, and `TRACE` retain the configured Azure.Core retry behavior. `POST`, `PUT`, `PATCH`, `DELETE`, and all other methods make one attempt by default for both transient responses and ambiguous transport failures. Set `ConnectorClientOptions.RetryUnsafeHttpMethods` to `true` only when replaying side effects is acceptable. A caller-supplied `RetryPolicy` remains authoritative, and callers that construct a standalone `ConnectorHttpClient` pipeline own its retry semantics. See [Retry safety](docs/retry-safety.md). ([#269](https://github.com/Azure/Connectors-NET-SDK/issues/269))

- **Microsoft Dataverse `GetItemsAsync` now returns `AsyncPageable<Item>`** — callers that previously awaited one `ItemsList` page must enumerate the pageable with `await foreach`, or call `AsPages()` when page boundaries and continuation tokens are required. This prevents the connector's `@odata.nextLink` value from being discarded after the first page. ([#208](https://github.com/Azure/Connectors-NET-SDK/issues/208))

- **Google Drive `CreateFileAsync`: parameter renamed, route updated to v2, query key changed** — the `folderPath` parameter is now named `folder`, and the method calls `/datasets/default/v2/files` with the query key `folderId` instead of the previous `/datasets/default/files` with `folderPath`. Callers using the named argument `folderPath:` must rename it to `folder:`. Callers using positional arguments compile cleanly but will route to the v2 endpoint automatically.

  Migration: rename `folderPath:` to `folder:` — the underlying wire semantics changed (folder path → folder ID), so pass the folder's unique identifier rather than its path string.

- **Jira `ListIssuesResponse` pagination changed** — `MaximumNumberOfItems` is removed. Use `NextPageToken` and `IsLastPage` to continue paging. `ListIssuesAsync` callers can now pass `jQLQuery` and `nextPageToken`; the connector no longer injects the previous fixed ten-year JQL filter.

- **Plumsail Timezone enum: signed Etc/GMT members now have semantic Plus/Minus names** — existing members are renamed according to the wire value they already represented: for example, `EtcGMT4` (`Etc/GMT-4`) becomes `EtcGmtMinus4`, while `EtcGMT1` (`Etc/GMT+1`) becomes `EtcGmtPlus1`. Previously dropped counterparts are added under the opposite semantic name, including `EtcGmtPlus4` for `Etc/GMT+4` and `EtcGmtMinus1` for `Etc/GMT-1`. For the `Etc/GMT` three-way zero group, existing `EtcGMT0` (`Etc/GMT+0`) becomes `EtcGmtPlus0`, with new `EtcGmtMinus0` and `EtcGmt0` members. The plain `GMT` group changes similarly: existing `Gmt0` (`GMT+0`) becomes `GmtPlus0`, while new `GmtMinus0` and `Gmt0` members represent `GMT-0` and `GMT0`. Singleton entries without a colliding counterpart, such as `EtcGMT13` and `EtcGMT14`, are unchanged. All underlying wire strings are preserved. ([#181](https://github.com/Azure/Connectors-NET-SDK/issues/181))

- **Google Tasks and PDF.co operation name typos corrected** — `CraeteTaskAsync` is now `CreateTaskAsync`, and `PDFSerarchTextAsync` is now `PDFSearchTextAsync`. PDF.co callers must also rename `PDFSerarchTextInput` and the corresponding model-factory method to `PDFSearchTextInput`. Connector routes and wire payload names are unchanged.

- **SigningHub: over 60 properties changed from `JsonElement?` to typed model classes** — properties including `CertifyPolicyResponse.Certify` (→ `CertifyPermissionResponse`), `GetDocumentDetailsResponse.Certify` (→ `DocumentCertifyResponse`), `GetDocumentDetailsResponse.Template` (→ `DocumentTemplateResponse`), and many authentication, permission, access-duration, and signature-field properties are now strongly typed. Source code that called `.GetProperty(...)` on these `JsonElement?` properties no longer compiles; switch to the typed model accessors.

- **Uppercase extensible-enum values now retain PascalCase word boundaries** — Key Vault members `Rsaoaep` and `Rsaoaep256` become `RsaOaep` and `RsaOaep256`. Plumsail members `Wsu` and `Nzchat` become `WSu` and `NzChat`. SigningHub corrects 20 members, including `Authenticationpassword` → `AuthenticationPassword`, `AUTHENTICATIONOIdC` → `AuthenticationOidc`, `Companylogo` → `CompanyLogo`, and `VALId` → `Valid`. Exact wire values are unchanged.
