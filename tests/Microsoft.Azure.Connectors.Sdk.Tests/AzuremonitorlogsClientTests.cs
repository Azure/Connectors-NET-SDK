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
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.Azure.Connectors.Sdk.Azuremonitorlogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzuremonitorlogsClient class.
    /// </summary>
    [TestClass]
    public class AzuremonitorlogsClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new AzuremonitorlogsClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzuremonitorlogsClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new AzuremonitorlogsClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public async Task QueryDataAsync_WithMockedResponse_ReturnsExpectedResult()
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
                            ["TimeGenerated"] = JsonSerializer.SerializeToElement("2026-05-01T00:00:00Z"),
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

            using var client = new AzuremonitorlogsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .QueryDataAsync(
                    input: "Heartbeat | take 10",
                    subscription: "sub-1",
                    resourceGroup: "rg-1",
                    resourceType: "Microsoft.OperationalInsights/workspaces",
                    resourceName: "my-workspace",
                    timeRange: "Last 24 hours",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(1, result.Value.Count);
            Assert.AreEqual(42, result.Value[0].AdditionalProperties["Count"].GetInt32());
        }

        [TestMethod]
        public async Task QueryDataAsync_WithErrorResponse_ThrowsConnectorException()
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

            using var client = new AzuremonitorlogsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .QueryDataAsync(
                            input: "invalid query |||",
                            subscription: "sub-1",
                            resourceGroup: "rg-1",
                            resourceType: "Microsoft.OperationalInsights/workspaces",
                            resourceName: "my-workspace",
                            timeRange: "Last 24 hours",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.StatusCode);
            Assert.IsTrue(exception.ResponseBody.Contains("Query syntax error", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task VisualizeQueryAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new VisualizeResults
            {
                Body = "base64encodedchart",
                AttachmentContent = "chartdata",
                AttachmentName = "chart.png"
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

            using var client = new AzuremonitorlogsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .VisualizeQueryAsync(
                    input: "Heartbeat | summarize count() by Computer",
                    subscription: "sub-1",
                    resourceGroup: "rg-1",
                    resourceType: "Microsoft.OperationalInsights/workspaces",
                    resourceName: "my-workspace",
                    timeRange: "Last 24 hours",
                    chartType: "piechart",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("base64encodedchart", result.Body);
            Assert.AreEqual("chart.png", result.AttachmentName);
        }

        [TestMethod]
        public void ConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new ConnectorException("azuremonitorlogs",
                operation: "POST /queryData",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.StatusCode);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("POST /queryData", StringComparison.Ordinal));
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
                    ["TimeGenerated"] = JsonSerializer.SerializeToElement("2026-05-01T12:00:00Z"),
                    ["Computer"] = JsonSerializer.SerializeToElement("web-server-01"),
                    ["CounterValue"] = JsonSerializer.SerializeToElement(95.5)
                }
            };

            // Act
            var json = JsonSerializer.Serialize(row);

            // Assert
            Assert.IsTrue(json.Contains("TimeGenerated", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("web-server-01", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("95.5", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Row_DynamicSchema_DeserializesArbitraryProperties()
        {
            // Arrange
            var json = """{"TimeGenerated":"2026-05-01","Computer":"db-server-02","Count":55}""";

            // Act
            var result = JsonSerializer.Deserialize<Row>(json);

            // Assert
            Assert.IsNotNull(result!.AdditionalProperties);
            Assert.AreEqual(3, result.AdditionalProperties!.Count);
            Assert.AreEqual("db-server-02", result.AdditionalProperties["Computer"].GetString());
            Assert.AreEqual(55, result.AdditionalProperties["Count"].GetInt32());
        }

        [TestMethod]
        public void Row_HasDynamicSchemaAttribute()
        {
            // Arrange & Act
            var attribute = typeof(Row).GetCustomAttribute<DynamicSchemaAttribute>();

            // Assert
            Assert.IsNotNull(attribute, message: "Row should have [DynamicSchema] attribute.");
            Assert.AreEqual("QuerySchema", attribute!.OperationId);
        }

        [TestMethod]
        public void VisualizeResults_JsonSerialization_RoundTrips()
        {
            // Arrange
            var results = new VisualizeResults
            {
                Body = "base64encodedchart",
                AttachmentContent = "chartdata",
                AttachmentName = "chart.png"
            };

            // Act
            var json = JsonSerializer.Serialize(results);
            var deserialized = JsonSerializer.Deserialize<VisualizeResults>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("base64encodedchart", deserialized!.Body);
            Assert.AreEqual("chart.png", deserialized.AttachmentName);
        }

        [TestMethod]
        public void QueryDataInput_JsonSerialization_RoundTrips()
        {
            // Arrange
            var input = new QueryDataInput
            {
                Query = "Heartbeat | summarize count()",
                TimeRangeType = "Relative",
                Timerange = "Last 24 hours"
            };

            // Act
            var json = JsonSerializer.Serialize(input);
            var deserialized = JsonSerializer.Deserialize<QueryDataInput>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Heartbeat | summarize count()", deserialized!.Query);
            Assert.AreEqual("Relative", deserialized.TimeRangeType);
        }

        [TestMethod]
        public void VisualizeQueryInput_JsonSerialization_RoundTrips()
        {
            // Arrange
            var input = new VisualizeQueryInput
            {
                Query = "Heartbeat | summarize count() by Computer",
                TimeRangeType = "Relative",
                Timerange = "Last 7 days"
            };

            // Act
            var json = JsonSerializer.Serialize(input);
            var deserialized = JsonSerializer.Deserialize<VisualizeQueryInput>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Heartbeat | summarize count() by Computer", deserialized!.Query);
            Assert.AreEqual("Last 7 days", deserialized.Timerange?.ToString());
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new AzuremonitorlogsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
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
            var client = new AzuremonitorlogsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }
    }
}
