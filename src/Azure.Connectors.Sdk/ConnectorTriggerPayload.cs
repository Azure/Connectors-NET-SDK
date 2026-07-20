//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Serialization;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Helpers that turn a raw Connector Namespace trigger callback (an HTTP body
/// delivered as a <see cref="string"/> or <see cref="Stream"/>) into a typed
/// <see cref="TriggerCallbackPayload{T}"/> or into the decoded file bytes of a
/// binary-content trigger.
/// </summary>
/// <remarks>
/// <para>
/// Connector Namespace delivers two distinct trigger callback shapes for file connectors:
/// </para>
/// <list type="definition">
///   <item>
///     <term>Metadata (properties only), e.g. <c>OnNewFilesV2</c></term>
///     <description>
///     The body is an object envelope <c>{"body":{"value":[{...item...}]}}</c>.
///     Read it with <see cref="Read{TPayload}(string)"/> /
///     <see cref="ReadAsync{TPayload}(Stream, long, CancellationToken)"/>.
///     </description>
///   </item>
///   <item>
///     <term>Binary content, e.g. <c>OnNewFileV2</c></term>
///     <description>
///     The body is a base64-encoded string <c>{"body":"&lt;base64&gt;"}</c>.
///     Read it with <see cref="TryReadBinaryContent(string, out byte[])"/> /
///     <see cref="ReadBinaryContentAsync(Stream, long, CancellationToken)"/>.
///     </description>
///   </item>
/// </list>
/// <para>
/// All metadata reads use case-insensitive property matching, so payloads whose wire
/// fields are camelCase deserialize correctly instead of silently yielding all-<see langword="null"/>
/// items.
/// </para>
/// </remarks>
public static class ConnectorTriggerPayload
{
    /// <summary>
    /// The default maximum trigger callback body size, in bytes, enforced by the stream-based
    /// readers (100 MB). This is a generous ceiling that guards against unbounded buffering of a
    /// hostile or malformed stream while comfortably accommodating large binary-content callbacks.
    /// Override it per call with the <c>maxBodySizeBytes</c> parameter.
    /// </summary>
    public const long DefaultMaxBodySizeBytes = 100L * 1024 * 1024;

    /// <summary>
    /// The buffer size, in bytes, rented from the shared <see cref="ArrayPool{T}"/> for each
    /// stream read. 80 KB matches the default <see cref="Stream.CopyTo(Stream)"/> chunk size,
    /// balancing read throughput against per-call memory.
    /// </summary>
    private const int ReadChunkSizeBytes = 81920;

    private static readonly JsonSerializerOptions DefaultSerializerOptions = new(ConnectorJsonSerializer.Options);

    /// <summary>
    /// Gets a copy of the <see cref="JsonSerializerOptions"/> used to read trigger callback payloads.
    /// Property matching is case-insensitive so camelCase wire fields bind correctly.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions => new(ConnectorTriggerPayload.DefaultSerializerOptions);

