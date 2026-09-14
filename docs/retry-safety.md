# Retry safety

Automatic retries can duplicate connector side effects. A connector might commit an operation, such as sending a message or creating a file, before the client receives a transient HTTP response or an ambiguous transport exception. The client cannot determine from either failure whether the operation completed.

## Cross-language contract

Connector SDKs use one protocol-level safety rule:

- `GET`, `HEAD`, `OPTIONS`, and `TRACE` use the configured retry behavior by default.
- Every other HTTP method is unsafe by default and makes one attempt. This includes `POST`, `PUT`, `PATCH`, `DELETE`, and custom or extension methods.
- The same classification applies to retriable HTTP responses and retriable exceptions from later pipeline policies or the transport.
- A per-client, explicit opt-in applies the configured retry behavior to unsafe methods.

The safe allowlist is deliberately narrow. SDKs do not infer idempotency from a method name, request body, operation name, or connector metadata. This automatic-retry contract also does not declare every `GET` operation semantically idempotent or make an unsafe operation safe.

The behavior is language-neutral, but option names follow each language's conventions:

| SDK | Option | Status as of September 11, 2026 |
| --- | --- | --- |
| [.NET](https://github.com/Azure/Connectors-NET-SDK) | `RetryUnsafeHttpMethods` | Implemented in this repository; availability depends on the package version in use. |
| [Node.js](https://github.com/Azure/Connectors-NodeJS-SDK/pull/91) | `retryUnsafeHttpMethods` | Proposed in open PR #91; do not treat it as released until that repository publishes it. |
| [Python](https://github.com/Azure/connectors-python-sdk/issues/85) | Python-idiomatic name to be decided there | Tracked by open issue #85; not shipped. |

## .NET behavior

`ConnectorClientBase` applies the safe default automatically to generated clients. Eligible requests continue to use Azure.Core's standard retry implementation, including the inherited `Retry` settings for retry count, delay, mode, maximum delay, network timeout, and `Retry-After` handling. Diagnostics, authentication, custom transports, and user-added per-call and per-retry policies retain their Azure.Core pipeline positions.

Configure ordinary retry behavior through `ConnectorClientOptions.Retry`:

```csharp
var options = new ConnectorClientOptions();
options.Retry.MaxRetries = 3;
options.Retry.Delay = TimeSpan.FromSeconds(1);
options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
options.Retry.Mode = RetryMode.Exponential;
options.Retry.NetworkTimeout = TimeSpan.FromSeconds(100);
```

Unsafe methods still make one attempt with those settings. Opt in only when the service contract makes replay acceptable:

```csharp
var options = new ConnectorClientOptions
{
    RetryUnsafeHttpMethods = true,
};
```

When `RetryUnsafeHttpMethods` is `true`, unsafe methods use the same configured standard retry behavior as safe methods. Request content created by generated clients is buffered so JSON and binary bodies can be replayed.

### Custom policy precedence

A caller-supplied `ClientOptions.RetryPolicy` replaces Azure.Core's standard retry implementation and remains authoritative. The SDK does not replace or wrap it. Custom retry-policy authors own HTTP-method safety, and `RetryUnsafeHttpMethods` does not alter their policy.

### Pre-built pipeline boundary

The standalone `ConnectorHttpClient` accepts an arbitrary pre-built `HttpPipeline`. The caller that constructs that pipeline owns its retry semantics; `ConnectorHttpClient` does not inspect or rewrite it. Use normal generated-client construction through `ConnectorClientBase` to receive the SDK's safe default automatically.

## Service protections

Disabling automatic retries for unsafe methods reduces duplicate side effects but cannot eliminate them. Applications should use service-specific idempotency keys, conditional requests, operation identifiers, or deduplication when the connector supports them. These protections remain valuable even when the client makes only one attempt.
