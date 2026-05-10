//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Azure;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using global::Azure.Identity;

namespace Azure.Connectors.Sdk
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
        private readonly string _connectionRuntimeUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class.
        /// This constructor exists for mocking frameworks (e.g. Moq) and should not be used directly.
        /// </summary>
        protected ConnectorClientBase()
        {
            this._pipeline = null!;
            this._connectionRuntimeUrl = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a connection runtime URL. Uses <see cref="ManagedIdentityCredential"/> by default.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        protected ConnectorClientBase(Uri connectionRuntimeUrl)
            : this(connectionRuntimeUrl, credential: new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a connection runtime URL and explicit Azure credential.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        /// <param name="credential">The Azure credential for authentication.</param>
        protected ConnectorClientBase(Uri connectionRuntimeUrl, TokenCredential credential)
            : this(connectionRuntimeUrl, credential, options: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a connection runtime URL and explicit Azure credential.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        /// <param name="credential">The Azure credential for authentication.</param>
        /// <param name="options">Optional client options for retry, transport, diagnostics, etc.</param>
        protected ConnectorClientBase(
            Uri connectionRuntimeUrl,
            TokenCredential credential,
            ConnectorClientOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(connectionRuntimeUrl);
            ArgumentNullException.ThrowIfNull(credential);

            if (!connectionRuntimeUrl.IsAbsoluteUri)
            {
                throw new ArgumentException(
                    message: $"The connection runtime URL '{connectionRuntimeUrl}' is not a valid absolute URI.",
                    paramName: nameof(connectionRuntimeUrl));
            }

            this._connectionRuntimeUrl = connectionRuntimeUrl.AbsoluteUri.TrimEnd('/');

            options = ConnectorClientBase.ApplyBaseUri(options, this._connectionRuntimeUrl);

            this._pipeline = HttpPipelineBuilder.Build(
                options,
                perRetryPolicies: new HttpPipelinePolicy[]
                {
                    new BearerTokenAuthenticationPolicy(credential, ApiHubScopes)
                });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorClientBase"/> class
        /// with a string connection runtime URL. Uses <see cref="ManagedIdentityCredential"/> by default.
        /// </summary>
        /// <param name="connectionRuntimeUrl">The connection runtime URL from Azure Portal.</param>
        protected ConnectorClientBase(string connectionRuntimeUrl)
            : this(ConnectorClientBase.ParseConnectionRuntimeUrl(connectionRuntimeUrl))
        {
        }

        /// <inheritdoc />
        public abstract string ConnectorName { get; }

        /// <summary>
        /// Gets the HTTP pipeline for making connector requests.
        /// </summary>
        protected HttpPipeline Pipeline => this._pipeline;

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
        protected virtual async Task<TResponse> CallConnectorAsync<TResponse>(
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
        protected virtual async Task CallConnectorAsync(
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

        /// <summary>
        /// Creates an <see cref="AsyncPageable{T}"/> that automatically follows pagination links
        /// to yield all items across multiple pages.
        /// </summary>
        /// <typeparam name="TPage">The page response type implementing <see cref="IPageable{TItem}"/>.</typeparam>
        /// <typeparam name="TItem">The type of items in each page.</typeparam>
        /// <param name="firstPageFunc">Function that fetches the first page.</param>
        /// <param name="nextPageFunc">Function that fetches subsequent pages given a NextLink URL.</param>
        /// <param name="cancellationToken">Cancellation token for the page fetch operations.</param>
        private protected AsyncPageable<TItem> CreatePageable<TPage, TItem>(
            Func<CancellationToken, Task<TPage>> firstPageFunc,
            Func<string, CancellationToken, Task<TPage>> nextPageFunc,
            CancellationToken cancellationToken = default)
            where TPage : class, IPageable<TItem>
            where TItem : notnull
        {
            return new ConnectorAsyncPageable<TPage, TItem>(firstPageFunc, nextPageFunc, cancellationToken);
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

        private static Uri ParseConnectionRuntimeUrl(string connectionRuntimeUrl)
        {
            ArgumentNullException.ThrowIfNull(connectionRuntimeUrl);

            if (!Uri.TryCreate(connectionRuntimeUrl, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(
                    message: $"The connection runtime URL '{connectionRuntimeUrl}' is not a valid absolute URI.",
                    paramName: nameof(connectionRuntimeUrl));
            }

            return uri;
        }

        /// <summary>
        /// An <see cref="AsyncPageable{T}"/> implementation that fetches pages on demand
        /// using first-page and next-page delegate functions.
        /// </summary>
        private sealed class ConnectorAsyncPageable<TPage, TItem> : AsyncPageable<TItem>
            where TPage : class, IPageable<TItem>
            where TItem : notnull
        {
            private readonly Func<CancellationToken, Task<TPage>> _firstPageFunc;
            private readonly Func<string, CancellationToken, Task<TPage>> _nextPageFunc;
            private readonly CancellationToken _cancellationToken;

            internal ConnectorAsyncPageable(
                Func<CancellationToken, Task<TPage>> firstPageFunc,
                Func<string, CancellationToken, Task<TPage>> nextPageFunc,
                CancellationToken cancellationToken)
            {
                this._firstPageFunc = firstPageFunc ?? throw new ArgumentNullException(nameof(firstPageFunc));
                this._nextPageFunc = nextPageFunc ?? throw new ArgumentNullException(nameof(nextPageFunc));
                this._cancellationToken = cancellationToken;
            }

            public override IAsyncEnumerable<Page<TItem>> AsPages(
                string? continuationToken = null,
                int? pageSizeHint = null)
            {
                return this.EnumeratePages(continuationToken);
            }

            private async IAsyncEnumerable<Page<TItem>> EnumeratePages(
                string? continuationToken = null,
                [EnumeratorCancellation] CancellationToken enumeratorCancellation = default)
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    this._cancellationToken, enumeratorCancellation);
                var cancellationToken = linkedCts.Token;

                // NOTE(daviburg): When a continuationToken is provided, resume from that page
                // instead of starting over. This supports callers who persist a ContinuationToken
                // from a previous AsPages() enumeration and resume later.
                var page = string.IsNullOrEmpty(continuationToken)
                    ? await this._firstPageFunc(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false)
                    : await this._nextPageFunc(continuationToken!, cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);

                while (page != null)
                {
                    var values = page.Value != null
                        ? (IReadOnlyList<TItem>)page.Value
                        : (IReadOnlyList<TItem>)Array.Empty<TItem>();

                    yield return new ConnectorPage<TItem>(values, page.NextLink);

                    if (string.IsNullOrEmpty(page.NextLink))
                    {
                        break;
                    }

                    page = await this._nextPageFunc(page.NextLink!, cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }

        /// <summary>
        /// A <see cref="Page{T}"/> implementation for connector pagination responses.
        /// </summary>
        private sealed class ConnectorPage<T> : Page<T>
            where T : notnull
        {
            private readonly IReadOnlyList<T> _values;
            private readonly string? _continuationToken;

            internal ConnectorPage(IReadOnlyList<T> values, string? continuationToken)
            {
                this._values = values;
                this._continuationToken = continuationToken;
            }

            public override IReadOnlyList<T> Values => this._values;

            public override string? ContinuationToken => this._continuationToken;

            public override Response GetRawResponse() => ConnectorClientBase.NoOpResponse.Instance;
        }

        /// <summary>
        /// A minimal <see cref="Response"/> implementation for connector pagination.
        /// Connector SDK requests go through <see cref="HttpPipeline"/> which owns the real
        /// response lifecycle — page objects don't retain the transport response, so this
        /// provides a non-null placeholder that satisfies the <see cref="Page{T}"/> contract.
        /// </summary>
        private sealed class NoOpResponse : Response
        {
            internal static readonly NoOpResponse Instance = new();

            public override int Status => 200;

            public override string ReasonPhrase => "OK";

            public override Stream? ContentStream { get; set; }

            public override string ClientRequestId { get; set; } = string.Empty;

            public override void Dispose() { }

            protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
            {
                value = null;
                return false;
            }

            protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
            {
                values = null;
                return false;
            }

            protected override bool ContainsHeader(string name) => false;

            protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
        }
    }
}
