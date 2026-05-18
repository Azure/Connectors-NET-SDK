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
    }
}
