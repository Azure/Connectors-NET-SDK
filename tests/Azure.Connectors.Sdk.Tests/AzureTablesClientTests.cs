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
using Azure.Connectors.Sdk.AzureTables;
using Azure.Connectors.Sdk.AzureTables.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzureTablesClient class.
    /// </summary>
    [TestClass]
    public class AzureTablesClientTests
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

        private static AzureTablesClient CreateMockedClient(HttpResponseMessage response)
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

            return new AzureTablesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public async Task CreateTableAsync_WithValidName_SendsRawUtf8Body()
        {
            const string tableName = "SdkSerializationProbe";
            byte[]? requestBytes = null;
            string? requestContentType = null;
            string? requestMethod = null;
            string? requestPath = null;
            var requestCount = 0;
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    requestCount++;
                    requestBytes = await request.Content!
                        .ReadAsByteArrayAsync(cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: false);
                    requestContentType = request.Content.Headers.ContentType?.MediaType;
                    requestMethod = request.Method.Method;
                    requestPath = request.RequestUri!.AbsolutePath;
                    var rawName = System.Text.Encoding.UTF8.GetBytes(tableName);
                    var validName = System.Linq.Enumerable.SequenceEqual(requestBytes, rawName);
                    return new HttpResponseMessage(validName ? HttpStatusCode.Created : HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(validName
                            ? "{\"TableName\":\"SdkSerializationProbe\"}"
                            : "{\"odata.error\":{\"code\":\"InvalidResourceName\",\"message\":{\"value\":\"The specified resource name contains invalid characters.\"}}}"),
                    };
                });
            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(handler.Object));
            options.Retry.MaxRetries = 0;
            using var client = new AzureTablesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: AzureTablesClientTests.SharedMockCredential.Object,
                options: options);

            var result = await client
                .CreateTableAsync(
                    storageAccountNameOrTableEndpoint: "AccountNameFromSettings",
                    input: tableName,
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: tableName, actual: result.TableName);
            CollectionAssert.AreEqual(expected: System.Text.Encoding.UTF8.GetBytes(tableName), actual: requestBytes);
            Assert.AreEqual(expected: "application/json", actual: requestContentType);
            Assert.AreEqual(expected: "POST", actual: requestMethod);
            Assert.AreEqual(expected: "/connection/v2/storageAccounts/AccountNameFromSettings/tables", actual: requestPath);
            Assert.AreEqual(expected: 1, actual: requestCount);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new AzureTablesClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureTablesClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new AzureTablesClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new AzureTablesClient(
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
                RowKey = "row1"
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
                EntityMetadataLocation = "https://test.azure.com/entity"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<InsertEntityResponse>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("mypartition", deserialized.PartitionKey);
            Assert.AreEqual("myrow", deserialized.RowKey);
            Assert.AreEqual("https://test.azure.com/entity", deserialized.EntityMetadataLocation);
        }

        [TestMethod]
        public async Task GetEntitiesAsync_TwoPagesAndResume_FollowsNextLinkAndPreservesMetadata()
        {
            var requestedUris = new List<string>();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken _) =>
                {
                    var uri = request.RequestUri!.AbsoluteUri;
                    requestedUris.Add(uri);
                    if (uri.Contains("NextRowKey=row-2", StringComparison.Ordinal))
                    {
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("""
                                {
                                    "odata.metadata": "https://contoso.table.core.windows.net/$metadata#Customers",
                                    "value": [{
                                        "PartitionKey": "p1",
                                        "RowKey": "row-2",
                                        "additionalProperties": "{\"Name\":\"Contoso West\"}"
                                    }]
                                }
                                """)
                        });
                    }

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""
                            {
                                "odata.metadata": "https://contoso.table.core.windows.net/$metadata#Customers",
                                "value": [{
                                    "PartitionKey": "p1",
                                    "RowKey": "row-1",
                                    "additionalProperties": "{\"Name\":\"Contoso East\"}"
                                }],
                                "nextLink": "https://test.azure.com/connection/v2/storageAccounts/https%253A%252F%252Fcontoso.table.core.windows.net/tables/Customers/entities?NextPartitionKey=p1&NextRowKey=row-2"
                            }
                            """)
                    });
                });

            var options = new ConnectorClientOptions
            {
                Transport = new HttpClientTransport(new HttpClient(handler.Object))
            };
            options.Retry.MaxRetries = 0;

            using var client = new AzureTablesClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);

            var pageable = client.GetEntitiesAsync(
                storageAccountNameOrTableEndpoint: "https://contoso.table.core.windows.net",
                table: "Customers",
                filterQuery: "PartitionKey eq 'p1'",
                selectQuery: "PartitionKey,RowKey,Name");

            var pages = new List<global::Azure.Page<EntityItem>>();
            await foreach (var page in pageable.AsPages().ConfigureAwait(continueOnCapturedContext: false))
            {
                pages.Add(page);
            }

            Assert.AreEqual(expected: 2, actual: pages.Count);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/v2/storageAccounts/https%253A%252F%252Fcontoso.table.core.windows.net/tables/Customers/entities?$filter=PartitionKey%20eq%20%27p1%27&$select=PartitionKey%2CRowKey%2CName",
                actual: requestedUris[0]);
            Assert.AreEqual(
                expected: "https://test.azure.com/connection/v2/storageAccounts/https%253A%252F%252Fcontoso.table.core.windows.net/tables/Customers/entities?NextPartitionKey=p1&NextRowKey=row-2",
                actual: requestedUris[1]);
            Assert.AreEqual(expected: "row-1", actual: pages[0].Values[0].RowKey);
            Assert.AreEqual(expected: "{\"Name\":\"Contoso East\"}", actual: pages[0].Values[0].EntityData);
            Assert.AreEqual(expected: "row-2", actual: pages[1].Values[0].RowKey);

            var typedPage1 = pages[0] as ConnectorPage<GetEntitiesResponse, EntityItem>;
            Assert.IsNotNull(typedPage1);
            Assert.AreEqual(
                expected: "https://contoso.table.core.windows.net/$metadata#Customers",
                actual: typedPage1.PageModel.TableMetadataLocation);

            var resumed = new List<global::Azure.Page<EntityItem>>();
            await foreach (var page in pageable.AsPages(continuationToken: pages[0].ContinuationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                resumed.Add(page);
            }

            Assert.AreEqual(expected: 1, actual: resumed.Count);
            Assert.AreEqual(expected: "row-2", actual: resumed[0].Values[0].RowKey);
        }
    }
}
