//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Azure.Core;
using global::Azure.Identity;
using Microsoft.Azure.Connectors.Sdk.Authentication;
using Microsoft.Azure.Connectors.Sdk.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Abstract base class for generated connector clients.
    /// Provides shared infrastructure: authentication, HTTP, JSON serialization,
    /// URL resolution with SSRF protection, and configurable retry/timeout.
    /// </summary>
    public abstract class ConnectorClientBase : IConnectorClient
    {
        /// <summary>
        /// The default OAuth scopes for API Hub authentication.
        /// </summary>
        protected static readonly string[] ApiHubScopes = ["https://apihub.azure.com/.default"];

        /// <summary>
        /// The default JSON serializer options for connector operations.
        /// </summary>
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ConnectorHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _connectionRuntimeUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a connection runtime URL and optional Azure credential.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        /// <param name="credential">Optional Azure credential. Defaults to <see cref="DefaultAzureCredential"/>.</param>
        /// <param name="options">Optional client options for retry, timeout, etc.</param>
        /// <param name="httpClient">Optional externally managed HttpClient.</param>
        protected ConnectorClientBase(
            string connectionRuntimeUrl,
            TokenCredential? credential = null,
            ConnectorClientOptions? options = null,
            HttpClient? httpClient = null)
            : this(
                  new TokenCredentialTokenProvider(credential ?? new DefaultAzureCredential()),
                  ConnectorClientBase.ApplyBaseUri(options, connectionRuntimeUrl),
                  logger: null,
                  httpClient)
        {
            this._connectionRuntimeUrl = connectionRuntimeUrl?.TrimEnd('/')
                ?? throw new ArgumentNullException(nameof(connectionRuntimeUrl));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a connection runtime URL and managed identity.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        /// <param name="managedIdentityClientId">
        /// The client ID for user-assigned managed identity.
        /// Use null or empty string for system-assigned identity.
        /// </param>
        /// <param name="options">Optional client options for retry, timeout, etc.</param>
        /// <param name="httpClient">Optional externally managed HttpClient.</param>
        protected ConnectorClientBase(
            string connectionRuntimeUrl,
            string managedIdentityClientId,
            ConnectorClientOptions? options = null,
            HttpClient? httpClient = null)
            : this(
                  connectionRuntimeUrl,
                  ConnectorClientBase.CreateManagedIdentityCredential(managedIdentityClientId),
                  options,
                  httpClient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a custom token provider.
        /// </summary>
        /// <param name="tokenProvider">The token provider for authentication.</param>
        /// <param name="options">The connector client options.</param>
        /// <param name="logger">The logger instance.</param>
        protected ConnectorClientBase(
            ITokenProvider tokenProvider,
            ConnectorClientOptions? options = null,
            ILogger? logger = null)
            : this(tokenProvider, options, logger, httpClient: null)
        {
        }

        private ConnectorClientBase(
            ITokenProvider tokenProvider,
            ConnectorClientOptions? options,
            ILogger? logger,
            HttpClient? httpClient)
        {
            ArgumentNullException.ThrowIfNull(tokenProvider);

            options ??= new ConnectorClientOptions();
            this._logger = logger ?? NullLogger.Instance;
            this._connectionRuntimeUrl = options.BaseUri?.ToString().TrimEnd('/') ?? string.Empty;
            this._httpClient = new ConnectorHttpClient(
                tokenProvider,
                options,
                this._logger,
                httpClient,
                connectorNameProvider: () => this.ConnectorName);
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

        /// <summary>
        /// Sends a connector API request and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="method">The HTTP method.</param>
        /// <param name="path">The relative path or absolute URL.</param>
        /// <param name="body">Optional request body (will be JSON-serialized).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized response.</returns>
        protected async Task<TResponse> CallConnectorAsync<TResponse>(
            HttpMethod method,
            string path,
            object? body = null,
            CancellationToken cancellationToken = default)
        {
            var url = this.ResolveUrl(path);
            var operation = $"{method} {path}";

            using var request = new HttpRequestMessage(method, url);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, ConnectorClientBase.JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await this._httpClient
                .SendAsync(request, ConnectorClientBase.ApiHubScopes, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                throw new ConnectorException(this.ConnectorName, operation, (int)response.StatusCode, errorBody);
            }

            if (typeof(TResponse) == typeof(byte[]))
            {
                var bytes = await response.Content
                    .ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                return (TResponse)(object)bytes;
            }

            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (string.IsNullOrEmpty(responseBody))
            {
                return default!;
            }

            return JsonSerializer.Deserialize<TResponse>(responseBody, ConnectorClientBase.JsonOptions)!;
        }

        /// <summary>
        /// Sends a connector API request with no response body.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="path">The relative path or absolute URL.</param>
        /// <param name="body">Optional request body (will be JSON-serialized).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected async Task CallConnectorAsync(
            HttpMethod method,
            string path,
            object? body = null,
            CancellationToken cancellationToken = default)
        {
            var url = this.ResolveUrl(path);
            var operation = $"{method} {path}";

            using var request = new HttpRequestMessage(method, url);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, ConnectorClientBase.JsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await this._httpClient
                .SendAsync(request, ConnectorClientBase.ApiHubScopes, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                throw new ConnectorException(this.ConnectorName, operation, (int)response.StatusCode, responseBody);
            }
        }

        /// <summary>
        /// Resolves a relative path or validates an absolute URL against the connection runtime URL,
        /// preventing SSRF by ensuring absolute URLs match the connection host.
        /// </summary>
        /// <param name="path">The relative path or absolute URL to resolve.</param>
        /// <returns>The resolved absolute URL.</returns>
        protected string ResolveUrl(string path)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var baseUri = new Uri(this._connectionRuntimeUrl);
                var nextUri = new Uri(path);
                if (!string.Equals(baseUri.Scheme, nextUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(baseUri.Host, nextUri.Host, StringComparison.OrdinalIgnoreCase) ||
                    baseUri.Port != nextUri.Port)
                {
                    throw new InvalidOperationException(
                        $"NextLink URI '{nextUri.Scheme}://{nextUri.Host}:{nextUri.Port}' does not match connection URI '{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}'. " +
                        "Refusing to send credentials to an unexpected host.");
                }

                return path;
            }

            return $"{this._connectionRuntimeUrl}{path}";
        }

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

        private static ConnectorClientOptions ApplyBaseUri(ConnectorClientOptions? options, string connectionRuntimeUrl)
        {
            options ??= new ConnectorClientOptions();
            options.BaseUri = new Uri(connectionRuntimeUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(connectionRuntimeUrl)));
            return options;
        }

        private static TokenCredential CreateManagedIdentityCredential(string managedIdentityClientId)
        {
            if (string.IsNullOrEmpty(managedIdentityClientId))
            {
                return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
            }

            return new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));
        }
    }
}
