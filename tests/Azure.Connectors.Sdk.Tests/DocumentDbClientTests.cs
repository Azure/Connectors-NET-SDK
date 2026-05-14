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
using Azure.Connectors.Sdk.Documentdb;
using Azure.Connectors.Sdk.Documentdb.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated DocumentDbClient class.
    /// </summary>
    [TestClass]
    public class DocumentDbClientTests
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

        private static DocumentDbClient CreateMockedClient(HttpResponseMessage response)
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

            return new DocumentDbClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new DocumentDbClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DocumentDbClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new DocumentDbClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new DocumentDbClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetCosmosDbAccountsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new CosmosDbAccountList
            {
                Value = new List<CosmosDbAccount>
                {
                    new CosmosDbAccount
                    {
                        AzureCosmosDBAccountName = "mycosmosdb",
                        AzureCosmosDBAccountDisplayName = "My Cosmos DB"
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
                .GetCosmosDbAccountsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Value.Count);
            Assert.AreEqual("mycosmosdb", result.Value[0].AzureCosmosDBAccountName);
        }

        [TestMethod]
        public async Task GetCosmosDbAccountsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\": \"Unauthorized\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetCosmosDbAccountsAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task CreateDocumentAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new PostDocumentsResponse
            {
                Id = "doc-001",
                Rid = "_rid_value",
                Self = "dbs/mydb/colls/mycoll/docs/doc-001/"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .CreateDocumentAsync(
                    azureCosmosDBAccountName: "mycosmosdb",
                    databaseId: "mydb",
                    collectionId: "mycoll",
                    input: new PostDocumentsRequest(),
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("doc-001", result.Id);
            Assert.AreEqual("_rid_value", result.Rid);
        }

        [TestMethod]
        public void CosmosDbAccountList_SerializationRoundTrip()
        {
            var original = new CosmosDbAccountList
            {
                Value = new List<CosmosDbAccount>
                {
                    new CosmosDbAccount
                    {
                        AzureCosmosDBAccountName = "testaccount",
                        AzureCosmosDBAccountDisplayName = "Test Account"
                    }
                }
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<CosmosDbAccountList>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.Value.Count);
            Assert.AreEqual("testaccount", deserialized.Value[0].AzureCosmosDBAccountName);
            Assert.AreEqual("Test Account", deserialized.Value[0].AzureCosmosDBAccountDisplayName);
        }

        [TestMethod]
        public void PostDocumentsResponse_SerializationRoundTrip()
        {
            var original = new PostDocumentsResponse
            {
                Id = "doc-123",
                Rid = "_rid_abc",
                Etag = "\"etag-value\"",
                Self = "dbs/db1/colls/coll1/docs/doc-123/"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<PostDocumentsResponse>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("doc-123", deserialized.Id);
            Assert.AreEqual("_rid_abc", deserialized.Rid);
            Assert.AreEqual("\"etag-value\"", deserialized.Etag);
        }
    }
}
