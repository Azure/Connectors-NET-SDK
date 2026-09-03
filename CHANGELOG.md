# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- **Distinct versioned route families are no longer collapsed onto one operation** — regenerated the three issue-pinned clients from AzureUX-BPM PR [17032938](https://msazure.visualstudio.com/One/_git/AzureUX-BPM/pullrequest/17032938). A version suffix is now only a naming hint: operations collapse when their HTTP route, visibility, trigger status, parameters, request schema, and response schemas are equivalent. Distinct contracts remain callable under deterministic names. Office 365 Groups adds `ListOwnedGroupsV0Async` for `/v1.0/me/ownedObjects` while preserving the existing V2 `memberOf` route; SQL adds `ExecuteProcedureV0Async` for the default dataset while `ExecuteProcedureAsync` remains server/database scoped; and Azure IoT Central adds `DeviceTemplatesListV0Async` for the dynamically reachable preview route while `DeviceTemplatesListAsync` remains on V1. A same-base catalog audit compared 1,517 clients and identified 41 additional connector candidates for fresh-snapshot follow-up; they are not included here because this PR does not replace newer checked-in output with historical cache data. The audit introduced no new dangling-discovery or route-based type-collapse findings and reproduced byte-identical output from the frozen cache. ([#265](https://github.com/Azure/Connectors-NET-SDK/issues/265))

- **Extensible enum member collisions now preserve every wire value** — distinct values that normalize to one C# identifier are allocated stable unique names instead of dropping later claimants. Sign-and-number collisions use readable names, such as `Etc/GMT+4` → `EtcGmtPlus4` and `Etc/GMT-4` → `EtcGmtMinus4`; ordinary delimiter hyphens still use the general numeric fallback. Values normalized to mandatory struct members (`Equals`, `GetHashCode`, `ToString`) use an available `Value`-suffixed name. The frozen-cache audit tested 100 checked-in SDK names: 97 connectors generated, 96 were unchanged, and Plumsail was the only generated client affected. `MsGraphGroupsAndUsers` was unavailable from the current ARM catalog, while `ConnectorNames` and `ManagedConnectors` are infrastructure files rather than connector inputs. ([#181](https://github.com/Azure/Connectors-NET-SDK/issues/181), AzureUX-BPM PR 16971205)

  Plumsail exercises the general normalization-collision path, not the reserved-member path. Reserved-member handling is retained as a synthetic generator regression for the historical failure reported in #181; no active reserved-member collision was found in the 97 connectors generated from the frozen snapshot. Of the eleven connector names in the issue, only Etsy remains in the current catalog, and its current Swagger has no such collision.

- **All 98 connector clients re-synchronized with live Swagger** — regenerated every shipped client from the ARM catalog captured 2026-08-31. DocuSign and Jira are intentionally excluded from unfiltered generation by the generator skip list, but explicit filtered generation succeeded from their current ARM exports. Seven clients have content changes: Azure Monitor Logs, Google Drive, Jira, Key Vault, Plumsail, SigningHub, and Teams. The other 91 clients are byte-identical to their current versions. Validation found no dangling discovery references or type-name collapses. (AzureUX-BPM PRs 16971205 and 16998561; live cache SHA-256 `894FB4C0977736CDF2A3D1103894C75470BF1ADBB2B2E44D742F20AACAFCED25`)

### Changed

- **Azure Monitor Logs: new `PartialQueryError` model exposes partial-query error details** — `Table.Error` and `VisualizeResults.Error` now carry a typed `PartialQueryError` object with a `code` property (wire name `error`), available when the service returns partial results. Previously these properties were absent from the model.

- **Teams: new operations and models from Swagger expansion** — `ArchiveChannelAsync` archives a Teams channel and accepts an `ArchiveChannelInput`; `GetSubscriptionScopeSchemaAsync` returns the subscription-scope schema used by dynamic discovery. New response model `AsyncOperationResponse` carries an extensible `Status` enum for async operation tracking. New trigger-schema models `DynamicTranscriptTriggerRequest` and `DynamicRecordingTriggerRequest` support transcript and recording trigger subscription registration.

- **Google Drive: `BlobMetadata` gains `FolderId` and `FolderPath` properties** — newly added properties (wire names `FolderId`, `FolderPath`) expose the parent folder's identifier and path on file and folder metadata responses.

- **Jira: issue search uses current JQL and token pagination contracts** — `ListIssuesAsync` now accepts optional `jQLQuery` and `nextPageToken` inputs. `ListIssuesResponse` replaces `MaximumNumberOfItems` with `NextPageToken` and `IsLastPage`, matching the current search response.

