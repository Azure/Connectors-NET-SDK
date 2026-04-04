//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net.Http;
using Microsoft.Azure.Connectors.Sdk.Authentication;
using Microsoft.Azure.Connectors.Sdk.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Abstract base class for generated connector clients.
    /// </summary>
    public abstract class ConnectorClientBase : IConnectorClient
    {
        private readonly ConnectorHttpClient _httpClient;
        private readonly ILogger _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class.
        /// </summary>
        /// <param name="tokenProvider">The token provider for authentication.</param>
        /// <param name="options">The connector client options.</param>
        /// <param name="logger">The logger instance.</param>
        protected ConnectorClientBase(
            ITokenProvider tokenProvider,
            ConnectorClientOptions? options = null,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(tokenProvider);

            options ??= new ConnectorClientOptions();
            this._logger = logger ?? NullLogger.Instance;
            this._httpClient = new ConnectorHttpClient(tokenProvider, options, this._logger);
        }

        /// <inheritdoc />
        public abstract string ConnectorName { get; }

        /// <summary>
        /// Gets the HTTP client for making connector requests.
        /// </summary>
        protected ConnectorHttpClient HttpClient => this._httpClient;

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected ILogger Logger => this._logger;

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the connector client resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this._httpClient?.Dispose();
                }

                this._disposed = true;
            }
        }
    }
}
