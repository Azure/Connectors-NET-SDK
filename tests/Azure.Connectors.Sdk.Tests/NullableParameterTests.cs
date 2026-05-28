//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Revai;
using Azure.Connectors.Sdk.Slack;
using Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for nullable optional value-type parameters (#174).
    /// Verifies that nullable int? and bool? parameters correctly emit or omit query string values.
    /// </summary>
    [TestClass]
    public class NullableParameterTests
    {
        private static (TClient Client, Mock<HttpMessageHandler> Handler) CreateMockedClientWithCapture<TClient>(string responseContent = "[]")
            where TClient : ConnectorClientBase
        {
            var mockCredential = new Mock<Azure.Core.TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<Azure.Core.TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Azure.Core.AccessToken("mock-token", new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent),
                }));

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            var client = (TClient)Activator.CreateInstance(
                typeof(TClient),
                new Uri("https://test.azure.com/connection"),
                mockCredential.Object,
                options)!;

            return (client, mockHandler);
        }

        [TestMethod]
        public async Task NullableInt_WithNull_OmitsQueryParameter()
        {
            // Arrange
            var (client, handler) = CreateMockedClientWithCapture<RevaiClient>();
            using (client)
            {
                // Act — pass null for limit (int?)
                await client
                    .TranscriptionsGetAsync(limit: null, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL does not contain "limit="
                handler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(request =>
                        !request.RequestUri!.Query.Contains("limit=", StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [TestMethod]
        public async Task NullableInt_WithZero_EmitsQueryParameterWithZero()
        {
            // Arrange
            var (client, handler) = CreateMockedClientWithCapture<RevaiClient>();
            using (client)
            {
                // Act — pass 0 for limit (int?) — this is a valid distinct value, not "unspecified"
                await client
                    .TranscriptionsGetAsync(limit: 0, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "limit=0"
                handler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.RequestUri!.Query.Contains("limit=0", StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [TestMethod]
        public async Task NullableInt_WithPositiveValue_EmitsQueryParameter()
        {
            // Arrange
            var (client, handler) = CreateMockedClientWithCapture<RevaiClient>();
            using (client)
            {
                // Act
                await client
                    .TranscriptionsGetAsync(limit: 25, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "limit=25"
                handler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.RequestUri!.Query.Contains("limit=25", StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [TestMethod]
        public async Task NullableBool_WithNull_OmitsQueryParameter()
        {
            // Arrange
            var (client, handler) = CreateMockedClientWithCapture<SlackClient>(responseContent: "{}");
            using (client)
            {
                // Act — pass null for isPrivateChannel (bool?)
                await client
                    .CreateChannelAsync(name: "test-channel", isPrivateChannel: null, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL does not contain "is_private="
                handler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(request =>
                        !request.RequestUri!.Query.Contains("is_private=", StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [TestMethod]
        public async Task NullableBool_WithFalse_EmitsQueryParameterWithFalse()
        {
            // Arrange
            var (client, handler) = CreateMockedClientWithCapture<SlackClient>(responseContent: "{}");
            using (client)
            {
                // Act — pass false for isPrivateChannel (bool?) — this is a valid distinct value, not "unspecified"
                await client
                    .CreateChannelAsync(name: "test-channel", isPrivateChannel: false, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "is_private=False"
                handler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.RequestUri!.Query.Contains("is_private=False", StringComparison.Ordinal)),
                    ItExpr.IsAny<CancellationToken>());
            }
        }

        [TestMethod]
        public async Task NullableBool_WithTrue_EmitsQueryParameterWithTrue()
        {
            // Arrange
            var (client, handler) = CreateMockedClientWithCapture<SlackClient>(responseContent: "{}");
            using (client)
            {
                // Act
                await client
                    .CreateChannelAsync(name: "test-channel", isPrivateChannel: true, cancellationToken: CancellationToken.None)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // Assert — verify the request URL contains "is_private=True"
                handler.Protected().Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(request =>
                        request.RequestUri!.Query.Contains("is_private=True", StringComparison.Ordinal)),
                    ItExpr.IsAny<CancellationToken>());
            }
        }
    }
}
