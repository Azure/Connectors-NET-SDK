//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using global::Azure.Core;
using Microsoft.Azure.Connectors.DirectClient.Azureloganalytics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzureloganalyticsClient class.
    /// </summary>
    [TestClass]
    public class AzureloganalyticsClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new AzureloganalyticsClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureloganalyticsClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new AzureloganalyticsClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_WithInjectedHttpClient_ShouldNotDisposeIt()
        {
            // Arrange
            var httpClient = new HttpClient();
            var mockCredential = new Mock<TokenCredential>();

            var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            client.Dispose();

            // Assert - injected HttpClient should still be usable (not disposed)
            httpClient.DefaultRequestHeaders.Add("X-Test-Header", "TestValue");
            Assert.IsTrue(httpClient.DefaultRequestHeaders.Contains("X-Test-Header"));
        }

        [TestMethod]
        public async Task ListArmQueryResultsSchemaAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var responseJson = "{\"tableName\":\"TestTable\",\"columns\":[{\"name\":\"col1\"}]}";

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var result = await client
                .ListArmQueryResultsSchemaAsync(
                    input: "{}",
                    subscription: "sub-1",
                    resourceGroup: "rg-1",
                    workspacesName: "workspace-1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.AdditionalProperties.ContainsKey("tableName"));
            Assert.AreEqual("TestTable", result.AdditionalProperties["tableName"].GetString());
        }

        [TestMethod]
        public async Task ListArmQueryResultsSchemaAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("{\"error\": \"Invalid credentials\"}")
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<AzureloganalyticsConnectorException>(async () =>
                    await client
                        .ListArmQueryResultsSchemaAsync(
                            input: "{}",
                            subscription: "sub-1",
                            resourceGroup: "rg-1",
                            workspacesName: "workspace-1",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(401, exception.StatusCode);
            Assert.IsTrue(exception.ResponseBody.Contains("Invalid credentials"));
        }

        [TestMethod]
        public void Subscription_JsonSerialization_RoundTrips()
        {
            // Arrange
            var subscription = new Subscription
            {
                DisplayName = "Test Subscription",
                SubscriptionId = "sub-123"
            };

            // Act
            var json = JsonSerializer.Serialize(subscription);
            var deserialized = JsonSerializer.Deserialize<Subscription>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(subscription.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(subscription.SubscriptionId, deserialized.SubscriptionId);
        }
    }
}
