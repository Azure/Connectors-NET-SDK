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
using Azure.Connectors.Sdk.Azuretables;
using Azure.Connectors.Sdk.Azuretables.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzuretablesClient class.
    /// </summary>
    [TestClass]
    public class AzuretablesClientTests
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

        private static AzuretablesClient CreateMockedClient(HttpResponseMessage response)
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

            return new AzuretablesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new AzuretablesClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzuretablesClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new AzuretablesClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new AzuretablesClient(
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
                        StorageAccountNameOrTableEndpoint = "mytablestorage",
                        StorageAccountDisplayName = "My Table Storage"
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
            Assert.AreEqual("mytablestorage", result.Value[0].StorageAccountNameOrTableEndpoint);
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
        public async Task CreateEntityAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new InsertEntityResponse
            {
                PartitionKey = "partition1",
                RowKey = "row1",
                EntityData = "{}"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .CreateEntityAsync(
                    storageAccountNameOrTableEndpoint: "mytablestorage",
                    table: "mytable",
                    input: new CreateEntityInput(),
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("partition1", result.PartitionKey);
            Assert.AreEqual("row1", result.RowKey);
        }

        [TestMethod]
        public void InsertEntityResponse_SerializationRoundTrip()
        {
            var original = new InsertEntityResponse
            {
                PartitionKey = "mypartition",
                RowKey = "myrow",
                EntityMetadataLocation = "https://test.azure.com/entity",
                EntityData = "{\"name\":\"test\"}"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<InsertEntityResponse>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("mypartition", deserialized.PartitionKey);
            Assert.AreEqual("myrow", deserialized.RowKey);
            Assert.AreEqual("https://test.azure.com/entity", deserialized.EntityMetadataLocation);
        }
    }
}
