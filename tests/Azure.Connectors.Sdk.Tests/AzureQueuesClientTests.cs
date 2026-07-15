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
using Azure.Connectors.Sdk.Azurequeues;
using Azure.Connectors.Sdk.Azurequeues.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzureQueuesClient class.
    /// </summary>
    [TestClass]
    public class AzureQueuesClientTests
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

        private static AzureQueuesClient CreateMockedClient(HttpResponseMessage response)
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

            return new AzureQueuesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        private static AzureQueuesClient CreateMockedClient(HttpResponseMessage response, Action<HttpRequestMessage> captureRequest)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) => captureRequest(request))
                .ReturnsAsync(response);

            var options = new ConnectorClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(mockHandler.Object)),
            };
            options.Retry.MaxRetries = 0;

            return new AzureQueuesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new AzureQueuesClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureQueuesClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new AzureQueuesClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new AzureQueuesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetStorageAccountsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new StorageAccountList
            {
                Value = new List<StorageAccount>
                {
                    new StorageAccount
                    {
                        StorageAccountNameOrQueueEndpoint = "mystorageaccount",
                        StorageAccountDisplayName = "My Storage Account"
                    }
                }
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetStorageAccountsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Value.Count);
            Assert.AreEqual("mystorageaccount", result.Value[0].StorageAccountNameOrQueueEndpoint);
        }

        [TestMethod]
        public async Task GetStorageAccountsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\": \"Internal Server Error\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetStorageAccountsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ListQueuesAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var queues = new List<QueueInfo>
            {
                new QueueInfo { Name = "myqueue" }
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(queues))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .ListQueuesAsync(
                    storageAccountNameOrQueueEndpoint: "mystorageaccount",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("myqueue", result[0].Name);
        }

        [TestMethod]
        public async Task ListQueuesAsync_QueueEndpoint_DoubleEncodesPathSegment()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            };
            Uri? requestUri = null;
            using var client = CreateMockedClient(responseMessage, request => requestUri = request.RequestUri!);

            await client
                .ListQueuesAsync(
                    storageAccountNameOrQueueEndpoint: "https://account.queue.core.windows.net",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(requestUri);
            Assert.IsTrue(
                requestUri.AbsolutePath.Contains(
                    "/v2/storageAccounts/https%253A%252F%252Faccount.queue.core.windows.net/queues/list",
                    StringComparison.Ordinal));
        }

        [TestMethod]
        public void StorageAccountList_SerializationRoundTrip()
        {
            var original = new StorageAccountList
            {
                Value = new List<StorageAccount>
                {
                    new StorageAccount
                    {
                        StorageAccountNameOrQueueEndpoint = "teststorage",
                        StorageAccountDisplayName = "Test Storage"
                    }
                }
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<StorageAccountList>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.Value.Count);
            Assert.AreEqual("teststorage", deserialized.Value[0].StorageAccountNameOrQueueEndpoint);
            Assert.AreEqual("Test Storage", deserialized.Value[0].StorageAccountDisplayName);
        }

        [TestMethod]
        public void QueueInfo_SerializationRoundTrip()
        {
            var original = new QueueInfo { Name = "myqueue" };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<QueueInfo>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("myqueue", deserialized.Name);
        }

        [TestMethod]
        public void Messages_NestedQueueMessageResponse_DeserializesExpectedValues()
        {
            const string json = """
                {
                    "QueueMessagesList": {
                        "QueueMessage": [
                            {
                                "MessageId": "message-1",
                                "InsertionTime": "2026-07-14T11:59:00Z",
                                "ExpirationTime": "2026-07-21T11:59:00Z",
                                "PopReceipt": "receipt-1",
                                "TimeNextVisible": "2026-07-14T12:00:00Z",
                                "DequeueCount": "3",
                                "MessageText": "hello"
                            }
                        ]
                    }
                }
                """;

            var response = JsonSerializer.Deserialize<Messages>(json);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.QueueMessagesList);
            Assert.HasCount(1, response.QueueMessagesList.QueueMessage);
            var message = response.QueueMessagesList.QueueMessage[0];
            Assert.AreEqual("message-1", message.MessageId);
            Assert.AreEqual("2026-07-14T12:00:00Z", message.NextVisibleTime);
            Assert.AreEqual("3", message.DequeueCount);
            Assert.AreEqual("hello", message.MessageText);
        }
    }
}
