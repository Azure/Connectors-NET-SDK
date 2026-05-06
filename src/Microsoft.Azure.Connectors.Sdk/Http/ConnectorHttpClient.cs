//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.Azure.Connectors.Sdk.Authentication;

namespace Microsoft.Azure.Connectors.Sdk.Http
{
    /// <summary>
    /// HTTP client for connector operations with Azure.Core <see cref="HttpPipeline"/>
    /// for retry, authentication, and diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is retained for standalone usage outside of <see cref="ConnectorClientBase"/>.
    /// Generated connector clients use the pipeline built by <see cref="ConnectorClientBase"/> directly.
    /// </para>
    /// </remarks>
    public class ConnectorHttpClient : IDisposable
    {
        /// <summary>
        /// The ActivitySource name used for OpenTelemetry instrumentation.
        /// Subscribe to this name to receive connector HTTP spans.
        /// </summary>
        public const string ActivitySourceName = "Microsoft.Azure.Connectors.Sdk";

        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

        private readonly HttpPipeline _pipeline;
        private readonly Uri? _baseUri;
        private readonly Func<string?>? _connectorNameProvider;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorHttpClient"/> class.
        /// </summary>
        /// <param name="tokenProvider">The token provider.</param>
        /// <param name="options">The client options (used to build the pipeline).</param>
        /// <param name="scopes">The authentication scopes for the pipeline.</param>
        /// <param name="connectorName">The connector name for telemetry.</param>
        public ConnectorHttpClient(
            ITokenProvider tokenProvider,
            ConnectorClientOptions options,
            string[] scopes,
            string? connectorName = null)
            : this(
                ConnectorHttpClient.BuildPipeline(tokenProvider, options, scopes),
                options.BaseUri,
                connectorName is not null ? () => connectorName : null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorHttpClient"/> class
        /// with a pre-built <see cref="HttpPipeline"/>.
        /// </summary>
        /// <param name="pipeline">The HTTP pipeline (handles retry, auth, diagnostics).</param>
        /// <param name="baseUri">Optional base URI for resolving relative request URIs.</param>
        /// <param name="connectorNameProvider">A function that returns the connector name for telemetry.</param>
        public ConnectorHttpClient(
            HttpPipeline pipeline,
            Uri? baseUri = null,
            Func<string?>? connectorNameProvider = null)
        {
            ArgumentNullException.ThrowIfNull(pipeline);

            this._pipeline = pipeline;
            this._baseUri = baseUri;
            this._connectorNameProvider = connectorNameProvider;
        }

        /// <summary>
        /// Sends an HTTP request through the Azure.Core pipeline with retry and authentication.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="scopes">The authentication scopes (used for telemetry; auth is configured in the pipeline).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            string[] scopes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            using var activity = ConnectorHttpClient.ActivitySource.StartActivity(
                $"HTTP {request.Method}",
                ActivityKind.Client);

            if (activity is not null)
            {
                activity.SetTag("http.method", request.Method.ToString());
                activity.SetTag("http.url", request.RequestUri?.ToString());

                var connectorName = this._connectorNameProvider?.Invoke();
                if (connectorName is not null)
                {
                    activity.SetTag("connector.name", connectorName);
                }

                if (request.Headers.TryGetValues("x-ms-client-request-id", out var requestIdValues))
                {
                    activity.SetTag("x-ms-client-request-id", string.Join(",", requestIdValues));
                }
            }

            try
            {
                // Convert HttpRequestMessage to Azure.Core pipeline request
                using var message = this._pipeline.CreateMessage();
                var pipelineRequest = message.Request;
                pipelineRequest.Method = RequestMethod.Parse(request.Method.Method);

                if (request.RequestUri is not null)
                {
                    if (!request.RequestUri.IsAbsoluteUri)
                    {
                        if (this._baseUri is null)
                        {
                            throw new InvalidOperationException(
                                "Cannot send a request with a relative URI because no BaseUri was configured. " +
                                "Set ConnectorClientOptions.BaseUri or use an absolute URI.");
                        }

                        pipelineRequest.Uri.Reset(new Uri(this._baseUri, request.RequestUri));
                    }
                    else
                    {
                        pipelineRequest.Uri.Reset(request.RequestUri);
                    }
                }

                // Copy request headers
                foreach (var header in request.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        pipelineRequest.Headers.Add(header.Key, value);
                    }
                }

                // Copy content
                if (request.Content is not null)
                {
                    var contentBytes = await request.Content
                        .ReadAsByteArrayAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                    pipelineRequest.Content = RequestContent.Create(contentBytes);

                    if (request.Content.Headers.ContentType is not null)
                    {
                        pipelineRequest.Headers.Add("Content-Type", request.Content.Headers.ContentType.ToString());
                    }
                }

                await this._pipeline
                    .SendAsync(message, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                var pipelineResponse = message.Response;

                // Convert Azure.Core Response to HttpResponseMessage
                var httpResponse = new HttpResponseMessage((HttpStatusCode)pipelineResponse.Status);

                if (pipelineResponse.ContentStream is not null)
                {
                    var contentStream = new MemoryStream();
                    await pipelineResponse.ContentStream
                        .CopyToAsync(contentStream, cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                    contentStream.Position = 0;
                    httpResponse.Content = new StreamContent(contentStream);
                }

                // Copy response headers
                foreach (var header in pipelineResponse.Headers)
                {
                    if (!httpResponse.Headers.TryAddWithoutValidation(header.Name, header.Value))
                    {
                        httpResponse.Content?.Headers.TryAddWithoutValidation(header.Name, header.Value);
                    }
                }

                if (activity is not null)
                {
                    activity.SetTag("http.status_code", pipelineResponse.Status);

                    if (pipelineResponse.IsError)
                    {
                        activity.SetTag("otel.status_code", "ERROR");
                        activity.SetTag("otel.status_description", $"HTTP {pipelineResponse.Status} {pipelineResponse.ReasonPhrase}");
                        activity.SetStatus(ActivityStatusCode.Error, $"HTTP {pipelineResponse.Status}");
                    }
                }

                return httpResponse;
            }
            catch (OperationCanceledException)
            {
                if (activity is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, "Canceled");
                }

                throw;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (activity is not null)
                {
                    activity.SetTag("otel.status_code", "ERROR");
                    activity.SetTag("otel.status_description", ex.Message);
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                this._disposed = true;
            }
        }

        private static HttpPipeline BuildPipeline(
            ITokenProvider tokenProvider,
            ConnectorClientOptions options,
            string[] scopes)
        {
            ArgumentNullException.ThrowIfNull(tokenProvider);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(scopes);

            var credential = new TokenProviderCredential(tokenProvider);
            return HttpPipelineBuilder.Build(
                options,
                perRetryPolicies: new HttpPipelinePolicy[]
                {
                    new BearerTokenAuthenticationPolicy(credential, scopes)
                });
        }
    }
}
