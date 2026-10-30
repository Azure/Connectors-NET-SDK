//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
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
            return CreateMockedClient((request, cancellationToken) => Task.FromResult(response));
        }

        private static ClickSendSmsClient CreateMockedClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(sendAsync)
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
        public async Task CreateListAsync_WithMockedRequest_PreservesSwaggerContract()
        {
            using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"http_code":200,"response_code":"SUCCESS","response_msg":"List created","data":{"list_id":123,"list_name":"Mock list"}}"""),
            };
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: new Uri("https://test.azure.com/connection/lists"), actual: request.RequestUri);
                Assert.IsNotNull(request.Content);
                var json = await request.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                using var document = JsonDocument.Parse(json);
                CollectionAssert.AreEquivalent(
                    expected: new[] { "list_name" },
                    actual: document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
                Assert.AreEqual(expected: "Mock list", actual: document.RootElement.GetProperty("list_name").GetString());
                return responseMessage;
            });

            var result = await client
                .CreateListAsync(input: new CreateListInput { ListName = "Mock list" })
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 200, actual: result.HttpCode);
            Assert.AreEqual(expected: "SUCCESS", actual: result.ResponseCode);
            Assert.AreEqual(expected: "List created", actual: result.ResponseMsg);
            var data = result.Data ?? throw new AssertFailedException("Expected created list data.");
            Assert.AreEqual(expected: 123, actual: data.GetProperty("list_id").GetInt32());
            Assert.AreEqual(expected: "Mock list", actual: data.GetProperty("list_name").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task CreateListContactAsync_WithMockedRequest_PreservesSwaggerContract()
        {
            using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"http_code":200,"response_code":"SUCCESS","response_msg":"Contact created","data":{"contact_id":456,"list_id":123,"first_name":"Example","last_name":"Contact","phone_number":"+61411111111"}}"""),
            };
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: new Uri("https://test.azure.com/connection/lists/123/contacts"), actual: request.RequestUri);
                Assert.IsNotNull(request.Content);
                var json = await request.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                using var document = JsonDocument.Parse(json);
                var body = document.RootElement;
                CollectionAssert.AreEquivalent(
                    expected: new[] { "first_name", "last_name", "phone_number" },
                    actual: body.EnumerateObject().Select(property => property.Name).ToArray());
                Assert.AreEqual(expected: "Example", actual: body.GetProperty("first_name").GetString());
                Assert.AreEqual(expected: "Contact", actual: body.GetProperty("last_name").GetString());
                Assert.AreEqual(expected: "+61411111111", actual: body.GetProperty("phone_number").GetString());
                return responseMessage;
            });
            var input = new CreateListContactInput
            {
                FirstName = "Example",
                LastName = "Contact",
                PhoneNumber = "+61411111111",
            };

            var result = await client
                .CreateListContactAsync(listId: 123, input: input)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 200, actual: result.HttpCode);
            Assert.AreEqual(expected: "SUCCESS", actual: result.ResponseCode);
            Assert.AreEqual(expected: "Contact created", actual: result.ResponseMsg);
            var data = result.Data ?? throw new AssertFailedException("Expected created contact data.");
            Assert.AreEqual(expected: 456, actual: data.GetProperty("contact_id").GetInt32());
            Assert.AreEqual(expected: 123, actual: data.GetProperty("list_id").GetInt32());
            Assert.AreEqual(expected: "Example", actual: data.GetProperty("first_name").GetString());
            Assert.AreEqual(expected: "Contact", actual: data.GetProperty("last_name").GetString());
            Assert.AreEqual(expected: "+61411111111", actual: data.GetProperty("phone_number").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task ViewListContactsAsync_WithMockedResponse_PreservesSwaggerContract()
        {
            using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"http_code":200,"response_code":"SUCCESS","response_msg":"Contacts retrieved","data":{"total":1,"data":[{"contact_id":456,"list_id":123,"phone_number":"+61411111111"}]}}"""),
            };
            using var client = CreateMockedClient((request, cancellationToken) =>
            {
                Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
                Assert.AreEqual(expected: new Uri("https://test.azure.com/connection/lists/123/contacts"), actual: request.RequestUri);
                return Task.FromResult(responseMessage);
            });

            var result = await client
                .ViewListContactsAsync(listId: 123)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 200, actual: result.HttpCode);
            Assert.AreEqual(expected: "SUCCESS", actual: result.ResponseCode);
            Assert.AreEqual(expected: "Contacts retrieved", actual: result.ResponseMsg);
            var data = result.Data ?? throw new AssertFailedException("Expected nonempty contact list data.");
            Assert.AreEqual(expected: 1, actual: data.GetProperty("total").GetInt32());
            var contact = data.GetProperty("data").EnumerateArray().Single();
            Assert.AreEqual(expected: 456, actual: contact.GetProperty("contact_id").GetInt32());
            Assert.AreEqual(expected: 123, actual: contact.GetProperty("list_id").GetInt32());
            Assert.AreEqual(expected: "+61411111111", actual: contact.GetProperty("phone_number").GetString());
        }

        [TestMethod]
        [Owner("daviburg")]
        public async Task SmsCampaignSendAsync_WithMockedRequest_PreservesSwaggerContract()
        {
            var input = ClickSendSmsModelFactory.SmsCampaignSendInput(
                listId: 123,
                name: "Mock campaign",
                from: "Example",
                body: "Test message. Reply STOP to opt-out",
                schedule: 2147483648L,
                urlToShorten: "https://example.com/campaign",
                senders: new List<JsonElement?>
                {
                    JsonSerializer.SerializeToElement(new
                    {
                        country = "AU",
                        sender_id = "Example",
                        sender_type = "longcode",
                        sender_country_code = "AU",
                    }),
                },
                source: "MSPowerAutomate");
            var attempts = 0;
            using var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"http_code":200,"response_code":"SUCCESS","response_msg":"Campaign queued","data":{"campaign_id":456,"list_id":123,"name":"Mock campaign"}}"""),
            };
            using var client = CreateMockedClient(async (request, cancellationToken) =>
            {
                attempts++;
                Assert.AreEqual(expected: HttpMethod.Post, actual: request.Method);
                Assert.AreEqual(expected: new Uri("https://test.azure.com/connection/sms-campaigns/send"), actual: request.RequestUri);
                Assert.IsNotNull(request.Content);
                Assert.AreEqual(expected: "application/json", actual: request.Content.Headers.ContentType?.MediaType);
                var json = await request.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                using var document = JsonDocument.Parse(json);
                var body = document.RootElement;
                CollectionAssert.AreEquivalent(
                    expected: new[] { "list_id", "name", "from", "body", "schedule", "url_to_shorten", "senders", "source" },
                    actual: body.EnumerateObject().Select(property => property.Name).ToArray());
                Assert.AreEqual(expected: 123, actual: body.GetProperty("list_id").GetInt32());
                Assert.AreEqual(expected: "Mock campaign", actual: body.GetProperty("name").GetString());
                Assert.AreEqual(expected: "Example", actual: body.GetProperty("from").GetString());
                Assert.AreEqual(expected: "Test message. Reply STOP to opt-out", actual: body.GetProperty("body").GetString());
                Assert.AreEqual(expected: 2147483648L, actual: body.GetProperty("schedule").GetInt64());
                Assert.AreEqual(expected: "https://example.com/campaign", actual: body.GetProperty("url_to_shorten").GetString());
                Assert.AreEqual(expected: "MSPowerAutomate", actual: body.GetProperty("source").GetString());
                var senders = body.GetProperty("senders");
                Assert.AreEqual(expected: 1, actual: senders.GetArrayLength());
                var sender = senders[0];
                CollectionAssert.AreEquivalent(
                    expected: new[] { "country", "sender_id", "sender_type", "sender_country_code" },
                    actual: sender.EnumerateObject().Select(property => property.Name).ToArray());
                Assert.AreEqual(expected: "AU", actual: sender.GetProperty("country").GetString());
                Assert.AreEqual(expected: "Example", actual: sender.GetProperty("sender_id").GetString());
                Assert.AreEqual(expected: "longcode", actual: sender.GetProperty("sender_type").GetString());
                Assert.AreEqual(expected: "AU", actual: sender.GetProperty("sender_country_code").GetString());
                return responseMessage;
            });

            var result = await client
                .SmsCampaignSendAsync(input: input, cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: 1, actual: attempts);
            Assert.IsNotNull(result);
            Assert.AreEqual(expected: 200, actual: result.HttpCode);
            Assert.AreEqual(expected: "SUCCESS", actual: result.ResponseCode);
            Assert.AreEqual(expected: "Campaign queued", actual: result.ResponseMsg);
            var campaign = result.Data ?? throw new AssertFailedException("Expected nonempty campaign response data.");
            Assert.AreEqual(expected: 456, actual: campaign.GetProperty("campaign_id").GetInt32());
            Assert.AreEqual(expected: 123, actual: campaign.GetProperty("list_id").GetInt32());
            Assert.AreEqual(expected: "Mock campaign", actual: campaign.GetProperty("name").GetString());
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

            foreach (var document in new[] { json, JsonSerializer.Serialize(restored, options), JsonSerializer.Serialize(callbackItem, options) }
                .Select(serialized => JsonDocument.Parse(serialized)))
            {
                using var parsedDocument = document;
                Assert.AreEqual(expected: "underscored-message", actual: parsedDocument.RootElement.GetProperty("original_message_id").GetString());
                Assert.AreEqual(expected: "compact-message", actual: parsedDocument.RootElement.GetProperty("originalmessageid").GetString());
                Assert.AreEqual(expected: "underscored-custom", actual: parsedDocument.RootElement.GetProperty("custom_string").GetString());
                Assert.AreEqual(expected: "compact-custom", actual: parsedDocument.RootElement.GetProperty("customstring").GetString());
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
