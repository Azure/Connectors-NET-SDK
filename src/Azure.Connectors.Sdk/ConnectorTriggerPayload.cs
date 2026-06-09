//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.IO;
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
/// <list type="bullet">
///   <item>
///     <term>Metadata (properties only), e.g. <c>OnNewFilesV2</c></term>
///     <description>
///     The body is an object envelope <c>{"body":{"value":[{...item...}]}}</c>.
///     Read it with <see cref="Read{TPayload}(string)"/> /
///     <see cref="ReadAsync{TPayload}(Stream, CancellationToken)"/>.
///     </description>
///   </item>
///   <item>
///     <term>Binary content, e.g. <c>OnNewFileV2</c></term>
///     <description>
///     The body is a base64-encoded string <c>{"body":"&lt;base64&gt;"}</c>.
///     Read it with <see cref="TryReadBinaryContent(string, out byte[])"/> /
///     <see cref="ReadBinaryContentAsync(Stream, CancellationToken)"/>.
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
    /// Gets the <see cref="JsonSerializerOptions"/> used to read trigger callback payloads.
    /// Property matching is case-insensitive so camelCase wire fields bind correctly.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions => ConnectorJsonSerializer.Options;

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

        return JsonSerializer.Deserialize<TPayload>(json, ConnectorTriggerPayload.SerializerOptions);
    }

    /// <summary>
    /// Reads a metadata trigger callback (for example OneDrive <c>OnNewFilesV2</c>) from a stream into
    /// its typed payload. The expected wire shape is <c>{"body":{"value":[{...item...}]}}</c>.
    /// </summary>
    /// <typeparam name="TPayload">
    /// The connector-specific payload type, a subclass of <see cref="TriggerCallbackPayload{T}"/>
    /// (for example <c>OneDriveForBusinessOnNewFilesTriggerPayload</c>).
    /// </typeparam>
    /// <param name="body">The callback body stream (for example <c>HttpRequestData.Body</c>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized payload, or <see langword="null"/> when the body is JSON <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">
    /// The body was a base64 string (a binary-content trigger such as <c>OnNewFileV2</c>) rather than
    /// a metadata object; read it with <see cref="ReadBinaryContentAsync(Stream, CancellationToken)"/> instead.
    /// </exception>
    public static async ValueTask<TPayload?> ReadAsync<TPayload>(
        Stream body,
        CancellationToken cancellationToken = default)
        where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(body);

        return await ConnectorJsonSerializer
            .DeserializeAsync<TPayload>(body, cancellationToken)
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
    /// <see langword="false"/> when the body was not a JSON string (for example a metadata callback)
    /// or was not valid base64.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryReadBinaryContent(string json, out byte[] content)
    {
        ArgumentNullException.ThrowIfNull(json);

        content = Array.Empty<byte>();

        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("body", out JsonElement bodyElement) ||
            bodyElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        // The base64 string may arrive wrapped in extra quotes from the Logic Apps
        // expression engine; strip them before decoding.
        string base64Content = (bodyElement.GetString() ?? string.Empty).Trim('"');

        if (base64Content.Length == 0)
        {
            return true;
        }

        var buffer = new byte[((base64Content.Length + 3) / 4) * 3];
        if (!Convert.TryFromBase64String(base64Content, buffer, out int decodedByteCount))
        {
            return false;
        }

        content = buffer.AsSpan(0, decodedByteCount).ToArray();
        return true;
    }

    /// <summary>
    /// Reads a binary-content trigger callback (for example OneDrive <c>OnNewFileV2</c>) from a stream,
    /// whose wire shape is <c>{"body":"&lt;base64&gt;"}</c>, into the decoded file bytes.
    /// </summary>
    /// <param name="body">The callback body stream (for example <c>HttpRequestData.Body</c>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The decoded file bytes, or <see langword="null"/> when the body was not a JSON string body
    /// (for example a metadata callback) or was not valid base64.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    public static async ValueTask<byte[]?> ReadBinaryContentAsync(
        Stream body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        using var reader = new StreamReader(body);
        string json = await reader
            .ReadToEndAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        return ConnectorTriggerPayload.TryReadBinaryContent(json, out byte[] content)
            ? content
            : null;
    }
}
