//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Plivo;
using Azure.Connectors.Sdk.Plivo.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class PlivoClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static PlivoClient CreateMockedClient(HttpResponseMessage response)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response)
                .Callback(() => { })
                .Verifiable();

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new PlivoClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        /// <summary>
        /// Keeps the renamed SMS API bound to the exact Plivo route and JSON wire fields.
        /// </summary>
        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task SendSmsAsync_PreservesRouteWireBodyAndResponse()
        {
            var attempts = 0;
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    attempts++;
                    Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                    Assert.AreEqual(expected: "/connection/v1/Account/account%2Fid/Message/", actual: request.RequestUri!.AbsolutePath);
                    using var body = JsonDocument.Parse(await request.Content!
                        .ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false));
                    Assert.AreEqual(expected: "15550000000", actual: body.RootElement.GetProperty("src").GetString());
                    Assert.AreEqual(expected: "15551111111", actual: body.RootElement.GetProperty("dst").GetString());
                    Assert.AreEqual(expected: "Hello", actual: body.RootElement.GetProperty("text").GetString());
                    Assert.AreEqual(expected: 3, actual: body.RootElement.GetPropertyCount());
                    return new HttpResponseMessage(HttpStatusCode.Accepted)
                    {
                        Content = new StringContent("{\"api_id\":\"request-123\",\"message\":\"queued\",\"message_uuid\":[\"message-123\"]}")
                    };
                });
            var options = new ConnectorClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(handler.Object))
            };
            options.Retry.MaxRetries = 0;
            using var client = new PlivoClient(new Uri("https://test.azure.com/connection"), SharedMockCredential.Object, options);

            var sentMessage = await client
                .SendSmsAsync(
                    authId: "account/id",
                    input: new Sms { From = "15550000000", To = "15551111111", Message = "Hello" })
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: attempts);
            Assert.AreEqual(expected: "request-123", actual: sentMessage.Apiid);
            Assert.AreEqual(expected: "queued", actual: sentMessage.Response);
            CollectionAssert.AreEqual(expected: new[] { "message-123" }, actual: sentMessage.MessageUuid);
        }

        /// <summary>
        /// Preserves the connector error contract on the renamed send operation.
        /// </summary>
        [TestMethod]
        [Owner("swapnilnagar")]
        public async Task SendSmsAsync_ErrorResponse_ThrowsConnectorException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"error\":{\"message\":\"Not allowed\"}}")
            };
            using var client = CreateMockedClient(response);

            var error = await Assert
                .ThrowsExactlyAsync<ConnectorException>(() => client.SendSmsAsync(
                    authId: "account",
                    input: new Sms { From = "15550000000", To = "15551111111", Message = "Hello" }))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 403, actual: error.Status);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new PlivoClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PlivoClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new PlivoClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new PlivoClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task ListMessagesAsync_WithMockedResponse_ReturnsExpected()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .ListMessagesAsync(authId: "auth1", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task ListMessagesAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.ListMessagesAsync(authId: "auth1", cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
