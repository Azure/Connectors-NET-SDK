# Azure SDK Guideline Compliance

This document explains which [Azure SDK design guidelines for .NET](https://azure.github.io/azure-sdk/dotnet_introduction.html) the Connectors SDK follows, where it intentionally diverges, and what work is in progress.

The decisions recorded here were made during a formal API design review in May 2026 with Azure SDK team reviewers (Anu Thomas, Steven Vukelich) and the APIView AI reviewer. The detailed per-suggestion analysis — 30 suggestions, each with rationale and a recorded decision — lives in [`docs/API-DESIGN-EVALUATION.md`](https://github.com/daviburg_microsoft/azure-logicapps-connector-sdk/blob/main/docs/API-DESIGN-EVALUATION.md) (internal). This document is the summary reference.

---

## Section 1: Followed Guidelines

| Guideline | Implementation |
|-----------|---------------|
| `Azure.*` namespace convention | Root namespace is `Azure.Connectors.Sdk`; generated clients use `Azure.Connectors.<Connector>` |
| `ClientOptions` inheritance | `ConnectorClientOptions : ClientOptions` — callers get standard `Retry`, `Transport`, and `Diagnostics` properties |
| `ServiceVersion` enum | `ConnectorClientOptions.ServiceVersion` with a required constructor parameter (defaults to `V1`) |
| `TokenCredential` authentication | All clients accept `Azure.Core.TokenCredential`; no custom `ITokenProvider` interface |
| `ManagedIdentityCredential` as production default | Simplest constructor uses `ManagedIdentityCredential(SystemAssigned)` — not `DefaultAzureCredential` (CodeQL SM05137) |
| `Uri` primary constructor | Primary constructor takes `Uri connectionRuntimeUrl`; `string` overload delegates to it as a convenience for app-settings scenarios |
| `HttpPipeline` for all HTTP | All traffic goes through `HttpPipelineBuilder.Build(...)` — retry, auth, and transport are pipeline policies |
| `BearerTokenAuthenticationPolicy` | Bearer token injection is a per-retry pipeline policy, not manual header code |
| `ConfigureAwait(false)` | All `await` calls use `ConfigureAwait(continueOnCapturedContext: false)` throughout infrastructure |
| `Async` suffix on all service methods | Every I/O operation is named `*Async` |
| `CancellationToken` as last parameter | Every service method accepts `CancellationToken cancellationToken = default` as its final parameter |
| `virtual` service methods | All generated client service methods are `virtual` to enable mocking without wrapper interfaces |
| Protected parameterless constructor | `ConnectorClientBase()` and each generated client provide a protected parameterless constructor for Moq/NSubstitute |
| `AsyncPageable<T>` for paginated results | Paginated operations return `Azure.Core.AsyncPageable<T>` |
| `RequestFailedException` inheritance | `ConnectorException : RequestFailedException` — `catch (RequestFailedException)` catches connector errors alongside other Azure SDK errors |
| `IDisposable` | All clients implement `IDisposable` |
| XML documentation on all public API | All public types and members carry `<summary>` documentation |
| Extensible enums for swagger-defined enum values | Properties with known swagger enum values use `readonly struct` with `implicit operator string` — type-safe IntelliSense without restricting unknown future values (design review suggestion #21) |
| `*ModelFactory` static classes for mocking | Each generated connector exposes a `<Connector>ModelFactory` with factory methods for constructing model instances in unit tests (design review suggestion #15) |
| Copyright headers on all generated files | Every generated `.cs` file carries the standard Microsoft copyright header ([#158](https://github.com/Azure/Connectors-NET-SDK/issues/158)) |
| Mock constructors chain to protected parameterless base | Each generated client's `protected` mock constructor calls `base()` so `Moq`/`NSubstitute` correctly initialise the base class ([#159](https://github.com/Azure/Connectors-NET-SDK/issues/159)) |

---

## Section 2: Intentional Divergences

These are decisions made after explicit review and confirmed as **Skip** — the guideline was evaluated and the conclusion was that following it would not benefit this SDK or would actively harm it.

### No synchronous method variants

| Guideline | This SDK |
|-----------|----------|
| Provide both async and sync variants for every service method | Async-only |

**Rationale (API design review, suggestion #5):** The guidelines' sync-variant requirement exists to support porting legacy synchronous desktop and server applications. This SDK targets Azure Functions, which are async-native. No legacy synchronous consumers exist or are anticipated. Adding sync wrappers using sync-over-async (`.GetAwaiter().GetResult()`) introduces deadlock risk in certain contexts and would double the API surface and test burden with no benefit.

---

### Return `T` instead of `Response<T>`

| Guideline | This SDK |
|-----------|----------|
| Service methods return `Task<Response<T>>` | Service methods return `Task<T>` directly (or `Task` for void operations) |

**Rationale (API design review, suggestion #7):** `Response<T>` is valuable when callers need raw HTTP metadata — status codes, headers, request ID — to make programmatic decisions or pass correlation IDs to downstream systems. For this SDK, the connection runtime endpoint is a managed intermediary, not a first-party Azure service. Its HTTP response metadata reflects connector-layer transport plumbing rather than the operation's semantic outcome, and is not a stable interface. No Azure SDK helper (LRO, test framework, DI extensions) depends on `Response<T>` in a way that would unlock concrete value for connector clients. Callers simply need the deserialized model.

Pagination returns `AsyncPageable<T>` (not `AsyncPageable<Response<T>>`) consistent with Azure SDK convention.

---

### `ETag` properties remain `string`

| Guideline | This SDK |
|-----------|----------|
| ETag values use `Azure.Core.ETag` | `string` |

**Rationale (API design review, suggestion #29):** `Azure.ETag` has no built-in `System.Text.Json` support. Azure SDK clients that use `ETag` avoid this by using manual `Utf8JsonReader`/`Utf8JsonWriter`; this SDK uses `JsonSerializer` throughout all generated models. Switching to `Azure.ETag` would require custom `JsonConverter<ETag>` registration on every deserialization call, and ETag field detection across 1,500+ external swagger definitions (which the team does not author) relies on name heuristics that are unreliable. The runtime deserialization failure risk outweighs the type-safety benefit.

---

### URI-valued model properties remain `string`

| Guideline | This SDK |
|-----------|----------|
| URI values use `System.Uri` | `string` |

**Rationale (API design review, suggestion #30):** `System.Uri` constructor throws `UriFormatException` on malformed input — if a connector returns an unexpected URL format, deserialization fails at runtime. `System.Text.Json` has no native `System.Uri` support; a custom converter would be required. Detecting which `string` properties hold URLs relies on name heuristics (`Url`, `URL`, `Link`, `Endpoint`, swagger `format: uri`) across 1,500+ external swagger definitions the team does not control; false positives and negatives are both likely. Same fundamental constraint as `ETag`: changing serialized model property types that `System.Text.Json` doesn't handle natively, on models whose schemas come from external parties.

---

### No options bag for many-parameter methods

| Guideline | This SDK |
|-----------|----------|
| Methods with more than ~6 parameters use an options bag type | Flat parameters with defaults |

**Rationale (API design review, suggestion #27):** The options-bag guideline targets hand-authored APIs where the SDK team controls the parameter surface. This SDK generates clients directly from 1,500+ external swagger definitions; each parameter maps 1:1 to a C# method parameter. An options bag would introduce a new public type with no corresponding swagger entity, add codegen heuristic complexity (threshold logic, edge cases when param counts change across swagger versions), and actually *worsen* the breaking-change story: adding a new optional parameter with a default value is non-breaking with flat params, but adding a property to an options bag type can be source-breaking. The flat-parameter approach is the honest, maintainable representation of external API contracts.

---

## Section 3: Deferred

### `IAsyncDisposable`

**Decision (API design review, suggestion #18):** Deferred. Neither `HttpClient` nor `Azure.Core.HttpPipeline` implement `IAsyncDisposable` as of May 2026. Adding `IAsyncDisposable` today would be a no-op wrapper (`DisposeAsync` calls `Dispose`). Azure SDK's own clients (`BlobClient`, `ConfigurationClient`, etc.) also do not implement it. Revisit if a future .NET version adds async disposal support to `HttpPipeline`.

---

## Section 4: Active Work

The following items are in progress — not intentional divergences, but gaps being closed incrementally.

| Issue / Suggestion | Description | Status |
|--------------------|-------------|--------|
| [#155](https://github.com/Azure/Connectors-NET-SDK/issues/155) | `ConnectorException` does not yet parse a structured `ErrorCode` from the response body | Pending fix |
| [#156](https://github.com/Azure/Connectors-NET-SDK/issues/156) | No `DiagnosticScope` distributed tracing | Pending |
| [#157](https://github.com/Azure/Connectors-NET-SDK/issues/157) | `object` used for dynamic-schema properties instead of `BinaryData`/`JsonElement` | Under evaluation |
| [#160](https://github.com/Azure/Connectors-NET-SDK/issues/160) | `[EditorBrowsable(Never)]` missing on inherited `Object` methods | Pending fix |
| [#161](https://github.com/Azure/Connectors-NET-SDK/issues/161) + design review #14 | Output-only model properties still use `{ get; set; }` instead of `{ get; init; }` — the companion `*ModelFactory` classes are already in place for when this lands | Pending fix |
| Design review #16 | Model types → `.Models` sub-namespace | Pending |
| Design review #17 | PascalCase / human-friendly generated client names | Pending |
| Design review #20/#22/#24 | Internal visibility cleanup (`ExceptionExtensions`, `HttpExtensions`, `RetryPolicy`) | Pending |
| Design review #23 | DI integration extensions (`Add<Connector>Client` for `IServiceCollection`) | Pending |

---

## Further Reading

- [Azure SDK .NET design guidelines](https://azure.github.io/azure-sdk/dotnet_introduction.html)
- [`docs/API-DESIGN-EVALUATION.md`](https://github.com/daviburg_microsoft/azure-logicapps-connector-sdk/blob/main/docs/API-DESIGN-EVALUATION.md) — Full per-suggestion decision log from the May 2026 API review (internal)
- [docs/concepts.md](concepts.md) — Architecture overview and glossary
- [docs/connection-setup.md](connection-setup.md) — Connection provisioning walkthrough
- [docs/migration-from-private-preview.md](migration-from-private-preview.md) — Migration guide from the private preview SDK
- [GENERATION.md](../GENERATION.md) — How connector clients are generated
