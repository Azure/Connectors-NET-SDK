//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk
{
    using System.Net;
    using System.Net.Http.Headers;

    /// <summary>
    /// Represents a response from a connector operation.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    public class ConnectorResponse<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorResponse{T}"/> class.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="headers">The response headers.</param>
        /// <param name="value">The response value.</param>
        public ConnectorResponse(HttpStatusCode statusCode, HttpResponseHeaders headers, T? value)
        {
            this.StatusCode = statusCode;
            this.Headers = headers;
            this.Value = value;
        }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the response headers.
        /// </summary>
        public HttpResponseHeaders Headers { get; }

        /// <summary>
        /// Gets the response value.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Gets a value indicating whether the response indicates success.
        /// </summary>
        public bool IsSuccessStatusCode => (int)this.StatusCode >= 200 && (int)this.StatusCode < 300;
    }
}
