//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.Kusto;
using Azure.Connectors.Sdk.Kusto.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated KustoClient class.
    /// </summary>
    [TestClass]
    public class KustoClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new KustoClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new KustoClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new KustoClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public async Task ListKustoResultsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new Table
            {
                Value = new List<Row>
                {
                    new Row
                    {
                        AdditionalProperties = new Dictionary<string, JsonElement>
                        {
                            ["Timestamp"] = JsonSerializer.SerializeToElement("2026-04-03T00:00:00Z"),
                            ["Count"] = JsonSerializer.SerializeToElement(42)
                        }
                    }
                }
            };

            using var responseMessage = new HttpResponseMessage
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Callback(() => { })
                .Verifiable();

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new KustoClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .ListKustoResultsAsync(
                    input: new QueryAndListSchema
                    {
                        Cluster = "https://mycluster.kusto.windows.net",
                        Db = "mydb",
                        Csl = "StormEvents | take 10"
                    },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(1, result.Value.Count);
            Assert.AreEqual(42, result.Value[0].AdditionalProperties["Count"].GetInt32());
        }

        [TestMethod]
        public async Task ListKustoResultsAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Query syntax error\"}")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Callback(() => { })
                .Verifiable();

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new KustoClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .ListKustoResultsAsync(
                            input: new QueryAndListSchema
                            {
                                Cluster = "https://mycluster.kusto.windows.net",
                                Db = "mydb",
                                Csl = "invalid query |||"
                            },
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Query syntax error", StringComparison.Ordinal));
        }

        [TestMethod]
        public void ConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new ConnectorException("kusto",
                operation: "POST /ListKustoResults/false",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.Status);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("POST /ListKustoResults/false", StringComparison.Ordinal));
            Assert.IsTrue(exception.Message.Contains("403", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Row_DynamicSchema_SerializesAdditionalProperties()
        {
            // Arrange
            var row = new Row
            {
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["Timestamp"] = JsonSerializer.SerializeToElement("2026-04-03T12:00:00Z"),
                    ["Source"] = JsonSerializer.SerializeToElement("East US"),
                    ["EventCount"] = JsonSerializer.SerializeToElement(100)
                }
            };

            // Act
            var json = JsonSerializer.Serialize(row);

            // Assert
            Assert.IsTrue(json.Contains("Timestamp", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("East US", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("100", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Row_DynamicSchema_DeserializesArbitraryProperties()
        {
            // Arrange
            var json = """{"Timestamp":"2026-04-03","Source":"West US","Count":55}""";

            // Act
            var result = JsonSerializer.Deserialize<Row>(json);

            // Assert
            Assert.IsNotNull(result!.AdditionalProperties);
            Assert.AreEqual(3, result.AdditionalProperties!.Count);
            Assert.AreEqual("West US", result.AdditionalProperties["Source"].GetString());
            Assert.AreEqual(55, result.AdditionalProperties["Count"].GetInt32());
        }

        [TestMethod]
        public void Row_HasDynamicSchemaAttribute()
        {
            // Arrange & Act
            var attribute = typeof(Row).GetCustomAttribute<DynamicSchemaAttribute>();

            // Assert
            Assert.IsNotNull(attribute, message: "Row should have [DynamicSchema] attribute.");
            Assert.AreEqual("listKustoResultsSchemaPost", attribute!.OperationId);
        }

        [TestMethod]
        public void VisualizeResults_JsonSerialization_RoundTrips()
        {
            // Arrange
            var results = new VisualizeResults
            {
                Body = "base64encodedchart",
                BodyHtml = "<img src='data:image/png;base64,...' />",
                AttachmentContent = "chartdata",
                AttachmentName = "chart.png",
                LinksToKustoExplorer = "https://dataexplorer.azure.com/..."
            };

            // Act
            var json = JsonSerializer.Serialize(results);
            var deserialized = JsonSerializer.Deserialize<VisualizeResults>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("base64encodedchart", deserialized!.Body);
            Assert.AreEqual("chart.png", deserialized.AttachmentName);
            Assert.AreEqual("https://dataexplorer.azure.com/...", deserialized.LinksToKustoExplorer);
        }

        [TestMethod]
        public void AsyncCommandResult_JsonSerialization_RoundTrips()
        {
            // Arrange
            var result = new AsyncCommandResult
            {
                State = "Completed",
                Status = "Success",
                OperationId = "op-12345"
            };

            // Act
            var json = JsonSerializer.Serialize(result);
            var deserialized = JsonSerializer.Deserialize<AsyncCommandResult>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Completed", deserialized!.State);
            Assert.AreEqual("Success", deserialized.Status);
            Assert.AreEqual("op-12345", deserialized.OperationId);
        }

        [TestMethod]
        public void QueryAndListSchema_JsonSerialization_RoundTrips()
        {
            // Arrange
            var schema = new QueryAndListSchema
            {
                Cluster = "https://mycluster.kusto.windows.net",
                Db = "mydb",
                Csl = "StormEvents | take 10"
            };

            // Act
            var json = JsonSerializer.Serialize(schema);
            var deserialized = JsonSerializer.Deserialize<QueryAndListSchema>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("https://mycluster.kusto.windows.net", deserialized!.Cluster);
            Assert.AreEqual("mydb", deserialized.Db);
            Assert.AreEqual("StormEvents | take 10", deserialized.Csl);
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new KustoClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act & Assert - calling Dispose twice should not throw (idempotent)
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_WithInternallyCreatedHttpClient_ShouldDisposeIt()
        {
            // Arrange - no httpClient provided, so client creates its own
            var mockCredential = new Mock<TokenCredential>();
            var client = new KustoClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }
    }
}
