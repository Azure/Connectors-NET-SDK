//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

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
        /// <param name="responseBody">The response body from the connector service.</param>
        public ConnectorException(string connectorName, string operation, int statusCode, string responseBody)
            : base(statusCode, $"[{connectorName}] {operation} failed with status {statusCode}: {TruncateBody(responseBody)}")
        {
            this.ConnectorName = connectorName;
            this.Operation = operation;
            this.ResponseBody = responseBody;
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
        /// Gets the response body.
        /// </summary>
        public string ResponseBody { get; }

        private static string TruncateBody(string body)
        {
            if (string.IsNullOrEmpty(body) || body.Length <= MaxResponseBodyLength)
            {
                return body;
            }

            return body.Substring(0, MaxResponseBodyLength) + "...[truncated]";
        }
    }
}
