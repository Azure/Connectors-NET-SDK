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
using Azure.Connectors.Sdk.EventHubs;
using Azure.Connectors.Sdk.EventHubs.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated EventHubsClient class.
    /// </summary>
    [TestClass]
    public class EventHubsClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        private static EventHubsClient CreateMockedClient(HttpResponseMessage response)
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

            return new EventHubsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new EventHubsClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new EventHubsClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new EventHubsClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new EventHubsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetEventHubsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new List<string> { "eventhub-1", "eventhub-2" };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetEventHubsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("eventhub-1", result[0]);
            Assert.AreEqual("eventhub-2", result[1]);
        }

        [TestMethod]
        public async Task GetEventHubsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"error\": \"Forbidden\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetEventHubsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task GetConsumerGroupsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new List<string> { "$Default", "mygroup" };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetConsumerGroupsAsync(
                    theEventHubName: "eventhub-1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("$Default", result[0]);
        }

        [TestMethod]
        public void Event_HasAdditionalPropertiesDictionary()
        {
            const string json = "{\"contentData\":{},\"dynamicField\":\"dynamicValue\"}";
            var eventObj = JsonSerializer.Deserialize<Event>(json);
            Assert.IsNotNull(eventObj);
            Assert.IsNotNull(eventObj.AdditionalProperties);
            Assert.IsTrue(eventObj.AdditionalProperties.ContainsKey("dynamicField"));
            Assert.AreEqual("dynamicValue", ((JsonElement)eventObj.AdditionalProperties["dynamicField"]).GetString());
        }

        [TestMethod]
        public async Task SendEventAsync_WithProperties_SerializesPropertiesMap()
        {
            var captured = new CapturedRequest();
            using var client = CreateCapturingClient(captured);

            await client
                .SendEventAsync(
                    eventHubName: "eventhub-1",
                    input: CreateSendEvent(content: "aGVsbG8=", region: "westus", priority: 2),
                    partitionKey: "pk-1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(HttpMethod.Post, captured.Method);
            StringAssert.EndsWith(captured.Uri!.ToString(), "/eventhub-1/events?partitionKey=pk-1");
            using var body = JsonDocument.Parse(captured.Body!);
            AssertSendEvent(body.RootElement, content: "aGVsbG8=", region: "westus", priority: 2);
        }

        [TestMethod]
        public async Task SendEventsAsync_WithProperties_SerializesEachEventPropertiesMap()
        {
            var captured = new CapturedRequest();
            using var client = CreateCapturingClient(captured);

            await client
                .SendEventsAsync(
                    eventHubName: "eventhub-1",
                    input: new List<SendEvent>
                    {
                        CreateSendEvent(content: "Zmlyc3Q=", region: "westus", priority: 1),
                        CreateSendEvent(content: "c2Vjb25k", region: "eastus", priority: 2),
                    },
                    partitionKey: "pk-2",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(HttpMethod.Post, captured.Method);
            StringAssert.EndsWith(captured.Uri!.ToString(), "/eventhub-1/events/batch?partitionKey=pk-2");
            using var body = JsonDocument.Parse(captured.Body!);
            Assert.AreEqual(JsonValueKind.Array, body.RootElement.ValueKind);
            Assert.AreEqual(2, body.RootElement.GetArrayLength());
            AssertSendEvent(body.RootElement[0], content: "Zmlyc3Q=", region: "westus", priority: 1);
            AssertSendEvent(body.RootElement[1], content: "c2Vjb25k", region: "eastus", priority: 2);
        }

        private static SendEvent CreateSendEvent(string content, string region, int priority)
        {
            return new SendEvent
            {
                Content = content,
                Properties = new Dictionary<string, ObjectEntity>
                {
                    ["region"] = new ObjectEntity { AdditionalProperties = { ["name"] = JsonSerializer.SerializeToElement(region) } },
                    ["priority"] = new ObjectEntity { AdditionalProperties = { ["level"] = JsonSerializer.SerializeToElement(priority) } },
                },
            };
        }

        private static void AssertSendEvent(JsonElement sendEvent, string content, string region, int priority)
        {
            Assert.AreEqual(content, sendEvent.GetProperty("ContentData").GetString());
            var properties = sendEvent.GetProperty("Properties");
            Assert.AreEqual(JsonValueKind.Object, properties.ValueKind);
            Assert.AreEqual(region, properties.GetProperty("region").GetProperty("name").GetString());
            Assert.AreEqual(priority, properties.GetProperty("priority").GetProperty("level").GetInt32());
            var keys = new List<string>();
            foreach (var property in properties.EnumerateObject())
            {
                keys.Add(property.Name);
            }

            CollectionAssert.AreEquivalent(new[] { "region", "priority" }, keys);
        }

        private static EventHubsClient CreateCapturingClient(CapturedRequest captured)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    captured.Method = request.Method;
                    captured.Uri = request.RequestUri;
                    captured.Body = request.Content is null
                        ? null
                        : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
                });

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            return new EventHubsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        private sealed class CapturedRequest
        {
            public HttpMethod? Method { get; set; }

            public Uri? Uri { get; set; }

            public string? Body { get; set; }
        }
    }
}
