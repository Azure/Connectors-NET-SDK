//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Zeptomail;
using Azure.Connectors.Sdk.Zeptomail.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ZeptomailClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static ZeptomailClient CreateMockedClient(HttpResponseMessage response)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response).Callback(() => { }).Verifiable();
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;
            return new ZeptomailClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object, options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new ZeptomailClient("https://test.azure.com/conn");
            Assert.AreEqual("zeptomail", client.ConnectorName);
        }

        [TestMethod]
        public void SendTemplateMailInput_MixedMergeInfo_RoundTripsFixedAndAdditionalValues()
        {
            var mergeItem = JsonSerializer.SerializeToElement(new { key = "customer", value = "Ada", rank = 2 });
            var model = new SendTemplateMailInput
            {
                MergeInfo = new List<JsonElement?> { mergeItem }
            };

            var json = JsonSerializer.Serialize(model);
            var roundTripped = JsonSerializer.Deserialize<SendTemplateMailInput>(json);
            var item = roundTripped!.MergeInfo[0]!.Value;

            Assert.AreEqual("customer", item.GetProperty("key").GetString());
            Assert.AreEqual("Ada", item.GetProperty("value").GetString());
            Assert.AreEqual(2, item.GetProperty("rank").GetInt32());
        }

        [TestMethod]
        public async Task SendTemplateMailAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Bad request\"}")
            };
            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.SendTemplateMailAsync(new SendTemplateMailInput(), CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
