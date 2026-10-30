//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.ClickSendSms;
using Azure.Connectors.Sdk.ClickSendSms.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ClickSendSmsClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static ClickSendSmsClient CreateMockedClient(HttpResponseMessage response)
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

            return new ClickSendSmsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new ClickSendSmsClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ClickSendSmsClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new ClickSendSmsClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new ClickSendSmsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetContactListsAsync_WithMockedResponse_ReturnsExpected()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetContactListsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetContactListsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetContactListsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        [Owner("daviburg")]
        [DataRow(false)]
        [DataRow(true)]
        public void SmsInboundAutomationNotification_CollidingNames_PreservesEveryWireField(bool caseInsensitive)
        {
            var model = ClickSendSmsModelFactory.SmsInboundAutomationNotification(
                originalMessageId: "underscored-message",
                originalmessageid: "compact-message",
                customString: "underscored-custom",
                customstring: "compact-custom");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = caseInsensitive };
            var json = JsonSerializer.Serialize(model, options);
            var restored = JsonSerializer.Deserialize<SmsInboundAutomationNotification>(json, options);
            Assert.IsNotNull(restored);
            var callback = JsonSerializer.Deserialize<ClickSendSmsOnSmsInboundAutomationTriggerPayload>(
                "{\"body\":" + json + "}", options);
            var items = callback?.Body?.Value;
            Assert.IsNotNull(items);
            var callbackItem = items.Single();

            foreach (var serialized in new[] { json, JsonSerializer.Serialize(restored, options), JsonSerializer.Serialize(callbackItem, options) })
            {
                using var document = JsonDocument.Parse(serialized);
                Assert.AreEqual(expected: "underscored-message", actual: document.RootElement.GetProperty("original_message_id").GetString());
                Assert.AreEqual(expected: "compact-message", actual: document.RootElement.GetProperty("originalmessageid").GetString());
                Assert.AreEqual(expected: "underscored-custom", actual: document.RootElement.GetProperty("custom_string").GetString());
                Assert.AreEqual(expected: "compact-custom", actual: document.RootElement.GetProperty("customstring").GetString());
            }
        }

        [TestMethod]
        public void SmsSendResponse_Serialization_RoundTrips()
        {
            var model = new SmsSendResponse
            {
                ResponseCode = "OK",
                ResponseMsg = "Success"
            };

            var json = JsonSerializer.Serialize(model);
            var deserialized = JsonSerializer.Deserialize<SmsSendResponse>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(expected: "OK", actual: deserialized!.ResponseCode);
            Assert.AreEqual(expected: "Success", actual: deserialized!.ResponseMsg);
        }
    }
}
