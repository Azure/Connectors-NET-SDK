//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using global::Azure.Identity;
using Microsoft.Azure.Connectors.Sdk.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Abstract base class for generated connector clients.
    /// Provides shared infrastructure: authentication via Azure.Core <see cref="HttpPipeline"/>,
    /// JSON serialization, URL resolution with SSRF protection, and configurable retry/diagnostics
    /// through <see cref="ConnectorClientOptions"/>.
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

        private readonly HttpPipeline _pipeline;
        private readonly ILogger _logger;
        private readonly string _connectionRuntimeUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a connection runtime URL and optional Azure credential.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        /// <param name="credential">Optional Azure credential. Defaults to <see cref="DefaultAzureCredential"/>.</param>
        /// <param name="options">Optional client options for retry, transport, diagnostics, etc.</param>
        protected ConnectorClientBase(
            string connectionRuntimeUrl,
            TokenCredential? credential = null,
            ConnectorClientOptions? options = null)
        {
            this._connectionRuntimeUrl = connectionRuntimeUrl?.TrimEnd('/')
                ?? throw new ArgumentNullException(nameof(connectionRuntimeUrl));

            options = ConnectorClientBase.ApplyBaseUri(options, connectionRuntimeUrl);
            credential ??= new DefaultAzureCredential();

            this._logger = NullLogger.Instance;
            this._pipeline = HttpPipelineBuilder.Build(
                options,
                perRetryPolicies: new HttpPipelinePolicy[]
                {
                    new BearerTokenAuthenticationPolicy(credential, ApiHubScopes)
                });
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
        /// <param name="options">Optional client options for retry, transport, diagnostics, etc.</param>
        protected ConnectorClientBase(
            string connectionRuntimeUrl,
            string? managedIdentityClientId,
            ConnectorClientOptions? options = null)
            : this(
                  connectionRuntimeUrl,
                  ConnectorClientBase.CreateManagedIdentityCredential(managedIdentityClientId),
                  options)
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
        {
            ArgumentNullException.ThrowIfNull(tokenProvider);

            options ??= new ConnectorClientOptions();
            this._logger = logger ?? NullLogger.Instance;
            this._connectionRuntimeUrl = options.BaseUri?.ToString().TrimEnd('/') ?? string.Empty;

            var credential = new TokenProviderCredential(tokenProvider);
            this._pipeline = HttpPipelineBuilder.Build(
                options,
                perRetryPolicies: new HttpPipelinePolicy[]
                {
                    new BearerTokenAuthenticationPolicy(credential, ApiHubScopes)
                });
        }

        /// <inheritdoc />
        public abstract string ConnectorName { get; }

        /// <summary>
        /// Gets the HTTP pipeline for making connector requests.
        /// </summary>
        protected HttpPipeline Pipeline => this._pipeline;

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected ILogger Logger => this._logger;

        /// <summary>
        /// Sends a connector API request and deserializes the JSON response.
        /// Uses the Azure.Core <see cref="HttpPipeline"/> for retry, authentication, and diagnostics.
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

            using var message = this._pipeline.CreateMessage();
            var request = message.Request;
            request.Method = RequestMethod.Parse(method.Method);
            request.Uri.Reset(new Uri(url));
            request.Headers.Add("Accept", "application/json");

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, ConnectorClientBase.JsonOptions);
                request.Content = RequestContent.Create(Encoding.UTF8.GetBytes(json));
                request.Headers.Add("Content-Type", "application/json; charset=utf-8");
            }

            await this._pipeline
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = message.Response;

            if (response.IsError)
            {
                var errorBody = response.Content.ToString();
                throw new ConnectorException(this.ConnectorName, operation, response.Status, errorBody);
            }

            if (typeof(TResponse) == typeof(byte[]))
            {
                return (TResponse)(object)response.Content.ToArray();
            }

            var responseBody = response.Content.ToString();

            if (string.IsNullOrEmpty(responseBody))
            {
                return default!;
            }

            return JsonSerializer.Deserialize<TResponse>(responseBody, ConnectorClientBase.JsonOptions)!;
        }

        /// <summary>
        /// Sends a connector API request with no response body.
        /// Uses the Azure.Core <see cref="HttpPipeline"/> for retry, authentication, and diagnostics.
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

            using var message = this._pipeline.CreateMessage();
            var request = message.Request;
            request.Method = RequestMethod.Parse(method.Method);
            request.Uri.Reset(new Uri(url));
            request.Headers.Add("Accept", "application/json");

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, ConnectorClientBase.JsonOptions);
                request.Content = RequestContent.Create(Encoding.UTF8.GetBytes(json));
                request.Headers.Add("Content-Type", "application/json; charset=utf-8");
            }

            await this._pipeline
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = message.Response;

            if (response.IsError)
            {
                var errorBody = response.Content.ToString();
                throw new ConnectorException(this.ConnectorName, operation, response.Status, errorBody);
            }
        }

        /// <summary>
        /// Resolves a relative path or validates an absolute URL against the connection runtime URL.
        /// When the NextLink host matches the connection URL, it's used as-is.
        /// When it doesn't match (codeless connectors like ARM return nextLink pointing to the backend
        /// host e.g. management.azure.com), the path+query is extracted and routed through the APIM proxy.
        /// This is safe because the request still goes through the connection runtime URL with API Hub auth.
        /// </summary>
        /// <param name="path">The relative path or absolute URL to resolve.</param>
        /// <returns>The resolved absolute URL.</returns>
        protected string ResolveUrl(string path)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                if (string.IsNullOrEmpty(this._connectionRuntimeUrl))
                {
                    throw new InvalidOperationException(
                        message: "Cannot validate absolute NextLink URL because no connection runtime URL was configured. " +
                        "Set ConnectorClientOptions.BaseUri or use a constructor that accepts connectionRuntimeUrl.");
                }

                var baseUri = new Uri(this._connectionRuntimeUrl);
                var nextUri = new Uri(path);
                if (string.Equals(baseUri.Host, nextUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(baseUri.Scheme, nextUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                        baseUri.Port == nextUri.Port)
                    {
                        return path;
                    }

                    // NOTE(daviburg): Same host but different scheme or port — reject to prevent
                    // sending credentials over an insecure channel (e.g., http instead of https).
                    throw new InvalidOperationException(
                        $"NextLink URI '{nextUri.Scheme}://{nextUri.Host}:{nextUri.Port}' has the same host as the connection " +
                        $"but uses a different scheme or port than '{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}'. " +
                        "Refusing to send credentials to a potentially insecure endpoint.");
                }

                // NOTE(daviburg): NextLink from a different host (e.g., codeless connector backend).
                // Extract path+query and route through the connection runtime URL.
                return $"{this._connectionRuntimeUrl}{nextUri.PathAndQuery}";
            }

            if (string.IsNullOrEmpty(this._connectionRuntimeUrl))
            {
                throw new InvalidOperationException(
                    message: "Cannot resolve relative path because no connection runtime URL was configured. " +
                    "Set ConnectorClientOptions.BaseUri or use a constructor that accepts connectionRuntimeUrl.");
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
                this._disposed = true;
            }
        }

        private static ConnectorClientOptions ApplyBaseUri(ConnectorClientOptions? options, string connectionRuntimeUrl)
        {
            ArgumentNullException.ThrowIfNull(connectionRuntimeUrl);

            var trimmed = connectionRuntimeUrl.TrimEnd('/');

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(
                    message: $"The connection runtime URL '{trimmed}' is not a valid absolute URI.",
                    paramName: nameof(connectionRuntimeUrl));
            }

            options ??= new ConnectorClientOptions();

            // NOTE(daviburg): Only set BaseUri when the caller did not provide one.
            // This avoids silently overwriting a user-specified BaseUri on a shared options instance.
            options.BaseUri ??= uri;

            return options;
        }

        private static TokenCredential CreateManagedIdentityCredential(string? managedIdentityClientId)
        {
            if (string.IsNullOrEmpty(managedIdentityClientId))
            {
                return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
            }

            return new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));
        }
    }
}
