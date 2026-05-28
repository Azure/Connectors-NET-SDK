//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Text.Json;
using Azure;

namespace Azure.Connectors.Sdk
{
    /// <summary>
    /// Exception thrown when a connector API operation fails.
    /// Inherits from <see cref="RequestFailedException"/> so that consumers can catch
    /// all Azure SDK HTTP errors (including connector errors) with a single catch block.
    /// </summary>
    public class ConnectorException : RequestFailedException
    {
        private const int MaxResponseBodyLength = 2000;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorException"/> class.
        /// </summary>
        /// <param name="connectorName">The connector name (e.g., "office365").</param>
        /// <param name="operation">The operation that failed (e.g., "POST /Mail").</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="responseBody">The response body from the connector service, or <see langword="null"/> if unavailable.</param>
        public ConnectorException(string connectorName, string operation, int statusCode, string? responseBody)
            : base(
                  statusCode,
                  $"[{connectorName}] {operation} failed with status {statusCode}: {TruncateBody(responseBody)}",
                  ExtractErrorCode(responseBody),
                  innerException: null)
        {
            this.ConnectorName = connectorName;
            this.Operation = operation;
            this.ResponseBody = responseBody ?? string.Empty;
        }

        /// <summary>
        /// Gets the connector name.
        /// </summary>
        public string ConnectorName { get; }

        /// <summary>
        /// Gets the operation that failed.
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Gets the response body, or <see cref="string.Empty"/> if unavailable.
        /// </summary>
        public string ResponseBody { get; }

        /// <summary>
        /// Attempts to extract the <c>"code"</c> field from a JSON error response body
        /// to populate <see cref="RequestFailedException.ErrorCode"/>.
        /// Returns <see langword="null"/> if the body is not valid JSON or has no <c>code</c> property.
        /// </summary>
        private static string? ExtractErrorCode(string? responseBody)
        {
            if (string.IsNullOrEmpty(responseBody))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);

                // Try top-level "code"
                if (doc.RootElement.TryGetProperty("code", out var code) &&
                    code.ValueKind == JsonValueKind.String)
                {
                    return code.GetString();
                }

                // Try nested "error.code" (common in Azure error responses)
                if (doc.RootElement.TryGetProperty("error", out var errorObj) &&
                    errorObj.ValueKind == JsonValueKind.Object &&
                    errorObj.TryGetProperty("code", out var nestedCode) &&
                    nestedCode.ValueKind == JsonValueKind.String)
                {
                    return nestedCode.GetString();
                }
            }
            catch (JsonException)
            {
                // Not valid JSON — return null
            }

            return null;
        }

        private static string? TruncateBody(string? body)
        {
            if (string.IsNullOrEmpty(body) || body.Length <= MaxResponseBodyLength)
            {
                return body;
            }

            return body.Substring(0, MaxResponseBodyLength) + "...[truncated]";
        }
    }
}