    /// <summary>
    /// Reads a metadata trigger callback (for example OneDrive <c>OnNewFilesV2</c>) into its
    /// typed payload. The expected wire shape is <c>{"body":{"value":[{...item...}]}}</c>.
    /// </summary>
    /// <typeparam name="TPayload">
    /// The connector-specific payload type, a subclass of <see cref="TriggerCallbackPayload{T}"/>
    /// (for example <c>OneDriveForBusinessOnNewFilesTriggerPayload</c>).
    /// </typeparam>
    /// <param name="json">The raw JSON callback body.</param>
    /// <returns>The deserialized payload, or <see langword="null"/> when <paramref name="json"/> is JSON <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">
    /// The body was a base64 string (a binary-content trigger such as <c>OnNewFileV2</c>) rather than
    /// a metadata object; read it with <see cref="TryReadBinaryContent(string, out byte[])"/> instead.
    /// </exception>
    public static TPayload? Read<TPayload>(string json)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<TPayload>(json, ConnectorTriggerPayload.DefaultSerializerOptions);
    }

    /// <summary>
    /// Reads a metadata trigger callback (for example OneDrive <c>OnNewFilesV2</c>) from a stream into
    /// its typed payload. The expected wire shape is <c>{"body":{"value":[{...item...}]}}</c>.
    /// </summary>
    /// <typeparam name="TPayload">
    /// The connector-specific payload type, a subclass of <see cref="TriggerCallbackPayload{T}"/>
    /// (for example <c>OneDriveForBusinessOnNewFilesTriggerPayload</c>).
    /// </typeparam>
    /// <param name="body">The callback body stream (for example <c>HttpRequestData.Body</c>). The stream is read but not disposed; the caller retains ownership.</param>
    /// <param name="maxBodySizeBytes">
    /// The maximum number of bytes to read from <paramref name="body"/> before failing.
    /// Defaults to <see cref="DefaultMaxBodySizeBytes"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized payload, or <see langword="null"/> when the body is JSON <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxBodySizeBytes"/> is not greater than zero.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="body"/> exceeded <paramref name="maxBodySizeBytes"/>.</exception>
    /// <exception cref="JsonException">
    /// The body was a base64 string (a binary-content trigger such as <c>OnNewFileV2</c>) rather than
    /// a metadata object; read it with <see cref="ReadBinaryContentAsync(Stream, long, CancellationToken)"/> instead.
    /// </exception>
    public static async ValueTask<TPayload?> ReadAsync<TPayload>(
        Stream body,
        long maxBodySizeBytes = ConnectorTriggerPayload.DefaultMaxBodySizeBytes,
        CancellationToken cancellationToken = default)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(body);

        if (maxBodySizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBodySizeBytes),
                maxBodySizeBytes,
                "The maximum body size must be greater than zero.");
        }

        // Dispose the wrapper once deserialization completes. It owns no resources of its own
        // and its Dispose does not touch the caller-owned inner stream (which stays open per
        // contract); disposing it simply satisfies IDisposable-scope analyzers and is defensive.
        using var bounded = new BoundedStream(body, maxBodySizeBytes);
        return await JsonSerializer
            .DeserializeAsync<TPayload>(bounded, ConnectorTriggerPayload.DefaultSerializerOptions, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Reads a metadata trigger callback from the framework-neutral <paramref name="transport"/>,
    /// validates the resolved trigger configuration against <paramref name="expectedIdentity"/>, and
    /// deserializes the body into its typed payload.
    /// </summary>
    /// <typeparam name="TPayload">
    /// The connector-specific payload type, a subclass of <see cref="TriggerCallbackPayload{T}"/>
    /// (for example <c>Office365OnNewEmailTriggerPayload</c>).
    /// </typeparam>
    /// <param name="transport">
    /// The framework-neutral callback representation. Carry the body stream and the HTTP headers
    /// forwarded from the host (Azure Functions, ASP.NET Core, etc.) using a host-local adapter.
    /// </param>
    /// <param name="expectedIdentity">
    /// The expected connector and operation identity. When the resolved trigger configuration does not
    /// match, a <see cref="ConnectorTriggerIdentityMismatchException"/> is thrown before deserialization.
    /// </param>
    /// <param name="triggerConfigResolver">
    /// Resolves the authoritative Connector Namespace trigger configuration identified by the callback headers.
    /// </param>
    /// <param name="maxBodySizeBytes">
    /// The maximum number of bytes to read from the body before failing.
    /// Defaults to <see cref="DefaultMaxBodySizeBytes"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized payload, or <see langword="null"/> when the body is JSON <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="transport"/>, <paramref name="expectedIdentity"/>, or
    /// <paramref name="triggerConfigResolver"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxBodySizeBytes"/> is not greater than zero.</exception>
    /// <exception cref="ConnectorTriggerResourceIdentityException">
    /// The callback did not contain the required Connector Namespace resource-context headers.
    /// </exception>
    /// <exception cref="ConnectorTriggerConfigurationResolutionException">
    /// The SDK could not resolve the authoritative Connector Namespace trigger configuration.
    /// </exception>
    /// <exception cref="ConnectorTriggerIdentityMismatchException">
    /// The resolved Connector Namespace trigger configuration does not match <paramref name="expectedIdentity"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">The body exceeded <paramref name="maxBodySizeBytes"/>.</exception>
    /// <exception cref="JsonException">
    /// The body was a base64 string (a binary-content trigger) rather than a metadata object.
    /// </exception>
    /// <remarks>
    /// Validation uses the resource-context header names defined in <see cref="ConnectorTriggerHeaderNames"/>,
    /// resolves the authoritative trigger configuration through <paramref name="triggerConfigResolver"/>,
    /// and compares the resolved connector and operation to <paramref name="expectedIdentity"/>.
    /// Header-name lookup and value comparison are both <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </remarks>
    public static async ValueTask<TPayload?> ReadAsync<TPayload>(
        ConnectorTriggerTransport transport,
        ConnectorTriggerIdentity expectedIdentity,
        IConnectorNamespaceTriggerConfigResolver triggerConfigResolver,
        long maxBodySizeBytes = ConnectorTriggerPayload.DefaultMaxBodySizeBytes,
        CancellationToken cancellationToken = default)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(expectedIdentity);
        ArgumentNullException.ThrowIfNull(triggerConfigResolver);

        string? correlationId = ConnectorTriggerPayload.GetFirstHeaderValue(transport.Headers, ConnectorTriggerHeaderNames.CorrelationId);
        var resourceIdentity = ConnectorTriggerPayload.GetResourceIdentity(transport.Headers, correlationId);

        ConnectorNamespaceTriggerConfig resolvedTriggerConfig;
        try
        {
            resolvedTriggerConfig = await triggerConfigResolver
                .GetTriggerConfigAsync(resourceIdentity, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ConnectorTriggerConfigurationResolutionException)
        {
            throw;
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            throw ConnectorTriggerPayload.CreateConfigurationResolutionException(
                resourceIdentity: resourceIdentity,
                correlationId: correlationId,
                detail: "The trigger configuration resolver failed to return an authoritative Connector Namespace trigger configuration.",
                status: null,
                innerException: ex);
        }

        if (resolvedTriggerConfig is null)
        {
            throw ConnectorTriggerPayload.CreateConfigurationResolutionException(
                resourceIdentity: resourceIdentity,
                correlationId: correlationId,
                detail: "The trigger configuration resolver returned a null trigger configuration.",
                status: null);
        }

        ConnectorTriggerPayload.ValidateIdentity(
            expectedIdentity: expectedIdentity,
            resolvedTriggerConfig: resolvedTriggerConfig,
            resourceIdentity: resourceIdentity,
            correlationId: correlationId);

        return await ConnectorTriggerPayload
            .ReadAsync<TPayload>(transport.Body, maxBodySizeBytes, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Attempts to read a binary-content trigger callback (for example OneDrive <c>OnNewFileV2</c>),
    /// whose wire shape is <c>{"body":"&lt;base64&gt;"}</c>, into the decoded file bytes.
    /// </summary>
    /// <param name="json">The raw JSON callback body.</param>
    /// <param name="content">
    /// When this method returns <see langword="true"/>, the decoded file bytes (empty when the body
    /// string was empty). When it returns <see langword="false"/>, an empty array.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the callback carried a base64 string body and was decoded;
    /// <see langword="false"/> when <paramref name="json"/> was not valid JSON, the body was not a
    /// JSON string (for example a metadata callback), or the string was not valid base64.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryReadBinaryContent(string json, out byte[] content)
    {
        ArgumentNullException.ThrowIfNull(json);

        content = Array.Empty<byte>();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            // This is a Try* API: malformed JSON is a "could not read" outcome, not an exception.
            return false;
        }

        using (document)
        {
            return ConnectorTriggerPayload.TryDecodeBinaryBody(document, out content);
        }
    }

    /// <summary>
    /// Creates a configuration-resolution exception with safe diagnostics.
    /// </summary>
    private static ConnectorTriggerConfigurationResolutionException CreateConfigurationResolutionException(
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        string? correlationId,
        string detail,
        int? status,
        Exception? innerException = null)
    {
        var message = new StringBuilder(detail);
        message.Append(
            $" Subscription '{resourceIdentity.SubscriptionId}', resource group '{resourceIdentity.ResourceGroupName}', " +
            $"Connector Namespace '{resourceIdentity.ConnectorNamespaceName}', trigger config '{resourceIdentity.TriggerConfigName}'.");

        if (status.HasValue)
        {
            message.Append($" Status: '{status.Value}'.");
        }

        if (correlationId is not null)
        {
            message.Append($" Correlation ID: '{correlationId}'.");
        }

        return new ConnectorTriggerConfigurationResolutionException(
            message: message.ToString(),
            resourceIdentity: resourceIdentity,
            status: status,
            correlationId: correlationId,
            innerException: innerException);
    }

    /// <summary>
    /// Returns the first non-empty value for <paramref name="headerName"/> in <paramref name="headers"/>,
    /// or <see langword="null"/> when the header is absent or all its values are empty.
    /// </summary>
    private static string? GetFirstHeaderValue(
        IReadOnlyDictionary<string, IEnumerable<string>>? headers,
        string headerName)
    {
        if (headers is null)
        {
            return null;
        }

        if (headers.TryGetValue(headerName, out IEnumerable<string>? values))
        {
            return ConnectorTriggerPayload.GetFirstNonEmptyHeaderValue(values);
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
        {
            if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
            {
                return ConnectorTriggerPayload.GetFirstNonEmptyHeaderValue(header.Value);
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the Connector Namespace trigger-config resource identity resolved from callback headers.
    /// </summary>
    private static ConnectorNamespaceTriggerConfigResourceIdentity GetResourceIdentity(
        IReadOnlyDictionary<string, IEnumerable<string>>? headers,
        string? correlationId)
    {
        string? subscriptionId = ConnectorTriggerPayload.GetFirstHeaderValue(headers, ConnectorTriggerHeaderNames.SubscriptionId);
        string? resourceGroupName = ConnectorTriggerPayload.GetFirstHeaderValue(headers, ConnectorTriggerHeaderNames.ResourceGroupName);
        string? connectorNamespaceName = ConnectorTriggerPayload.GetFirstHeaderValue(headers, ConnectorTriggerHeaderNames.ConnectorNamespaceName);
        string? triggerConfigName = ConnectorTriggerPayload.GetFirstHeaderValue(headers, ConnectorTriggerHeaderNames.TriggerConfigName);

        var presentHeaders = new List<string>(capacity: 4);
        if (subscriptionId is not null)
        {
            presentHeaders.Add(ConnectorTriggerHeaderNames.SubscriptionId);
        }

        if (resourceGroupName is not null)
        {
            presentHeaders.Add(ConnectorTriggerHeaderNames.ResourceGroupName);
        }

        if (connectorNamespaceName is not null)
        {
            presentHeaders.Add(ConnectorTriggerHeaderNames.ConnectorNamespaceName);
        }

        if (triggerConfigName is not null)
        {
            presentHeaders.Add(ConnectorTriggerHeaderNames.TriggerConfigName);
        }

        if (subscriptionId is not null &&
            resourceGroupName is not null &&
            connectorNamespaceName is not null &&
            triggerConfigName is not null)
        {
            return new ConnectorNamespaceTriggerConfigResourceIdentity(
                SubscriptionId: subscriptionId,
                ResourceGroupName: resourceGroupName,
                ConnectorNamespaceName: connectorNamespaceName,
                TriggerConfigName: triggerConfigName);
        }

        var message = new StringBuilder("Trigger resource identity headers were missing or empty.");

        if (subscriptionId is null)
        {
            message.Append($" Required header '{ConnectorTriggerHeaderNames.SubscriptionId}' was absent or empty.");
        }

        if (resourceGroupName is null)
        {
            message.Append($" Required header '{ConnectorTriggerHeaderNames.ResourceGroupName}' was absent or empty.");
        }

        if (connectorNamespaceName is null)
        {
            message.Append($" Required header '{ConnectorTriggerHeaderNames.ConnectorNamespaceName}' was absent or empty.");
        }

        if (triggerConfigName is null)
        {
            message.Append($" Required header '{ConnectorTriggerHeaderNames.TriggerConfigName}' was absent or empty.");
        }

        if (correlationId is not null)
        {
            message.Append($" Correlation ID: '{correlationId}'.");
        }

        throw new ConnectorTriggerResourceIdentityException(
            message: message.ToString(),
            presentResourceIdentityHeaderNames: ConnectorTriggerPayload.GetReadOnlyHeaderNames(presentHeaders),
            correlationId: correlationId);
    }

    /// <summary>
    /// Returns the first non-empty, trimmed value in <paramref name="values"/>, when present.
    /// </summary>
    private static string? GetFirstNonEmptyHeaderValue(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Creates an immutable view over header names captured for diagnostics.
    /// </summary>
    private static IReadOnlyList<string> GetReadOnlyHeaderNames(List<string> headerNames)
    {
        return Array.AsReadOnly(headerNames.ToArray());
    }

    /// <summary>
    /// Validates the resolved trigger identity against the caller-selected expected identity.
    /// </summary>
    private static void ValidateIdentity(
        ConnectorTriggerIdentity expectedIdentity,
        ConnectorNamespaceTriggerConfig resolvedTriggerConfig,
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        string? correlationId)
    {
        bool connectorMatch = string.Equals(
            resolvedTriggerConfig.ConnectorName,
            expectedIdentity.ConnectorName,
            StringComparison.OrdinalIgnoreCase);
        bool operationMatch = string.Equals(
            resolvedTriggerConfig.OperationName,
            expectedIdentity.OperationName,
            StringComparison.OrdinalIgnoreCase);

        if (connectorMatch && operationMatch)
        {
            return;
        }

        var message = new StringBuilder("Trigger identity mismatch.");

        if (!connectorMatch)
        {
            message.Append(
                $" Expected connector '{expectedIdentity.ConnectorName}', resolved connector '{resolvedTriggerConfig.ConnectorName}'.");
        }

        if (!operationMatch)
        {
            message.Append(
                $" Expected operation '{expectedIdentity.OperationName}', resolved operation '{resolvedTriggerConfig.OperationName}'.");
        }

        message.Append(
            $" Subscription '{resourceIdentity.SubscriptionId}', resource group '{resourceIdentity.ResourceGroupName}', " +
            $"Connector Namespace '{resourceIdentity.ConnectorNamespaceName}', trigger config '{resourceIdentity.TriggerConfigName}'.");

        if (correlationId is not null)
        {
            message.Append($" Correlation ID: '{correlationId}'.");
        }

        throw new ConnectorTriggerIdentityMismatchException(
            message: message.ToString(),
            expectedConnectorName: expectedIdentity.ConnectorName,
            expectedOperationName: expectedIdentity.OperationName,
            resolvedConnectorName: resolvedTriggerConfig.ConnectorName,
            resolvedOperationName: resolvedTriggerConfig.OperationName,
            resourceIdentity: resourceIdentity,
            correlationId: correlationId);
    }

    /// <summary>
    /// Decodes the base64 <c>body</c> string of a parsed binary-content trigger callback into bytes.
    /// </summary>
    /// <param name="document">The parsed callback document.</param>
    /// <param name="content">
    /// When this method returns <see langword="true"/>, the decoded file bytes (empty when the body
    /// string was empty). When it returns <see langword="false"/>, an empty array.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the document carried a base64 string body and was decoded;
    /// <see langword="false"/> when the root was not an object, the body was not a JSON string, or
    /// the string was not valid base64.
    /// </returns>
    private static bool TryDecodeBinaryBody(JsonDocument document, out byte[] content)
    {
        content = Array.Empty<byte>();

        // TryGetProperty throws InvalidOperationException when the root is not an object
        // (for example a JSON null, array, or string). Guard the kind first so a non-object
        // body is a "could not read" outcome rather than an exception, honouring the Try* contract.
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty(TriggerCallbackPropertyNames.Body, out JsonElement bodyElement) ||
            bodyElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        // Fast path: decode base64 straight from the JSON value without allocating a UTF-16
        // string. System.Text.Json reads the bytes directly, so the entire base64 payload is
        // never materialized as a string — this matters for large binary-content trigger bodies.
        // Covers the common, unquoted wire shape {"body":"<base64>"}.
        if (bodyElement.TryGetBytesFromBase64(out byte[]? decoded))
        {
            content = decoded ?? Array.Empty<byte>();
            return true;
        }

        // Fallback: the base64 string may arrive wrapped in extra quotes from the Logic Apps
        // expression engine (for example "\"<base64>\""), which the fast path rejects. Strip the
        // quotes before decoding. An empty body decodes to empty content.
        string base64Content = (bodyElement.GetString() ?? string.Empty).Trim('"');

        if (base64Content.Length == 0)
        {
            return true;
        }

        // Decode directly into a single right-sized array. The try/catch keeps the Try* contract:
        // invalid base64 returns false rather than throwing.
        try
        {
            content = Convert.FromBase64String(base64Content);
            return true;
        }
        catch (FormatException)
        {
            content = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>
    /// Reads a binary-content trigger callback (for example OneDrive <c>OnNewFileV2</c>) from a stream,
    /// whose wire shape is <c>{"body":"&lt;base64&gt;"}</c>, into the decoded file bytes.
    /// </summary>
    /// <param name="body">The callback body stream (for example <c>HttpRequestData.Body</c>). The stream is read but not disposed; the caller retains ownership.</param>
    /// <param name="maxBodySizeBytes">
    /// The maximum number of bytes to read from <paramref name="body"/> before failing.
    /// Defaults to <see cref="DefaultMaxBodySizeBytes"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The decoded file bytes, or <see langword="null"/> when the body was not a JSON string body
    /// (for example a metadata callback) or was not valid base64.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxBodySizeBytes"/> is not greater than zero, or exceeds <see cref="Array.MaxLength"/>.
    /// Because the decoded bytes are buffered into a single array, the limit cannot exceed the maximum array length.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="body"/> exceeded <paramref name="maxBodySizeBytes"/>.</exception>
    public static async ValueTask<byte[]?> ReadBinaryContentAsync(
        Stream body,
        long maxBodySizeBytes = ConnectorTriggerPayload.DefaultMaxBodySizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        byte[] utf8Json = await ConnectorTriggerPayload
            .ReadBoundedAsync(body, maxBodySizeBytes, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        // Parse straight from the UTF-8 bytes (JSON's native encoding) rather than first
        // decoding to a UTF-16 string, avoiding a large intermediate allocation for big bodies.
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(utf8Json);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            return ConnectorTriggerPayload.TryDecodeBinaryBody(document, out byte[] content)
                ? content
                : null;
        }
    }

    /// <summary>
    /// Reads the caller-owned <paramref name="body"/> stream into a byte array, enforcing
    /// <paramref name="maxBodySizeBytes"/>. The stream is read but never disposed.
    /// </summary>
    /// <param name="body">The stream to read.</param>
    /// <param name="maxBodySizeBytes">The maximum number of bytes to read before failing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bytes read from the stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxBodySizeBytes"/> is not greater than zero, or exceeds <see cref="Array.MaxLength"/>.
    /// The bytes are buffered into a single array, so the limit cannot exceed the maximum array length.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="body"/> exceeded <paramref name="maxBodySizeBytes"/>.</exception>
    private static async ValueTask<byte[]> ReadBoundedAsync(
        Stream body,
        long maxBodySizeBytes,
        CancellationToken cancellationToken)
    {
        if (maxBodySizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBodySizeBytes),
                maxBodySizeBytes,
                "The maximum body size must be greater than zero.");
        }

        // This path materializes the body into a single byte[], so a limit above the maximum
        // array length can never be satisfied. Fail up front with a clear, predictable error
        // rather than letting a huge body eventually throw an opaque OutOfMemoryException.
        if (maxBodySizeBytes > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBodySizeBytes),
                maxBodySizeBytes,
                $"The maximum body size for buffered binary-content reads cannot exceed {Array.MaxLength} bytes (the maximum array length).");
        }

        using var buffer = new MemoryStream();
        byte[] chunk = ArrayPool<byte>.Shared.Rent(ConnectorTriggerPayload.ReadChunkSizeBytes);
        try
        {
            long totalBytesRead = 0;
            while (true)
            {
                // Never request more than one byte past the remaining allowance, so a single
                // ReadAsync cannot pull far beyond maxBodySizeBytes before the limit is checked.
                // The extra byte lets us detect an over-limit body without reading the whole stream.
                // Compare against the chunk length WITHOUT adding the probe first, so a very large
                // maxBodySizeBytes (e.g. long.MaxValue) cannot overflow the long arithmetic; only
                // add the +1 probe in the branch where the remaining allowance is the smaller bound.
                long remainingAllowance = maxBodySizeBytes - totalBytesRead;
                int requestSize = remainingAllowance >= chunk.Length
                    ? chunk.Length
                    : (int)Math.Min(chunk.Length, remainingAllowance + 1);

                int bytesRead = await body
                    .ReadAsync(chunk.AsMemory(0, requestSize), cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                if (bytesRead <= 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
                if (totalBytesRead > maxBodySizeBytes)
                {
                    throw new InvalidOperationException(
                        $"The trigger callback body exceeded the maximum allowed size of {maxBodySizeBytes} bytes.");
                }

                buffer.Write(chunk, 0, bytesRead);
            }
        }
        finally
        {
            // Clear the rented buffer on return: it can hold trigger callback content
            // (including base64 file bytes), and a subsequent renter in the same process
            // must not be able to observe residual data.
            ArrayPool<byte>.Shared.Return(chunk, clearArray: true);
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// A read-only wrapper that enforces a byte limit on the underlying stream without
    /// buffering the entire content. Each read is capped to at most the remaining
    /// allowance plus one byte (to detect over-limit bodies), and
    /// <see cref="InvalidOperationException"/> is thrown when the limit is exceeded.
    /// The inner stream is never closed or disposed; the caller retains ownership.
    /// </summary>
    private sealed class BoundedStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private long _totalBytesRead;

        public BoundedStream(Stream inner, long maxBytes)
        {
            this._inner = inner;
            this._maxBytes = maxBytes;
        }

        public override bool CanRead => this._inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set
            {
                _ = value;
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int cappedCount = this.CapRequestSize(count);
            int bytesRead = this._inner.Read(buffer, offset, cappedCount);
            this.EnforceBound(bytesRead);
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            int cappedLength = this.CapRequestSize(buffer.Length);
            int bytesRead = await this._inner
                .ReadAsync(buffer.Slice(0, cappedLength), cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            this.EnforceBound(bytesRead);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int cappedCount = this.CapRequestSize(count);
            int bytesRead = await this._inner
                .ReadAsync(buffer, offset, cappedCount, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            this.EnforceBound(bytesRead);
            return bytesRead;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // Intentionally do not dispose the inner stream — the caller owns it.
            // Still let the base Stream implementation mark this wrapper as disposed.
            base.Dispose(disposing);
        }

        /// <summary>
        /// Caps the requested read size so a single read never pulls far beyond the
        /// remaining allowance. Mirrors the logic in <see cref="ReadBoundedAsync"/>.
        /// </summary>
        private int CapRequestSize(int requestedCount)
        {
            long remainingAllowance = this._maxBytes - this._totalBytesRead;
            return remainingAllowance >= requestedCount
                ? requestedCount
                : (int)Math.Min(requestedCount, remainingAllowance + 1);
        }

        private void EnforceBound(int bytesRead)
        {
            this._totalBytesRead += bytesRead;
            if (this._totalBytesRead > this._maxBytes)
            {
                throw new InvalidOperationException(
                    $"The trigger callback body exceeded the maximum allowed size of {this._maxBytes} bytes.");
            }
        }
    }
}
