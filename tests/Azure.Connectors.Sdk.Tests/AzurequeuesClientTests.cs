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
            var queues = new List<Queue>
            {
                new Queue { Name = "myqueue" }
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
        public void Queue_SerializationRoundTrip()
        {
            var original = new Queue { Name = "myqueue" };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<Queue>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("myqueue", deserialized.Name);
        }
    }
}
