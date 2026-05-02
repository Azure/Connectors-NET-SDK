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
using global::Azure.Core;
using Microsoft.Azure.Connectors.Sdk.Azureloganalytics;
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
        public async Task ListSubscriptionsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new SubscriptionListResult
            {
                Value = new List<Subscription>
                {
                    new Subscription
                    {
                        SubscriptionId = "00000000-0000-0000-0000-000000000001",
                        DisplayName = "Test Subscription",
                        State = "Enabled",
                        TenantId = "tenant-1"
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

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var items = new List<Subscription>();
            await foreach (var subscription in client.ListSubscriptionsAsync())
            {
                items.Add(subscription);
            }

            // Assert
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("00000000-0000-0000-0000-000000000001", items[0].SubscriptionId);
            Assert.AreEqual("Test Subscription", items[0].DisplayName);
        }

        [TestMethod]
        public async Task ListResourceGroupsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new ResourceGroupListResult
            {
                Value = new List<ResourceGroup>
                {
                    new ResourceGroup
                    {
                        Id = "/subscriptions/sub1/resourceGroups/rg1",
                        Name = "rg1"
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

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var items = new List<ResourceGroup>();
            await foreach (var rg in client.ListResourceGroupsAsync(subscription: "sub1"))
            {
                items.Add(rg);
            }

            // Assert
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("rg1", items[0].Name);
        }

        [TestMethod]
        public async Task ListWorkspaceNamesAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new ResourceGroupListResult
            {
                Value = new List<ResourceGroup>
                {
                    new ResourceGroup
                    {
                        Id = "/subscriptions/sub1/resourceGroups/rg1/providers/Microsoft.OperationalInsights/workspaces/ws1",
                        Name = "ws1"
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

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var items = new List<ResourceGroup>();
            await foreach (var ws in client.ListWorkspaceNamesAsync(subscription: "sub1", resourceGroup: "rg1"))
            {
                items.Add(ws);
            }

            // Assert
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("ws1", items[0].Name);
        }

        [TestMethod]
        public async Task ListArmQueryResultsSchemaAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new ObjectEntity
            {
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["columns"] = JsonSerializer.SerializeToElement(new[] { "TimeGenerated", "Computer" })
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

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var result = await client
                .ListArmQueryResultsSchemaAsync(
                    input: "Heartbeat | take 10",
                    subscription: "sub1",
                    resourceGroup: "rg1",
                    workspacesName: "myworkspace",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.AdditionalProperties.ContainsKey("columns"));
        }

        [TestMethod]
        public async Task ListArmQueryResultsSchemaAsync_WithErrorResponse_ThrowsConnectorException()
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
                            input: "invalid query |||",
                            subscription: "sub1",
                            resourceGroup: "rg1",
                            workspacesName: "myworkspace",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.StatusCode);
            Assert.IsTrue(exception.ResponseBody.Contains("Query syntax error", StringComparison.Ordinal));
        }

        [TestMethod]
        public void AzureloganalyticsConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new AzureloganalyticsConnectorException(
                operation: "POST /omsQuerySchema",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.StatusCode);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("POST /omsQuerySchema", StringComparison.Ordinal));
            Assert.IsTrue(exception.Message.Contains("403", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Subscription_JsonSerialization_RoundTrips()
        {
            // Arrange
            var subscription = new Subscription
            {
                Id = "/subscriptions/00000000-0000-0000-0000-000000000001",
                SubscriptionId = "00000000-0000-0000-0000-000000000001",
                TenantId = "tenant-1",
                DisplayName = "Test Subscription",
                State = "Enabled",
                AuthorizationSource = "RoleBased"
            };

            // Act
            var json = JsonSerializer.Serialize(subscription);
            var deserialized = JsonSerializer.Deserialize<Subscription>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("00000000-0000-0000-0000-000000000001", deserialized!.SubscriptionId);
            Assert.AreEqual("Test Subscription", deserialized.DisplayName);
            Assert.AreEqual("Enabled", deserialized.State);
            Assert.AreEqual("tenant-1", deserialized.TenantId);
            Assert.AreEqual("RoleBased", deserialized.AuthorizationSource);
        }

        [TestMethod]
        public void ResourceGroup_JsonSerialization_RoundTrips()
        {
            // Arrange
            var resourceGroup = new ResourceGroup
            {
                Id = "/subscriptions/sub1/resourceGroups/rg1",
                Name = "rg1",
                ManagedBy = "someone"
            };

            // Act
            var json = JsonSerializer.Serialize(resourceGroup);
            var deserialized = JsonSerializer.Deserialize<ResourceGroup>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("rg1", deserialized!.Name);
            Assert.AreEqual("/subscriptions/sub1/resourceGroups/rg1", deserialized.Id);
            Assert.AreEqual("someone", deserialized.ManagedBy);
        }

        [TestMethod]
        public void ObjectEntity_DynamicSchema_SerializesAdditionalProperties()
        {
            // Arrange
            var entity = new ObjectEntity
            {
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["columns"] = JsonSerializer.SerializeToElement(new[] { "TimeGenerated", "Computer" }),
                    ["rowCount"] = JsonSerializer.SerializeToElement(100)
                }
            };

            // Act
            var json = JsonSerializer.Serialize(entity);

            // Assert
            Assert.IsTrue(json.Contains("columns", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("TimeGenerated", StringComparison.Ordinal));
            Assert.IsTrue(json.Contains("100", StringComparison.Ordinal));
        }

        [TestMethod]
        public void ObjectEntity_DynamicSchema_DeserializesArbitraryProperties()
        {
            // Arrange
            var json = """{"columns":["TimeGenerated","Computer"],"rowCount":55}""";

            // Act
            var result = JsonSerializer.Deserialize<ObjectEntity>(json);

            // Assert
            Assert.IsNotNull(result!.AdditionalProperties);
            Assert.AreEqual(2, result.AdditionalProperties!.Count);
            Assert.AreEqual(55, result.AdditionalProperties["rowCount"].GetInt32());
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
        public void Dispose_WithInternallyCreatedHttpClient_ShouldDisposeIt()
        {
            // Arrange - no httpClient provided, so client creates its own
            var mockCredential = new Mock<TokenCredential>();
            var client = new AzureloganalyticsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task ListSubscriptionsAsync_PaginatesMultiplePages()
        {
            // Arrange
            var callCount = 0;
            var mockHandler = new Mock<HttpMessageHandler>();

            var page1 = new SubscriptionListResult
            {
                Value = new List<Subscription>
                {
                    new Subscription { SubscriptionId = "sub-1", DisplayName = "First" }
                },
                NextLink = "https://test.azure.com/connection/listSubscriptions?$skipToken=page2"
            };

            var page2 = new SubscriptionListResult
            {
                Value = new List<Subscription>
                {
                    new Subscription { SubscriptionId = "sub-2", DisplayName = "Second" }
                },
                NextLink = null
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    var page = callCount == 1 ? page1 : page2;
                    return new HttpResponseMessage
                    {
                        Content = new StringContent(JsonSerializer.Serialize(page))
                    };
                })
                .Verifiable();

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
            var items = new List<Subscription>();
            await foreach (var subscription in client.ListSubscriptionsAsync())
            {
                items.Add(subscription);
            }

            // Assert
            Assert.AreEqual(2, items.Count);
            Assert.AreEqual("sub-1", items[0].SubscriptionId);
            Assert.AreEqual("sub-2", items[1].SubscriptionId);
            Assert.AreEqual(2, callCount);
        }
    }
}
