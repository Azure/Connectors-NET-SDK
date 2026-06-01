# Azure SDK Guideline Compliance

This document explains which [Azure SDK design guidelines for .NET](https://azure.github.io/azure-sdk/dotnet_introduction.html) the Connectors SDK follows, where it intentionally diverges, and what work is in progress.

The decisions recorded here were made during a formal API design review in May 2026 with Azure SDK team reviewers (Anu Thomas, Steven Vukelich) and the APIView AI reviewer. The detailed per-suggestion analysis — 30 suggestions, each with rationale and a recorded decision — lives in [`docs/API-DESIGN-EVALUATION.md`](https://github.com/daviburg_microsoft/azure-logicapps-connector-sdk/blob/main/docs/API-DESIGN-EVALUATION.md) (internal). This document is the summary reference.

This document was reconciled rule-by-rule against the live [.NET design](https://azure.github.io/azure-sdk/dotnet_introduction.html), [.NET implementation](https://azure.github.io/azure-sdk/dotnet_implementation.html), [general design](https://azure.github.io/azure-sdk/general_design.html), and [general implementation](https://azure.github.io/azure-sdk/general_implementation.html) guideline pages on 2026-06-01; every normative `DO` / `DO NOT` / `SHOULD` is accounted for as Followed, an Intentional Divergence, Not Applicable, or Deferred. Note this SDK is **pre-1.0** (currently `0.9.0-preview.1`), so source- and binary-breaking changes are permitted ahead of GA where they improve the long-term API.

---

## Section 1: Followed Guidelines

| Guideline | Implementation |
|-----------|---------------|
| `Azure.*` namespace convention | Root namespace is `Azure.Connectors.Sdk`; generated clients use `Azure.Connectors.<Connector>` |
| `ClientOptions` inheritance | `ConnectorClientOptions : ClientOptions` — callers get standard `Retry`, `Transport`, and `Diagnostics` properties |
| `ServiceVersion` enum (version-first, enum-from-1, `0` reserved) | `ConnectorClientOptions(ServiceVersion version = ServiceVersion.V1)` — `version` is the first parameter and defaults to the latest version (per the guideline's own canonical example), the enum uses explicit values starting at `1`, and the reserved value `0` throws `ArgumentException` |
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
| `[EditorBrowsable(Never)]` on inherited `Object` methods | Generated clients suppress `Equals`, `GetHashCode`, and `ToString` from IntelliSense to reduce API noise ([#160](https://github.com/Azure/Connectors-NET-SDK/issues/160)) |
| `ErrorCode` from structured error responses | `ConnectorException` parses `"code"` (top-level or nested `"error.code"`) from JSON response bodies to populate `RequestFailedException.ErrorCode` ([#155](https://github.com/Azure/Connectors-NET-SDK/issues/155)) |
| Service client `Client` suffix | Every generated client is named `<Connector>Client` (e.g. `KustoClient`, `Office365Client`) |
| Immutable, reference-type clients | `ConnectorClientBase` and all clients are `class` types with `readonly` fields — never structs, and not mutable after construction |
| Thread-safe clients | All public client members are safe for concurrent use; client state is immutable after construction |
| `ClientOptions` subclass `Options` suffix | `ConnectorClientOptions` follows the `<Client>Options` naming convention |
| Credential rotation without a new client | `BearerTokenAuthenticationPolicy` re-reads the `TokenCredential` on every request, so rotated credentials take effect without reconstructing the client |
| `System.Text.Json` for serialization | All models serialize/deserialize via `System.Text.Json`; there is no `Newtonsoft.Json` dependency |
| No APIs in the second-level namespace | Public types live in `Azure.Connectors.Sdk` and `Azure.Connectors.<Connector>` (third level), never directly under `Azure.*` |
| Dependency allow-list | Package references are limited to `Azure.*`, `System.*`, and `Microsoft.Extensions.*` packages (`Directory.Packages.props`) |
| No native dependencies | Pure managed code; no native/interop dependencies |
| `MAJOR.MINOR.PATCH` versioning | Version is `MAJOR.MINOR.PATCH` with a pre-release suffix (`eng/build/Version.props`, currently `0.9.0-preview.1`) |
| No default parameters in simplest client constructor | Simplest constructor `Client(Uri connectionRuntimeUrl)` takes only the required binding parameter with no defaults; options/credentials overloads add optional parameters (standard Azure SDK pattern) |
| Package name matches main namespace | NuGet package ID is `Azure.Connectors.Sdk`, matching the root namespace |
| Version-resilient serialization | Optional value-type model properties are `Nullable<T>` (`bool?`, `DateTime?`, `int?`); generated files use `#nullable disable` so reference-type properties accept `null` gracefully; `System.Text.Json` ignores unknown properties during deserialization |
| No non-guideline dependency types in public API | All types exposed in the public API surface come from `Azure.Core` (`ClientOptions`, `TokenCredential`, `HttpPipeline`, `RequestFailedException`, `AsyncPageable<T>`), `System.*`, or `Microsoft.Extensions.*` — all on the approved dependency list |
| Configuration via `ClientOptions` inheritance | Each client accepts `ConnectorClientOptions : ClientOptions`, which inherits global retry, transport, and diagnostics configuration; different client instances can use different options; configuration is immutable after construction |

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

### Output-only model properties use `init` setters, not `internal set` ([#161](https://github.com/Azure/Connectors-NET-SDK/issues/161))

| Guideline | This SDK |
|-----------|----------|
| Output-only model properties use `{ get; internal set; }` so only the deserializer (in the same assembly) can populate them | `{ get; init; }` |

**Rationale:** The [Model Types guidance](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-model-types) prescribes `internal set` for output-only properties (e.g. its `Locked` example uses `public bool Locked { get; internal set; }`) so the value is immutable to callers while remaining settable by same-assembly deserialization. This SDK uses `init` instead because:

- **True immutability.** `init` makes the property unsettable after object construction by *anyone* — including same-assembly code — which is a stronger guarantee than `internal set`. `System.Text.Json` populates `init` setters during deserialization via object-initializer semantics, so the deserialization path still works without `[JsonInclude]` on internal members.
- **Test and mocking ergonomics.** `init` setters are usable from object initializers in test code and from the generated `*ModelFactory` classes, so unit tests can construct fully-populated model instances without reflection. `internal set` would require `[InternalsVisibleTo]` or factory-only construction.
- **No polyfill cost.** The usual objection to `init` — that `IsExternalInit` is missing on older targets — does not apply: this SDK targets **net8.0 only**, where `System.Runtime.CompilerServices.IsExternalInit` is built into the framework.

**Trade-off:** `init` is part of the public API surface (callers see it in IntelliSense as settable at construction time), whereas `internal set` hides the setter entirely. The team accepts this surface in exchange for true immutability and test ergonomics. Affects ~493 output-only properties across generated models.

---

### Required reference-type parameters get null guards even when service-bound ([#175](https://github.com/Azure/Connectors-NET-SDK/issues/175))

| Guideline | This SDK |
|-----------|----------|
| DO validate client parameters; **DO NOT** validate service parameters — let the service validate its own parameters | Required reference-type parameters that are dereferenced during request construction get an explicit `ArgumentNullException` null guard, even when their value is service-bound (URL/query segment) |

**Rationale:** The [Parameter Validation guidance](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-parameter-validation) says not to reimplement the service's validation of *service parameters*. This SDK still null-guards required reference-type parameters (e.g. `method`, `requestingSiteId`, `lockTokenOfTheMessage`) because the guard protects **client-side request construction**, not service semantics:

- The generator appends these parameters to the URL/query string via `.ToString()`. A `null` value makes a well-formed request **impossible to build** — there is no service round-trip to defer the error to.
- The guard checks only for `null` (constructability), never the value's format, range, or business rules — the service still owns all value validation.
- The choice is purely between a clear `ArgumentNullException(nameof(param))` and an opaque `NullReferenceException` thrown deep inside request construction (generated files use `#nullable disable`).

Guards are emitted only for required reference-type parameters dereferenced during request construction; optional parameters and service-validated value content are untouched. Originally surfaced by Copilot review on PR [#170](https://github.com/Azure/Connectors-NET-SDK/pull/170).

---

### Per-method tracing uses `ActivitySource` directly, not `DiagnosticScope` ([#156](https://github.com/Azure/Connectors-NET-SDK/issues/156))

| Guideline | This SDK |
|-----------|----------|
| ✅ DO support distributed tracing using `ClientDiagnostics`/`DiagnosticScope`; ⛔️ **DO NOT use `DiagnosticSource` or `ActivitySource` API directly from client libraries** | Per-method operation spans created with `System.Diagnostics.ActivitySource` / `Activity` directly |

**Rationale:** The [.NET implementation tracing guidance](https://azure.github.io/azure-sdk/dotnet_implementation.html#dotnet-tracing) is explicit on both points: use `ClientDiagnostics`/`DiagnosticScope`, and *do not* use `ActivitySource` directly (`Activity` may be used only where `DiagnosticScope` lacks an API). This SDK knowingly diverges from that `DO NOT` and emits operation spans with raw `ActivitySource`/`Activity` because:

- **One tracing primitive.** `ConnectorHttpClient` already emits its HTTP spans via a raw `ActivitySource`. Using the same primitive for the method-level operation span keeps a single tracing mechanism instead of mixing `ActivitySource` (HTTP layer) and `DiagnosticScope` (method layer).
- **`DiagnosticScope` wraps `ActivitySource`.** Azure.Core's `DiagnosticScope` is itself built on `ActivitySource`/`Activity`; using `ActivitySource` directly produces the same parent/child span correlation that OpenTelemetry exporters consume, and HTTP spans become children of the operation `Activity` automatically via `Activity.Current`.
- **No `ClientDiagnostics` surface.** Avoids introducing `ClientDiagnostics` construction into every generated client for correlation the HTTP layer already establishes.

The per-method catch filters fatal exceptions (`!ex.IsFatal()`) so process-fatal conditions are never swallowed or annotated. Paginated (`AsyncPageable`) methods are traced at the HTTP-pipeline level only.

---

### Dynamic model properties use `JsonElement?`, not `object` ([#157](https://github.com/Azure/Connectors-NET-SDK/issues/157))

| Guideline | This SDK |
|-----------|----------|
| (No explicit Model Types rule on dynamic-property typing) | Properties holding arbitrary JSON use `JsonElement?` instead of `object` |

**Rationale:** The [Model Types guidance](https://azure.github.io/azure-sdk/dotnet_introduction.html#dotnet-model-types) does not prescribe a type for dynamically-typed (free-form JSON) properties — this is an unguided choice made on engineering merits. `JsonElement?` is preferred over `object` because `System.Text.Json` already materializes these values as `JsonElement` at runtime, so the type signature becomes honest; the JSON structure is preserved for caller navigation without re-parsing; and it matches the same files' existing `[JsonExtensionData] Dictionary<string, JsonElement>` usage. `JsonElement?` (nullable) also distinguishes an absent property from `JsonValueKind.Null`.

**Breaking change:** This changes the type of affected properties (`MCPQueryResponse`, `MCPQueryRequest`, and similar). Consumers that assigned arbitrary .NET objects must now pre-serialize to `JsonElement`. Intentional and acceptable for the pre-1.0 SDK.

---

### Single target framework: `net8.0` only, no `netstandard2.0`

| Guideline | This SDK |
|-----------|----------|
| DO build all libraries for `netstandard2.0` (plus the current LTS .NET) | Targets `net8.0` only (`eng/build/Engineering.props`) |

**Rationale:** The `netstandard2.0` requirement exists so libraries remain usable from .NET Framework and other legacy runtimes. This SDK's only consumer surface is modern Azure Functions / .NET 8+ hosts — there is no .NET Framework or netstandard consumer. Targeting `net8.0` only eliminates polyfill shim packages and lets the generator rely on built-in framework features (`IsExternalInit` for the `init` setters above, `ArgumentNullException.ThrowIfNull`, collection expressions, nullable reference types) without the multi-TFM API-parity burden. **This is a confirmed decision:** there is no ask for legacy (.NET Framework / netstandard) client consumption, and the cost and constraints of multi-targeting older frameworks are not justifiable without an explicit customer need. Revisit only if such a need is raised.

---

### Namespace group `Azure.Connectors` is not a pre-approved group

| Guideline | This SDK |
|-----------|----------|
| DO use one of the pre-approved namespace groups (`Azure.AI`, `Azure.Data`, `Azure.Messaging`, …) and register namespaces with adparch | Root `Azure.Connectors.Sdk`; clients in `Azure.Connectors.<Connector>` |

**Rationale:** `Connectors` is not on the published pre-approved group list. The SDK still honors the structural rules around it — the `Azure.<group>.<service>` shape, the `Client` suffix, and **no APIs in the second-level namespace**. The `Azure.Connectors` group reflects the Logic Apps connector domain and is the natural home for the 90+ generated connector clients.

**Disclosure: the `Azure.Connectors` namespace group is not yet approved by the Azure SDK Architecture Board ([adparch](https://github.com/azure/azure-sdk/issues)).** Registration and approval are pending and tracked outside this repo. The group name may change if the Board requires a different grouping before GA; consumers should treat the namespace as provisional until approval is recorded.

---

### DI extensions use `IServiceCollection`, not the `Microsoft.Extensions.Azure` builder

| Guideline | This SDK |
|-----------|----------|
| DO provide `*ClientBuilderExtensions` in the `Microsoft.Extensions.Azure` namespace using `IAzureClientFactoryBuilder.RegisterClientFactory` | `ConnectorServiceCollectionExtensions.Add<Connector>Client(IServiceCollection, IConfiguration)` in `Azure.Connectors.Sdk` |

**Rationale:** The `Microsoft.Extensions.Azure` builder pattern centers on a single `TokenCredential` flowing through `IAzureClientFactoryBuilderWithCredential`. Connector clients instead bind to a per-connection runtime URL plus connection-level identity sourced from `IConfiguration` (app settings), not one account credential — so an `IServiceCollection` + `IConfiguration` registration maps directly onto the Functions configuration model connector consumers already use. Adopting `IAzureClientFactoryBuilder` would add a `Microsoft.Extensions.Azure` dependency without matching the connection-per-connector binding model. This divergence is accepted and documented as the SDK's standard DI integration pattern. ([#116](https://github.com/Azure/Connectors-NET-SDK/issues/116))

---

## Section 3: Not Applicable

These guidelines target hand-authored, first-party Azure *service* SDKs. They do not apply here because this SDK generates flat clients from 1,500+ external connector swagger definitions and exposes no Azure-style resource hierarchy or long-running-operation model.

| Guideline | Why not applicable |
|-----------|--------------------|
| Synchronous `Pageable<T>` variants | Subsumed by the async-only decision in Section 2; paging is async-only via `AsyncPageable<T>` |
| Long-Running Operations (`Operation<T>`, `WaitUntil`) | Connector operations are request/response; no service exposes Azure-style LRO polling through the connection runtime |
| Subclients and `Get<Child>Client()` factory methods | Each connector is a single flat client; there is no resource hierarchy to model |
| Standard verb method naming (`Create`/`Set`/`Get`/`Delete`) | Method names are generated from swagger `operationId`s authored by external connector teams, not chosen by this SDK |
| Model types as `struct` / `IEquatable<T>` / `IComparable<T>` | Generated models are mutable JSON DTOs with no value-equality semantics |
| Conditional-request methods (`MatchConditions`/`RequestConditions`, `If-Match`) | Connector swaggers do not expose ETag-based optimistic concurrency |
| Custom `[Package]EventSource` logging | HTTP request/retry logging is handled by the `Azure.Core` `HttpPipeline`; there are no SDK-specific events worth logging |
| `Sample1_HelloWorld.md` sample naming / `#region` snippet conventions | Functional samples live in the separate `Connectors-NET-Samples` repo as runnable Azure Functions, not in-repo region snippets |
| Azure.Core `Argument` validation class | Generated guards use BCL `ArgumentNullException.ThrowIfNull`; the shared-source `Argument` class is an internal Azure.Core engineering-system convenience not consumed by this repo (see the parameter-guard divergence in Section 2) |
| Connection string constructors | No portal-provided connection string exists for Connector Namespace connections; clients authenticate via `Uri` + `TokenCredential`, the standard Azure SDK credential pattern |
| Repeatable request headers (`Repeatability-Request-ID` / `Repeatability-First-Sent`) | The connector runtime gateway manages request idempotency; this SDK does not add OASIS repeatability headers because the intermediary endpoint is not an OASIS-conformant service |
| Repository in `azure/azure-sdk-for-net` / Azure SDK engineering system | This SDK is published as an independent open-source repository ([`Azure/Connectors-NET-SDK`](https://github.com/Azure/Connectors-NET-SDK)) with its own CI/CD pipeline, not built inside the monorepo or with the Azure SDK engineering system tooling |
| Custom `HttpPipelinePolicy` subclasses | All HTTP policies (retry, auth, logging, telemetry) come from `Azure.Core` built-in policies; no SDK-specific custom policy is needed |
| Custom credential types | Standard `TokenCredential` from `Azure.Identity` covers all authentication scenarios; no custom credential type is defined |

---

## Section 4: Deferred

### `IAsyncDisposable`

**Decision (API design review, suggestion #18):** Deferred. Neither `HttpClient` nor `Azure.Core.HttpPipeline` implement `IAsyncDisposable` as of May 2026. Adding `IAsyncDisposable` today would be a no-op wrapper (`DisposeAsync` calls `Dispose`). Azure SDK's own clients (`BlobClient`, `ConfigurationClient`, etc.) also do not implement it. Revisit if a future .NET version adds async disposal support to `HttpPipeline`.

---

## Section 5: Active Work

The following items are in progress — not intentional divergences, but gaps being closed incrementally.

_No items currently in progress._

### Completed

Previously tracked items now delivered:

| Issue | Description | Delivered |
|-------|-------------|-----------|
| [#108](https://github.com/Azure/Connectors-NET-SDK/issues/108) | Internal visibility cleanup (`ExceptionExtensions`, `HttpExtensions`, `RetryPolicy`) | [#109](https://github.com/Azure/Connectors-NET-SDK/pull/109) |
| [#114](https://github.com/Azure/Connectors-NET-SDK/issues/114) | `.Models` sub-namespace + PascalCase client names | [#119](https://github.com/Azure/Connectors-NET-SDK/pull/119) |
| [#116](https://github.com/Azure/Connectors-NET-SDK/issues/116) | DI integration extensions (`Add<Connector>Client` for `IServiceCollection`) | [#117](https://github.com/Azure/Connectors-NET-SDK/pull/117) |
| [#155](https://github.com/Azure/Connectors-NET-SDK/issues/155) | `ConnectorException` parses structured `ErrorCode` from JSON response body | [#180](https://github.com/Azure/Connectors-NET-SDK/pull/180) |
| [#156](https://github.com/Azure/Connectors-NET-SDK/issues/156) | Per-method operation-span tracing via `ActivitySource` (see Section 2) | This PR |
| [#157](https://github.com/Azure/Connectors-NET-SDK/issues/157) | Dynamic model properties use `JsonElement?` instead of `object` (see Section 2) | This PR |
| [#160](https://github.com/Azure/Connectors-NET-SDK/issues/160) | `[EditorBrowsable(Never)]` on inherited `Object` methods | [#170](https://github.com/Azure/Connectors-NET-SDK/pull/170) |
| [#161](https://github.com/Azure/Connectors-NET-SDK/issues/161) | Output-only model properties use `init` setters (see Section 2) | This PR |
| [#174](https://github.com/Azure/Connectors-NET-SDK/issues/174) | Test coverage for nullable optional value-type parameters | [#180](https://github.com/Azure/Connectors-NET-SDK/pull/180) |
| [#175](https://github.com/Azure/Connectors-NET-SDK/issues/175) | `ArgumentNullException` guards for required reference-type parameters (see Section 2) | This PR |
| [#176](https://github.com/Azure/Connectors-NET-SDK/issues/176) | Test coverage for Teams trigger payload types | [#180](https://github.com/Azure/Connectors-NET-SDK/pull/180) |
| — | Remove `IConnectorClient` marker interface (`DO NOT use abstractions unless both returned and consumed`; `DO NOT use interfaces if abstract classes work`) — `ConnectorClientBase : IDisposable` directly | This PR |

---

## Further Reading

- [Azure SDK .NET design guidelines](https://azure.github.io/azure-sdk/dotnet_introduction.html)
- [Azure SDK .NET implementation guidelines](https://azure.github.io/azure-sdk/dotnet_implementation.html)
- [`docs/API-DESIGN-EVALUATION.md`](https://github.com/daviburg_microsoft/azure-logicapps-connector-sdk/blob/main/docs/API-DESIGN-EVALUATION.md) — Full per-suggestion decision log from the May 2026 API review (internal)
- [docs/concepts.md](concepts.md) — Architecture overview and glossary
- [docs/connection-setup.md](connection-setup.md) — Connection provisioning walkthrough
- [docs/migration-from-private-preview.md](migration-from-private-preview.md) — Migration guide from the private preview SDK
- [GENERATION.md](../GENERATION.md) — How connector clients are generated
