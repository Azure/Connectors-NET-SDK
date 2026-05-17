# Azure SDK Guideline Compliance

This document explains which [Azure SDK design guidelines for .NET](https://azure.github.io/azure-sdk/dotnet_introduction.html) the Connectors SDK follows, where it intentionally diverges, and which gaps are actively being addressed.

---

## Section 1: Followed Guidelines

The SDK follows Azure SDK conventions wherever they apply without conflict with the connector-layer architecture.

### Naming and API Shape

- **`Async` suffix** — All I/O-bound operations are named `*Async` (e.g., `SendEmailAsync`, `GetAllTeamsAsync`).
- **`CancellationToken` as last parameter** — Every async method accepts a `CancellationToken cancellationToken = default` as its final parameter.
- **PascalCase types and methods** — All public types and methods follow PascalCase naming.
- **Namespace `Azure.*`** — The library is rooted at `Azure.Connectors.Sdk`, consistent with the `Azure.*` namespace convention.

### Client Construction

- **`ClientOptions` inheritance** — `ConnectorClientOptions` inherits from `Azure.Core.ClientOptions`, giving callers the standard `Retry`, `Transport`, and `Diagnostics` properties.
- **`TokenCredential` acceptance** — Every client accepts a `TokenCredential` (from `Azure.Core`) for authentication, defaulting to `ManagedIdentityCredential`.
- **Overloaded constructors** — Clients expose both `Uri` and `string` overloads for the connection runtime URL, following the pattern for progressive disclosure.
- **`IDisposable` implementation** — All clients implement `IDisposable` via `ConnectorClientBase`.
- **Parameterless constructor for mocking** — A protected parameterless constructor is provided so mocking frameworks (Moq, NSubstitute) can create test doubles without real infrastructure.

### HTTP Infrastructure

- **`Azure.Core.HttpPipeline`** — All HTTP traffic routes through `HttpPipelineBuilder.Build(...)`, giving callers access to the full Azure.Core pipeline (retry, transport, per-call policies).
- **`BearerTokenAuthenticationPolicy`** — The SDK uses the Azure.Core bearer-token policy for authentication rather than manual header injection.
- **Retry configuration** — Retry behavior is configured via `ClientOptions.Retry`, consistent with all Azure SDK clients.
- **`ConfigureAwait(false)`** — All `await` calls use `ConfigureAwait(continueOnCapturedContext: false)` throughout the infrastructure layer.

### Error Handling

- **`RequestFailedException` inheritance** — `ConnectorException` inherits from `Azure.RequestFailedException`. Callers that already catch `RequestFailedException` for other Azure SDK clients will automatically catch connector errors with no additional catch blocks.

### Code Generation and Tooling

- **`[DynamicValues]` and `[DynamicSchema]` attributes** — Dynamic lookup parameters and schema-dependent properties are annotated with custom attributes, enabling tooling (LSP, IntelliSense) to provide live schema support.

---

## Section 2: Intentional Divergences

The SDK targets a specific architectural layer — it is a typed wrapper around the Azure-hosted connector runtime, not a direct client for a REST endpoint the SDK team owns. Several Azure SDK guidelines were designed for direct-service clients and do not translate cleanly to this model. The divergences below are intentional.

### Return `T` instead of `Response<T>`

| Dimension | Azure SDK guideline | This SDK |
|-----------|---------------------|----------|
| Service method return type | `Response<T>` | `T` directly (or `Task` for void operations) |

**Rationale:** `Response<T>` exists so callers can access raw HTTP metadata (status code, headers, request ID). In the Connectors SDK, the connector runtime is a managed intermediary, not the final service. The raw HTTP response from the connector runtime reflects connector-layer plumbing, not the operation's semantic outcome. Callers need the deserialized model (`T`), not connector-layer HTTP headers. Exposing `Response<T>` would surface internal connector transport details that are neither stable nor meaningful to the application.

Pagination operations (`AsyncPageable<T>`) follow Azure SDK conventions because they do carry meaningful metadata (page continuation tokens).

### No full `DiagnosticScope` / distributed tracing

| Dimension | Azure SDK guideline | This SDK |
|-----------|---------------------|----------|
| Distributed tracing | `DiagnosticScope` around each service call | Not implemented |

**Rationale:** The connector runtime (API Hub) already provides its own observability layer — it traces operations end-to-end across the connector and the SaaS backend. Adding a `DiagnosticScope` in the SDK would create duplicate (and potentially misleading) spans that do not align with the connector's internal trace boundaries. When the connector layer exposes a correlation mechanism, the SDK will integrate with it (tracked in [#156](https://github.com/Azure/Connectors-NET-SDK/issues/156)).

### `object` for dynamic-schema properties

| Dimension | Azure SDK guideline | This SDK |
|-----------|---------------------|----------|
| Unknown/variable JSON | `BinaryData` or `JsonElement` | `object` |

**Rationale:** Connector action schemas are runtime-defined. A Teams "Post Message" operation has a `body` shape that differs from an Office 365 "Send Email" body; the exact structure for dynamic properties is determined at runtime by the connector's Swagger definition, not at SDK compile time. Using `object` preserves the connector-defined structure through `System.Text.Json` serialization without requiring callers to round-trip through `BinaryData`. Migration to `BinaryData` or `JsonElement` is tracked in [#157](https://github.com/Azure/Connectors-NET-SDK/issues/157) with a plan to evaluate the trade-offs once the full set of dynamic schema patterns is characterized.

### No per-connector `ClientOptions` subclass

| Dimension | Azure SDK guideline | This SDK |
|-----------|---------------------|----------|
| Per-client options | Each client ships its own `*ClientOptions` | Single shared `ConnectorClientOptions` |

**Rationale:** All 80+ generated connector clients share the same infrastructure: the same `HttpPipeline`, the same authentication flow, the same retry policy surface. Per-connector options classes would be identical stubs with no connector-specific properties, adding noise without benefit. `ConnectorClientOptions` serves all clients uniformly. If a connector eventually requires client-specific configuration, a typed subclass can be introduced without breaking existing callers.

---

## Section 3: Tracked Improvements

The following known gaps are being addressed incrementally. They are not intentional divergences — they are pending fixes.

| Issue | Description | Status |
|-------|-------------|--------|
| [#155](https://github.com/Azure/Connectors-NET-SDK/issues/155) | `ConnectorException` does not parse a structured `ErrorCode` from the response body | Pending fix |
| [#156](https://github.com/Azure/Connectors-NET-SDK/issues/156) | No `DiagnosticScope` distributed tracing | Pending — see Section 2 for rationale on the current design |
| [#157](https://github.com/Azure/Connectors-NET-SDK/issues/157) | `object` used for dynamic-schema properties instead of `BinaryData`/`JsonElement` | Under evaluation |
| [#158](https://github.com/Azure/Connectors-NET-SDK/issues/158) | Missing Microsoft copyright headers on generated `*Extensions.cs` files | Fix in PR `fix/copyright-headers` |
| [#159](https://github.com/Azure/Connectors-NET-SDK/issues/159) | Mock constructors do not chain to the protected parameterless base constructor correctly | Fix in PR `fix/mock-constructor-chain` |
| [#160](https://github.com/Azure/Connectors-NET-SDK/issues/160) | `[EditorBrowsable(Never)]` missing on inherited `object` methods (`Equals`, `GetHashCode`, `ToString`, `GetType`) | Pending fix |
| [#161](https://github.com/Azure/Connectors-NET-SDK/issues/161) | Output-only properties use `{ get; set; }` instead of `{ get; init; }` | Pending fix |

---

## Further Reading

- [Azure SDK .NET design guidelines](https://azure.github.io/azure-sdk/dotnet_introduction.html)
- [docs/concepts.md](concepts.md) — Architecture overview and glossary
- [docs/connection-setup.md](connection-setup.md) — Connection provisioning walkthrough
- [docs/migration-from-private-preview.md](migration-from-private-preview.md) — Migration guide from the private preview SDK
- [GENERATION.md](../GENERATION.md) — How connector clients are generated