- **SigningHub: over 60 previously opaque `JsonElement?` properties now have typed models** — properties such as `CertifyPolicyResponse.Certify`, `GetDocumentDetailsResponse.Certify`, `GetDocumentDetailsResponse.Template`, and many authentication, permission, access-duration, and signature-field objects are now strongly typed with dedicated model classes. Consumer code that read these properties as raw `JsonElement` and called `.GetProperty()` must switch to the typed accessors; see the Breaking Changes section.

### Breaking Changes

- **Google Drive `CreateFileAsync`: parameter renamed, route updated to v2, query key changed** — the `folderPath` parameter is now named `folder`, and the method calls `/datasets/default/v2/files` with the query key `folderId` instead of the previous `/datasets/default/files` with `folderPath`. Callers using the named argument `folderPath:` must rename it to `folder:`. Callers using positional arguments compile cleanly but will route to the v2 endpoint automatically.

  Migration: rename `folderPath:` to `folder:` — the underlying wire semantics changed (folder path → folder ID), so pass the folder's unique identifier rather than its path string.

- **Jira `ListIssuesResponse` pagination changed** — `MaximumNumberOfItems` is removed. Use `NextPageToken` and `IsLastPage` to continue paging. `ListIssuesAsync` callers can now pass `jQLQuery` and `nextPageToken`; the connector no longer injects the previous fixed ten-year JQL filter.

