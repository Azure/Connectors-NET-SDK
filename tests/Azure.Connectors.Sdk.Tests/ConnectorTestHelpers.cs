//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Shared test helpers for creating mocked connector clients consistently.
    /// </summary>
    internal static class ConnectorTestHelpers
    {
        /// <summary>
        /// Creates a fresh <see cref="TokenCredential"/> mock and <see cref="ConnectorClientOptions"/>
        /// with a mocked <see cref="HttpMessageHandler"/> that returns a new <see cref="HttpResponseMessage"/>
        /// on each request via <paramref name="responseFactory"/>.
        /// </summary>
        /// <param name="responseFactory">Factory invoked per HTTP request to produce a fresh response.</param>
        /// <returns>A tuple of the mocked credential and configured client options.</returns>
        public static (TokenCredential Credential, ConnectorClientOptions Options) CreateMockedClientSetup(
            Func<HttpResponseMessage> responseFactory)
        {
            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => Task.FromResult(responseFactory()));

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return (mockCredential.Object, options);
        }

        /// <summary>
        /// Like <see cref="CreateMockedClientSetup"/>, but also captures the most recent outgoing
        /// <see cref="HttpRequestMessage"/> so tests can assert on the request URI (e.g., path encoding).
        /// </summary>
        /// <param name="responseFactory">Factory invoked per HTTP request to produce a fresh response.</param>
        /// <returns>
        /// A tuple of the mocked credential, configured client options, and an accessor that returns the
        /// last request the client issued (or <c>null</c> if no request has been made yet).
        /// </returns>
        public static (TokenCredential Credential, ConnectorClientOptions Options, Func<HttpRequestMessage?> GetLastRequest) CreateCapturingClientSetup(
            Func<HttpResponseMessage> responseFactory)
        {
            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            HttpRequestMessage? lastRequest = null;
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) => lastRequest = request)
                .Returns(() => Task.FromResult(responseFactory()));

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return (mockCredential.Object, options, () => lastRequest);
        }

        /// <summary>
        /// Creates a mocked client setup that snapshots the outgoing request body and content type.
        /// </summary>
        /// <param name="responseFactory">Factory invoked per HTTP request to produce a fresh response.</param>
        /// <returns>The credential, options, and captured request content.</returns>
        public static (TokenCredential Credential, ConnectorClientOptions Options, CapturedRequestContent Capture) CreateContentCapturingClientSetup(
            Func<HttpResponseMessage> responseFactory)
        {
            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            var capture = new CapturedRequestContent();
            var handler = new ContentCapturingHandler(responseFactory, capture);

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(handler));
            options.Retry.MaxRetries = 0;

            return (mockCredential.Object, options, capture);
        }

        /// <summary>
        /// Captures the outgoing request body and content type for assertions.
        /// </summary>
        public sealed class CapturedRequestContent
        {
            /// <summary>Gets the captured request body.</summary>
            public byte[]? Body { get; internal set; }

            /// <summary>Gets the captured request content type.</summary>
            public string? ContentType { get; internal set; }
        }

        private sealed class ContentCapturingHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _responseFactory;
            private readonly CapturedRequestContent _capture;

            public ContentCapturingHandler(
                Func<HttpResponseMessage> responseFactory,
                CapturedRequestContent capture)
            {
                this._responseFactory = responseFactory;
                this._capture = capture;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                this._capture.Body = request.Content is null
                    ? null
                    : await request.Content
                        .ReadAsByteArrayAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                this._capture.ContentType = request.Content?.Headers.ContentType?.MediaType;
                return this._responseFactory();
            }
        }
    }
}
