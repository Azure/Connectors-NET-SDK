//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net.Http.Headers;
using System.Text;
using Azure.Connectors.Sdk.Serialization;

namespace Azure.Connectors.Sdk.Http;

/// <summary>
/// Extension methods for HTTP operations.
/// </summary>
internal static class HttpExtensions
{
    private const string JsonMediaType = "application/json";

    /// <summary>
    /// Creates JSON content from an object.
    /// </summary>
    /// <typeparam name="T">The type of the content.</typeparam>
    /// <param name="content">The content to serialize.</param>
    /// <returns>The HTTP content with JSON payload.</returns>
    public static HttpContent ToJsonContent<T>(this T content)
    {
        var json = ConnectorJsonSerializer.Serialize(content);

        return new StringContent(json, Encoding.UTF8, JsonMediaType);
    }

    /// <summary>
    /// Reads the response content as a typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="response">The HTTP response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized object.</returns>
    public static async Task<T?> ReadAsAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        return await ConnectorJsonSerializer
            .DeserializeAsync<T>(stream, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    /// <summary>
    /// Adds a correlation ID header to the request.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="correlationId">The correlation ID.</param>
    public static void AddCorrelationId(this HttpRequestMessage request, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.Add("x-ms-correlation-id", correlationId);
        }
    }

    /// <summary>
    /// Adds a client request ID header to the request.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="clientRequestId">The client request ID.</param>
    public static void AddClientRequestId(this HttpRequestMessage request, string? clientRequestId = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestId = clientRequestId ?? Guid.NewGuid().ToString();
        request.Headers.Add("x-ms-client-request-id", requestId);
    }
}