- **Plumsail Timezone enum: signed Etc/GMT members now have semantic Plus/Minus names** — existing members are renamed according to the wire value they already represented: for example, `EtcGMT4` (`Etc/GMT-4`) becomes `EtcGmtMinus4`, while `EtcGMT1` (`Etc/GMT+1`) becomes `EtcGmtPlus1`. Previously dropped counterparts are added under the opposite semantic name, including `EtcGmtPlus4` for `Etc/GMT+4` and `EtcGmtMinus1` for `Etc/GMT-1`. For the `Etc/GMT` three-way zero group, existing `EtcGMT0` (`Etc/GMT+0`) becomes `EtcGmtPlus0`, with new `EtcGmtMinus0` and `EtcGmt0` members. The plain `GMT` group changes similarly: existing `Gmt0` (`GMT+0`) becomes `GmtPlus0`, while new `GmtMinus0` and `Gmt0` members represent `GMT-0` and `GMT0`. Singleton entries without a colliding counterpart, such as `EtcGMT13` and `EtcGMT14`, are unchanged. All underlying wire strings are preserved. ([#181](https://github.com/Azure/Connectors-NET-SDK/issues/181))

- **Google Tasks and PDF.co operation name typos corrected** — `CraeteTaskAsync` is now `CreateTaskAsync`, and `PDFSerarchTextAsync` is now `PDFSearchTextAsync`. PDF.co callers must also rename `PDFSerarchTextInput` and the corresponding model-factory method to `PDFSearchTextInput`. Connector routes and wire payload names are unchanged.

- **SigningHub: over 60 properties changed from `JsonElement?` to typed model classes** — properties including `CertifyPolicyResponse.Certify` (→ `CertifyPermissionResponse`), `GetDocumentDetailsResponse.Certify` (→ `DocumentCertifyResponse`), `GetDocumentDetailsResponse.Template` (→ `DocumentTemplateResponse`), and many authentication, permission, access-duration, and signature-field properties are now strongly typed. Source code that called `.GetProperty(...)` on these `JsonElement?` properties no longer compiles; switch to the typed model accessors.

- **Uppercase extensible-enum values now retain PascalCase word boundaries** — Key Vault members `Rsaoaep` and `Rsaoaep256` become `RsaOaep` and `RsaOaep256`. Plumsail members `Wsu` and `Nzchat` become `WSu` and `NzChat`. SigningHub corrects 20 members, including `Authenticationpassword` → `AuthenticationPassword`, `AUTHENTICATIONOIdC` → `AuthenticationOidc`, `Companylogo` → `CompanyLogo`, and `VALId` → `Valid`. Exact wire values are unchanged. (AzureUX-BPM PR 16998561)

<!-- MAINTAINER NOTE: release_notes.md continuously mirrors this [Unreleased]
  content for NuGet packaging. Before tagging a release, (1) cut this content
  into a new versioned section here (e.g. ## [X.Y.Z-preview.N] - YYYY-MM-DD),
  (2) add a reference link at the bottom, (3) update [Unreleased] compare base,
  and (4) rename the mirrored release_notes.md heading to the same version/date.
  Do NOT put HTML comments in release_notes.md — it is packed verbatim. -->

## [0.14.0-preview.1] - 2026-08-12

### Breaking Changes

- **Seven clients preserve wire properties that previously collided after C# name generation** — regenerated Etsy, GitHub, Office 365 Outlook, Plumsail Documents, SigningHub, Twitter, and WordPress from the merged collision resolver in AzureUX-BPM PR 16763267. GitHub `PullRequest.DiffUrl` now represents `diff_url`, while `PullRequest.PullRequestDiffUrl` represents the previously dropped `comments_url`. Office 365 `MailTipsClientReceive.ExternalMemberCount` now represents `externalMemberCount`, and `IsModerated` is corrected from `int?` to `bool?` for `isModerated`. Plumsail `AddPowerAutomateWebhookData.ProcessId` now represents `processId`, while `ProcessName` represents the previously dropped `hookUrl`. Etsy adds `BuyerTotalAdjustmentAmount`; SigningHub adds `DocumentId2` for `document_id`; Twitter adds `CreatedAtIso` to both tweet models; and WordPress adds `Id2` for `guid` to both post models. Callers using the three previously misbound GitHub, Office 365, or Plumsail properties must move values to the corrected properties.

- **DocuSign embedded-signing discovery now binds the current route to the unsuffixed method name** — `StaticResponseForEmbeddedSigningSchemaAsync` now requires `isThisAnPersonSigner` and calls `/embeddedSigning_schema_v2`. The older `/embeddedSigning_schema` operation is no longer retained, and callers of `StaticResponseForEmbeddedSigningSchemaV2Async` must use `StaticResponseForEmbeddedSigningSchemaAsync`. This follows the generator rule that the current retained revision owns the unsuffixed name. (AzureUX-BPM PR 16671915)

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

### Added

- **CloudConvert typed client** — adds the four public file-conversion operations and five internal discovery helpers referenced exclusively through `x-ms-dynamic-properties`. The client is generated from [AzureUX-BPM PR 16753182](https://msazure.visualstudio.com/One/_git/AzureUX-BPM/pullrequest/16753182).
- **MailChimp A/B split options preserve both wait fields** — `ABSplitOpts` now exposes distinct `WaitUnits` and `WaitTime` properties for the `wait_units` and `wait_time` wire fields instead of silently dropping one generated-name collision. ([#231](https://github.com/Azure/Connectors-NET-SDK/issues/231), AzureUX-BPM PR 16763267)
- **Discovery helpers retained from nested trigger and response schemas** — regenerated Azure Automation, Azure Monitor Logs, Elfsquad Data, Formstack Forms, Monday, Typeform, and Microsoft Defender ATP clients now include helpers pinned by nested trigger and response schemas in connector Swagger. Added `GetRunbookAsync`, `GetTimeRangeSelectionControlAsync`, `GetTriggerSchemaAsync`, `GetFormSchemaAsync`, three Monday column-schema helpers, Typeform's current `GetSchemaAsync`, and `AdvancedHuntingSchemaAsync`, plus five response/request models and four model-factory methods. Elfsquad Data's `GetTriggerSchemaAsync` and Formstack Forms' `GetFormSchemaAsync` return `Task` because owner Swagger declares no success response schema. Azure Automation's single-runbook route is likewise typed as `RunbookListResults` because that is the response definition declared by owner Swagger for both runbook routes. (AzureUX-BPM PR 16671915)

- **Zendesk regains four operations lost to internal versioned siblings** — `GetTablesAsync`, `GetItemsAsync`, `GetItemAsync` and the `GetTableAsync` discovery method are generated again, and the public `GetOnNewItems` trigger is registered alongside `GetOnUpdatedItemsV2` with its own typed callback payload. Version-collision resolution previously ran across the whole operation set, so each `x-ms-visibility: internal` `V2` sibling displaced its public counterpart and was then dropped as unreferenced. Surviving methods also carried a `[DynamicValues("GetTables")]` reference to an operation no longer present in the client. ([#220](https://github.com/Azure/Connectors-NET-SDK/issues/220), AzureUX-BPM PR 16576205)
- **SQL Server regains two default-dataset procedure discovery routes** — `GetProceduresAsync` and `GetProcedureAsync` now address `/datasets/default/...` and are joined by `GetProceduresV2Async` and `GetProcedureV2Async` for the server- and database-scoped routes. Each pair is a distinct reachable contract that previously collapsed onto the V2 route alone. `GetTableAsync`, `GetPassThroughNativeQueryMetadataAsync`, and `GetTablesAsync` deliberately continue to target versioned routes because their unversioned siblings are not reachable or are deprecated. ([#222](https://github.com/Azure/Connectors-NET-SDK/issues/222), AzureUX-BPM PRs 16576205 and 16671915)
- **New operations from current managed connector Swagger** — DocuSign adds `GetDocGenTemplateTabsAsync` and `GetOrganizationsAsync`; Teams adds `RemoveMemberFromChatAsync` and `UpdateChannelPropertiesAsync`; SharePoint adds `GetDayOfWeekOptionsAsync`. (#210)

### Changed

- **Discovery methods are now emitted after the public surface** — generated clients list customer-facing operations first, then the internal discovery methods retained for dynamic values and dynamic schema. Pipedrive and Plumsail change by this ordering alone; their operation and model sets are unchanged. (AzureUX-BPM PR 16576205)

- **Generated types are documented from their own definition instead of from whatever reached them** — a type's summary was derived from the property or operation that happened to reach it first, which produced meaningless text and made the documentation depend on declaration order. A type reached through a bare envelope property such as `value` was summarized as `Item in value`, naming the wrapper rather than the element, and raw Swagger keys such as `x-ms-capabilities` and `sortRestrictions` were emitted as public API documentation. Zendesk `Table` and `Item` now carry their own names, three Zendesk metadata types gain real descriptions, and seventeen SQL Server summaries improve, for example `Item in List of datasets` becomes `Represents a database.` and `Response for GetTablesForDeleteItem` becomes `Represents a list of tables.` Documentation only; no operation, model, or wire change. (AzureUX-BPM PR 16600581)

- **Regenerated all 97 existing connector clients** using AzureUX-BPM PR 16421737. Generation now uses Azure Identity instead of the legacy ARMClient executable, preserves valid ARM JSON before applying malformed-response fallbacks, filters operations marked `deprecated`, and correctly handles connectors that advertise both JSON and multipart content types. Office 365, Docuware, and SigningHub now regenerate successfully from current ARM exports. (#210)
- **Corrected generated Swagger documentation text** — fixed `seperated`/`Comma-seperated` and `lists’s` in generated XML documentation. (#210)
- **Connector-specific corrections now preserve owner contracts** — Azure Queues receives named types for its nested message response without flattening the wire shape. Current AAPT connector Swagger remains authoritative for Event Hubs required parameters, Microsoft Forms and Word Online Business internal discovery methods, and DocumentDB per-document dynamic fields. (#210)

### Fixed

- **Connector path and query values now use invariant-culture wire formatting** — regenerated all 98 shipped clients from AzureUX-BPM PR 16770557 so numeric and other formattable parameters no longer depend on `CurrentCulture` before URI escaping. For example, a `double` value of `3.14` now remains `3.14` under `de-DE` instead of becoming `3%2C14`. The hand-written ISO DateTime converter now parses and formats with `InvariantCulture`, and CA1305 is build-enforced to prevent regressions. ([#200](https://github.com/Azure/Connectors-NET-SDK/issues/200))

## [0.13.0-preview.1] - 2026-07-09

### Added

- **`ConnectorTriggerPayload` helper to read trigger callbacks** — turns a raw Connector Namespace trigger callback (`string` or `Stream`) into a typed payload or decoded file bytes without per-function boilerplate. `Read<TPayload>` / `ReadAsync<TPayload>` deserialize metadata triggers (e.g. OneDrive `OnNewFilesV2`) with case-insensitive property matching, so camelCase wire fields bind correctly instead of silently yielding all-`null` items. `TryReadBinaryContent` / `ReadBinaryContentAsync` decode binary-content triggers (e.g. OneDrive `OnNewFileV2`), whose body is a base64 string. The `Stream` overloads read the caller-owned stream without closing it and enforce a generous, overridable body-size limit (`DefaultMaxBodySizeBytes`, 100 MB); `TryReadBinaryContent` returns `false` (rather than throwing) on malformed JSON. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))
- **Microsoft Dataverse client (`commondataservice`)** — generated typed `CommondataserviceClient` exposing the legacy Common Data Model (CDM) REST surface, which is the surface reachable through the Connector Namespace runtime. Covers environment/table discovery (`GetDataSetsAsync`, `GetTablesAsync`, `GetTableAsync`), row operations (`GetItemsAsync`, `GetItemAsync`, `PostItemAsync`, `PatchItemAsync`, `DeleteItemAsync`), attachments (`CreateAttachmentAsync`, `GetItemAttachmentsAsync`, `GetAttachmentContentAsync`, `DeleteAttachmentAsync`), relationships (`AssociateRecordsPatchItemAsync`, `DisassociateRecordsPostItemAsync`, `DisassociateSingleValueRecordDeleteItemAsync`, `GetCollectionRelationshipsAsync`), choice/metadata discovery, and pagination (`GetNextPageAsync`). Dataset values are full environment URLs (e.g., `https://contoso.crm.dynamics.com`) and are double URL-encoded per the connector's `x-ms-url-encoding: "double"` contract. The modern Dataverse Web API routes (`/api/data`, `/getorgs`, ...) return HTTP 404 through Connector Namespace and are excluded by the generator's deterministic per-connector route-selection policy, along with the redundant OData-style key-syntax routes.

### Changed

- **`TriggerCallbackBodyConverter<T>` now throws an actionable error for binary-content bodies** — when a binary-content trigger callback (a JSON string body, e.g. OneDrive `OnNewFileV2`) is deserialized into a metadata payload type, the error now explains the cause and points to `ConnectorTriggerPayload.TryReadBinaryContent` / `ReadBinaryContentAsync` instead of failing with a generic token-mismatch message. ([#190](https://github.com/Azure/Connectors-NET-SDK/issues/190))

## [0.12.0-preview.1] - 2026-06-02

### Breaking Changes

- **All 1,460 `ConnectorNames.*` constants renamed to PascalCase** — constant names now derive from ARM connector display names via `IdentifierNormalizer.Normalize` instead of just capitalizing the first character of the raw connector ID. For example: `Googledrive` → `GoogleDrive`, `Microsoftteams` → `MicrosoftTeams`, `Sharepointonline` → `SharePoint`, `Office365` → `Office365Outlook`. Brand casing overrides applied where ARM display name differs from official brand: `DataBlend`, `VATCheckApi`, `MetaTask`. Consumers referencing any renamed constant by name must update their references. (#170)
- **Optional value-type parameters changed to nullable across all generated clients** — the generator was updated to use nullable types for optional value-type parameters so that `null` correctly represents "unspecified" while `0`/`false` are valid distinct values. Affects `int`, `bool`, and `double` optional parameters across many connectors including `TodoClient`, `TicketmasterClient`, `ShiftsClient`, `TeamsClient`, `SharePointOnlineClient`, `ServiceBusConnectorClient`, `WaywedoClient`, `UniversalPrintClient`, `TextRequestClient`, `RevaiClient`, `SigningHubClient`, and `SeismicPlannerClient`. Callers passing literals still compile (implicit conversion); subclasses that override these `virtual` methods with the old non-nullable signature must update the parameter type. (#180)
- **`Teams.OnGroupMemberChangeResponseItem` model class and its model factory method removed** — Teams connector swagger (v1.0.4) changed the membership trigger response from a named typed definition (`OnGroupMemberChangeResponseItem` with a `UserId` property) to an inline anonymous object array. The generator no longer produces a named class for inline schemas without a definition reference; membership trigger payloads are now `TriggerCallbackPayload<object>`. Consumers referencing `OnGroupMemberChangeResponseItem` directly must migrate; the `id` field is still present at runtime in the deserialized callback body. (#170)
- **Dynamic model properties changed from `object` to `JsonElement?`** — all generated model properties typed as `object` (for free-form JSON) are now `JsonElement?`. Consumers that assigned arbitrary .NET objects must now pre-serialize to `JsonElement`. Affects ~493 properties across Kusto, MsGraphGroupsAndUsers, Projectplace, and others. ([#157](https://github.com/Azure/Connectors-NET-SDK/issues/157))
- **Output-only model properties changed from `{ get; internal set; }` to `{ get; init; }`** — all generated output-only model properties now use `init` setters. Post-construction assignment (`model.Prop = x;`) no longer compiles; use object initializers or the generated `*ModelFactory` classes. ([#161](https://github.com/Azure/Connectors-NET-SDK/issues/161))
- **`IConnectorClient` marker interface removed** — `ConnectorClientBase` now implements `IDisposable` directly. Code referencing `IConnectorClient` must use `ConnectorClientBase` instead. (#183)

### Added

- **Teams trigger payload types** — `TeamsOnNewChannelMessageTriggerPayload`, `TeamsOnNewChannelMessageMentioningMeTriggerPayload`, `TeamsOnTeamMemberRemovedTriggerPayload`, and `TeamsOnTeamMemberAddedTriggerPayload` added; static `TeamsTriggers.Operations` dictionary maps operation names to payload types for dynamic dispatch (#170)
- **OpenTelemetry distributed tracing on all generated clients** — each generated client has a per-connector `ConnectorActivitySource` (e.g., `Azure.Connectors.Sdk.teams`, `Azure.Connectors.Sdk.office365`) and every operation starts an `Activity`, enabling end-to-end traceability when subscribed through `ActivitySource` listeners or OpenTelemetry exporters. Subscribe to `Azure.Connectors.Sdk.*` to capture all connector operations. (#183)
- **`ConnectorException` now parses the connector error code** — the response body's error code is extracted to populate `RequestFailedException.ErrorCode`, giving callers a structured error code alongside `Status`, `Message`, and `ResponseBody` (#180)
- **`[EditorBrowsable(EditorBrowsableState.Never)]` on inherited `Object` methods** — all generated clients now suppress `Equals`, `GetHashCode`, and `ToString` from IntelliSense autocomplete, reducing noise when working with client instances ([#160](https://github.com/Azure/Connectors-NET-SDK/issues/160))

### Changed

- **Regenerated all 96 connector clients** from combined BPM generator improvements: Microsoft copyright header on every generated file ([#158](https://github.com/Azure/Connectors-NET-SDK/issues/158)), `[EditorBrowsable(EditorBrowsableState.Never)]` on inherited `Object` methods ([#160](https://github.com/Azure/Connectors-NET-SDK/issues/160)), `protected` mock constructors now chain to `base()` ([#159](https://github.com/Azure/Connectors-NET-SDK/issues/159)), and null-guard hardening ([#175](https://github.com/Azure/Connectors-NET-SDK/issues/175))

## [0.11.0-preview.1] - 2026-05-15

### Fixed

- **`TriggerCallbackBody<T>` now handles both batch and single-item callback shapes** — Connector Namespace delivers trigger callbacks in two shapes depending on the trigger configuration's splitOn setting: batch `{"body":{"value":[...]}}` and single-item `{"body":{...item...}}`. The new `TriggerCallbackBodyConverter<T>` transparently normalizes both shapes into `Body.Value` as a list, preventing silent zero-item processing when splitOn is enabled. All 77+ generated `TriggerCallbackPayload<T>` subclasses inherit this fix automatically. (#149)

### Breaking Changes

- **`TriggerCallbackPayload<T>.Body` is now init-only** — the setter changed from `public set` to `init`. Post-construction assignment (`payload.Body = x;`) no longer compiles; use an object initializer or `ConnectorModelFactory.TriggerCallbackPayload<T>(body)` instead.
- **`TriggerCallbackBody<T>.Value` setter is now internal and the type narrowed to `IReadOnlyList<T>?`** — the property changed from `public List<T>? Value { get; set; }` to `public IReadOnlyList<T>? Value { get; internal set; }`. External assignments (`body.Value = list;`) and `List<T>`-specific mutations no longer compile; use `ConnectorModelFactory.TriggerCallbackBody<T>(value)` to construct instances in tests.
- **Removed `CamelCase` JSON naming policy** from `ConnectorClientBase.JsonOptions` and `ConnectorJsonSerializer` — properties without `[JsonPropertyName]` attributes now serialize using their C# PascalCase names, matching swagger/connector API expectations. Properties with `[JsonPropertyName]` are unaffected. Also changed `JsonStringEnumConverter` to use default casing instead of camelCase. (#84, #85)
- **Renamed `AzuremonitorlogsClient` to `AzureMonitorLogsClient`** and `Office365usersClient` to `Office365UsersClient` for consistent PascalCase naming (#126)
  - Namespaces updated: `Azure.Connectors.Sdk.Azuremonitorlogs` → `Azure.Connectors.Sdk.AzureMonitorLogs`, `Azure.Connectors.Sdk.Office365users` → `Azure.Connectors.Sdk.Office365Users`
  - DI extension methods renamed: `AddAzuremonitorlogsClient` → `AddAzureMonitorLogsClient`, `AddOffice365usersClient` → `AddOffice365UsersClient`
  - Model factories renamed: `AzuremonitorlogsModelFactory` → `AzureMonitorLogsModelFactory`, `Office365usersModelFactory` → `Office365UsersModelFactory`
  - `ConnectorNames` constants renamed: `ConnectorNames.Azuremonitorlogs` → `ConnectorNames.AzureMonitorLogs`, `ConnectorNames.Office365users` → `ConnectorNames.Office365Users`
- **Made `IPageable<T>` internal** — this interface is now an internal deserialization contract only; generated clients already return `AsyncPageable<T>` from Azure.Core publicly (#127)
- **Changed `ConnectorClientBase.CreatePageable` from `protected` to `private protected`** — only accessible to derived classes within the assembly; external subclasses of `ConnectorClientBase` cannot call this method directly (#127)
- **Made JSON converter types internal** — `Iso8601DateTimeConverter`, `Iso8601TimeSpanConverter`, `NullableTimeSpanConverter` are serialization infrastructure not intended for direct consumer use (#124)

### Added

- **Constructor overload `(Uri, TokenCredential)` without `ClientOptions` parameter** on `ConnectorClientBase` and all 12 generated clients, following the [Azure SDK constructor guideline](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-client-constructor-minimal) (#123)
- **`ConnectorHttpClient` now supports mocking** — added protected parameterless constructor and marked `SendAsync` as virtual (#125)
- **5 new connector clients** — `ExcelOnlineClient`, `AzureEventGridClient`, `YammerClient`, `WdatpClient` (Microsoft Defender ATP), `UniversalPrintClient` (#7)
- **15 more connector clients (batch 2)** — `CampfireClient`, `ClickSendSmsClient`, `CloudmersiveConvertClient`, `EtsyClient`, `FormstackFormsClient`, `FreshServiceClient`, `InfusionsoftClient`, `InsightlyClient`, `PipedriveClient`, `PlivoClient`, `PlumsailClient`, `RepliconClient`, `RevaiClient`, `SigningHubClient`, `ZohoSignClient` (#7)
- **15 more connector clients (batch 3)** — `DocuwareClient`, `ElfsquadDataClient`, `ImpexiumClient`, `JedoxOdataHubClient`, `MeetingRoomMapClient`, `OrderfulClient`, `PdfCoClient`, `ProjectplaceClient`, `SeismicPlannerClient`, `StarmindClient`, `StarrezRestV1Client`, `TallyfyClient`, `TextRequestClient`, `TicketmasterClient`, `WaywedoClient` (#7)
- **13 Microsoft 1st-party connector clients (batch 4)** — `AzureAutomationClient`, `AzureDataFactoryClient`, `AzureDigitalTwinsClient`, `AzureVMClient`, `KeyVaultClient`, `MicrosoftBookingsClient`, `Office365GroupsClient`, `Office365GroupsMailClient`, `OnenoteClient`, `PlannerClient`, `PowerBIClient`, `ShiftsClient`, `TodoClient` (#7)
- **11 connector clients (batch 5)** — `AzureADClient`, `AzureIoTCentralClient`, `MicrosoftFormsClient` regenerated with generator bug fixes; plus 8 new clients: `AzureQueuesClient`, `AzureTablesClient`, `DocumentDbClient`, `EventHubsClient`, `ExcelOnlineBusinessClient`, `OutlookClient`, `ServiceBusConnectorClient`, `WordOnlineBusinessClient`; also fixes generator bugs #135, #136, #137, #138, #139 (IPageable property name derived from x-ms-summary; array-typed `$ref` definitions resolved to `List<T>` instead of undefined class name)
- **25 new connector clients (batch 6)** — `BoxClient`, `DocusignClient`, `DropboxClient`, `DynamicsaxClient`, `EventbriteClient`, `FtpClient`, `GithubClient`, `GooglecalendarClient`, `GoogledriveClient`, `GoogletasksClient`, `JiraClient`, `MailchimpClient`, `MondayClient`, `OnedriveClient` (personal OneDrive), `RssClient`, `SalesforceClient`, `SendgridClient`, `SlackClient`, `SqlClient`, `TrelloClient`, `TwitterClient`, `TypeformClient`, `WebexClient`, `WordpressClient`, `ZendeskClient`

### Changed

- **Regenerated all 12 connector clients** from updated CodefulSdkGenerator with PascalCase name overrides and constructor additions
- **Regenerated 15 existing connector clients** with latest generator bug fixes — `AzureMonitorLogsClient`, `AzureTablesClient`, `DocumentDbClient`, `ExcelOnlineBusinessClient`, `ExcelOnlineClient`, `InfusionsoftClient`, `Office365Client`, `Office365GroupsClient`, `Office365UsersClient`, `OneDriveForBusinessClient`, `PipedriveClient`, `PlumsailClient`, `SmtpClient`, `WdatpClient`, `YammerClient`

## [0.10.0-preview.1] - 2026-05-11

### Breaking Changes

- **Removed `CamelCase` JSON naming policy** from `ConnectorClientBase.JsonOptions` and `ConnectorJsonSerializer` — properties without `[JsonPropertyName]` attributes now serialize using their C# PascalCase names, matching swagger/connector API expectations. Properties with `[JsonPropertyName]` are unaffected. Also changed `JsonStringEnumConverter` to use default casing instead of camelCase. (#84, #85)
- **Renamed `AzuremonitorlogsClient` to `AzureMonitorLogsClient`** and `Office365usersClient` to `Office365UsersClient` for consistent PascalCase naming (#126)
  - Namespaces updated: `Azure.Connectors.Sdk.Azuremonitorlogs` → `Azure.Connectors.Sdk.AzureMonitorLogs`, `Azure.Connectors.Sdk.Office365users` → `Azure.Connectors.Sdk.Office365Users`
  - DI extension methods renamed: `AddAzuremonitorlogsClient` → `AddAzureMonitorLogsClient`, `AddOffice365usersClient` → `AddOffice365UsersClient`
  - Model factories renamed: `AzuremonitorlogsModelFactory` → `AzureMonitorLogsModelFactory`, `Office365usersModelFactory` → `Office365UsersModelFactory`
  - `ConnectorNames` constants renamed: `ConnectorNames.Azuremonitorlogs` → `ConnectorNames.AzureMonitorLogs`, `ConnectorNames.Office365users` → `ConnectorNames.Office365Users`
- **Made `IPageable<T>` internal** — this interface is now an internal deserialization contract only; generated clients already return `AsyncPageable<T>` from Azure.Core publicly (#127)
- **Changed `ConnectorClientBase.CreatePageable` from `protected` to `private protected`** — only accessible to derived classes within the assembly; external subclasses of `ConnectorClientBase` cannot call this method directly (#127)
- **Made JSON converter types internal** — `Iso8601DateTimeConverter`, `Iso8601TimeSpanConverter`, `NullableTimeSpanConverter` are serialization infrastructure not intended for direct consumer use (#124)

### Added

- **Constructor overload `(Uri, TokenCredential)` without `ClientOptions` parameter** on `ConnectorClientBase` and all 12 generated clients, following the [Azure SDK constructor guideline](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-client-constructor-minimal) (#123)
- **`ConnectorHttpClient` now supports mocking** — added protected parameterless constructor and marked `SendAsync` as virtual (#125)
- **5 new connector clients** — `ExcelOnlineClient`, `AzureEventGridClient`, `YammerClient`, `WdatpClient` (Microsoft Defender ATP), `UniversalPrintClient` (#7)
- **15 more connector clients (batch 2)** — `CampfireClient`, `ClickSendSmsClient`, `CloudmersiveConvertClient`, `EtsyClient`, `FormstackFormsClient`, `FreshServiceClient`, `InfusionsoftClient`, `InsightlyClient`, `PipedriveClient`, `PlivoClient`, `PlumsailClient`, `RepliconClient`, `RevaiClient`, `SigningHubClient`, `ZohoSignClient` (#7)
- **15 more connector clients (batch 3)** — `DocuwareClient`, `ElfsquadDataClient`, `ImpexiumClient`, `JedoxOdataHubClient`, `MeetingRoomMapClient`, `OrderfulClient`, `PdfCoClient`, `ProjectplaceClient`, `SeismicPlannerClient`, `StarmindClient`, `StarrezRestV1Client`, `TallyfyClient`, `TextRequestClient`, `TicketmasterClient`, `WaywedoClient` (#7)
- **13 Microsoft 1st-party connector clients (batch 4)** — `AzureAutomationClient`, `AzureDataFactoryClient`, `AzureDigitalTwinsClient`, `AzureVMClient`, `KeyVaultClient`, `MicrosoftBookingsClient`, `Office365GroupsClient`, `Office365GroupsMailClient`, `OnenoteClient`, `PlannerClient`, `PowerBIClient`, `ShiftsClient`, `TodoClient` (#7)

### Changed

- **Regenerated all 12 connector clients** from updated CodefulSdkGenerator with PascalCase name overrides and constructor additions

## [0.9.0-preview.1] - 2026-05-08

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
- **Regenerated all 12 connector clients** from CodefulSdkGenerator to ensure consistency with Azure SDK design changes — no manual edits remain in generated files

### Added

- **Extensible enum types for Swagger enum properties** (#115) — string properties with Swagger `enum` arrays are now generated as `readonly struct` types following the Azure SDK extensible enum pattern. Each struct provides static members for known values, implicit `string` conversion, case-insensitive equality, and a nested `JsonConverter` for `System.Text.Json` serialization.
- **DI integration extension methods** (`AddOffice365Client`, `AddTeamsClient`, etc.) — register connector clients as singletons from an `IConfiguration` section, eliminating ~15 lines of boilerplate per connector in Azure Functions `Program.cs`. Resolves `TokenCredential` from DI or defaults to system-assigned managed identity. (#116)
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

[Unreleased]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.14.0-preview.1...HEAD
[0.14.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.13.0-preview.1...v0.14.0-preview.1
[0.13.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.12.0-preview.1...v0.13.0-preview.1
[0.12.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.11.0-preview.1...v0.12.0-preview.1
[0.11.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.10.0-preview.1...v0.11.0-preview.1
[0.10.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.9.0-preview.1...v0.10.0-preview.1
[0.9.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.8.0-preview.1...v0.9.0-preview.1
[0.8.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.7.0-preview.1...v0.8.0-preview.1
[0.7.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.6.0-preview.1...v0.7.0-preview.1
[0.6.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.5.0-preview.1...v0.6.0-preview.1
[0.5.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.4.0-preview.1...v0.5.0-preview.1
[0.4.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.3.0-preview.1...v0.4.0-preview.1
[0.3.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.2.0-preview.1...v0.3.0-preview.1
[0.2.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/compare/v0.1.0-preview.1...v0.2.0-preview.1
[0.1.0-preview.1]: https://github.com/Azure/Connectors-NET-SDK/releases/tag/v0.1.0-preview.1
