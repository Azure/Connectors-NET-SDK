//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.DocuSign;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class DocuSignClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static DocuSignClient CreateMockedClient(HttpResponseMessage response, Action<HttpRequestMessage>? requestCallback = null)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response)
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestCallback?.Invoke(request))
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new DocuSignClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new DocuSignClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DocuSignClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new DocuSignClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new DocuSignClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetDocgenFormFieldsAsync_WithMockedResponse_ReturnsExpected()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetDocgenFormFieldsAsync(accountId: "acct1",
                    envelopeId: "env1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetDocgenFormFieldsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetDocgenFormFieldsAsync(accountId: "acct1",
                    envelopeId: "env1",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task StaticResponseForEmbeddedSigningSchemaAsync_WithMockedResponse_ReturnsExpected()
        {
            Uri? requestUri = null;
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage, request => requestUri = request.RequestUri);

            var result = await client
                .StaticResponseForEmbeddedSigningSchemaAsync(returnURL: "https://contoso.example/return",
                    isThisAnPersonSigner: "true",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(requestUri);
            var capturedRequestUri = requestUri ?? throw new InvalidOperationException(message: "The mocked handler did not capture a request URI.");
            Assert.AreEqual(expected: "/connection/embeddedSigning_schema_v2", actual: capturedRequestUri.AbsolutePath);
            Assert.IsTrue(capturedRequestUri.Query.Contains("returnUrl=https%3A%2F%2Fcontoso.example%2Freturn", StringComparison.Ordinal));
            Assert.IsTrue(capturedRequestUri.Query.Contains("isInPersonSigner=true", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task StaticResponseForEmbeddedSigningSchemaAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.StaticResponseForEmbeddedSigningSchemaAsync(returnURL: "https://contoso.example/return",
                    isThisAnPersonSigner: "true",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

    }
}
