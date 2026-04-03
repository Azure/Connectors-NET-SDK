//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Http
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using Microsoft.Azure.Workflows.Connectors.Sdk.Authentication;
    using Microsoft.Extensions.Logging;
    using Polly;
    using Polly.Extensions.Http;

    /// <summary>
    /// HTTP client for connector operations with retry and authentication.
    /// </summary>
    public class ConnectorHttpClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenProvider _tokenProvider;
        private readonly ConnectorClientOptions _options;
        private readonly ILogger _logger;
        private readonly AsyncPolicy<HttpResponseMessage> _retryPolicy;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorHttpClient"/> class.
        /// </summary>
        /// <param name="tokenProvider">The token provider.</param>
        /// <param name="options">The client options.</param>
        /// <param name="logger">The logger.</param>
        public ConnectorHttpClient(
            ITokenProvider tokenProvider,
            ConnectorClientOptions options,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(tokenProvider);
            ArgumentNullException.ThrowIfNull(options);

            this._tokenProvider = tokenProvider;
            this._options = options;
            this._logger = logger;

            this._httpClient = new HttpClient
            {
                Timeout = options.Timeout
            };

            if (options.BaseUri != null)
            {
                this._httpClient.BaseAddress = options.BaseUri;
            }

            this._retryPolicy = this.CreateRetryPolicy();
        }

        /// <summary>
        /// Sends an HTTP request with authentication and retry.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="scopes">The authentication scopes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var token = await this._tokenProvider
                .GetAccessTokenAsync(scopes, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await this._retryPolicy
                .ExecuteAsync(() => this._httpClient.SendAsync(request, cancellationToken))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Sends a GET request.
        /// </summary>
        /// <typeparam name="T">The response type.</typeparam>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="scopes">The authentication scopes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public async Task<ConnectorResponse<T>> GetAsync<T>(
            string requestUri,
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var response = await this
                .SendAsync(request, scopes, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return await this
                .ParseResponseAsync<T>(response, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Sends a POST request with JSON body.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="body">The request body.</param>
        /// <param name="scopes">The authentication scopes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public async Task<ConnectorResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string requestUri,
            TRequest body,
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await this
                .SendAsync(request, scopes, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return await this
                .ParseResponseAsync<TResponse>(response, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the HTTP client resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
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

        private AsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            if (this._options.UseExponentialBackoff)
            {
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        retryCount: this._options.MaxRetryAttempts,
                        sleepDurationProvider: retryAttempt =>
                            TimeSpan.FromMilliseconds(
                                this._options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1)),
                        onRetry: (outcome, delay, retryAttempt, context) =>
                        {
                            this._logger.LogWarning(
                                "Retry attempt {RetryAttempt} after {Delay}ms due to {Reason}",
                                retryAttempt,
                                delay.TotalMilliseconds,
                                outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                        });
            }

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(this._options.MaxRetryAttempts);
        }

        private async Task<ConnectorResponse<T>> ParseResponseAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            T? value = default;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (!string.IsNullOrEmpty(content))
                {
                    value = JsonSerializer.Deserialize<T>(content);
                }
            }

            return new ConnectorResponse<T>(response.StatusCode, response.Headers, value);
        }
    }
}
