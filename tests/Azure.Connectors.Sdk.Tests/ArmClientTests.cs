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
using Azure.Connectors.Sdk.Arm;
using Azure.Connectors.Sdk.Arm.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated ArmClient class.
    /// </summary>
    [TestClass]
    public class ArmClientTests
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

        /// <summary>
        /// Creates an ArmClient with a mocked handler that returns the specified response.
        /// Uses HttpClientTransport to inject the mock into the Azure.Core pipeline.
        /// </summary>
        private static ArmClient CreateMockedClient(HttpResponseMessage response)
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

            return new ArmClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        /// <summary>
        /// Verifies that the constructor creates a valid instance with a connection runtime URL.
        /// </summary>
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new ArmClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new ArmClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new ArmClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new ArmClient(
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
            var client = new ArmClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public void SubscriptionListResult_Pagination_RoundTrips()
        {
            // Arrange
            var page = new SubscriptionListResult
            {
                Value = new List<Subscription>
                {
                    new Subscription { SubscriptionId = "sub-1", DisplayName = "Sub One" },
                    new Subscription { SubscriptionId = "sub-2", DisplayName = "Sub Two" }
                },
                NextLink = "https://test.azure.com/nextpage"
            };

            // Act
            var json = JsonSerializer.Serialize(page);
            var deserialized = JsonSerializer.Deserialize<SubscriptionListResult>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(2, deserialized!.Value.Count);
            Assert.AreEqual("https://test.azure.com/nextpage", deserialized.NextLink);
        }

        [TestMethod]
        public void ResourceGroupListResult_Pagination_RoundTrips()
        {
            // Arrange
            var page = new ResourceGroupListResult
            {
                Value = new List<ResourceGroup>
                {
                    new ResourceGroup { Name = "rg-1", Location = "eastus" }
                },
                NextLink = "https://management.azure.com/subscriptions/sub-id/resourcegroups?$skiptoken=abc"
            };

            // Act
            var json = JsonSerializer.Serialize(page);
            var deserialized = JsonSerializer.Deserialize<ResourceGroupListResult>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized!.Value.Count);
            Assert.IsTrue(deserialized.NextLink.Contains("skiptoken", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task SubscriptionsGetAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var expectedResponse = new Subscription
            {
                Id = "/subscriptions/00000000-0000-0000-0000-000000000000",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                DisplayName = "Test Subscription",
                State = "Enabled",
                TenantId = "11111111-1111-1111-1111-111111111111"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            // Act
            var result = await client
                .SubscriptionsGetAsync(
                    subscription: "00000000-0000-0000-0000-000000000000",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("00000000-0000-0000-0000-000000000000", result.SubscriptionId);
            Assert.AreEqual("Test Subscription", result.DisplayName);
            Assert.AreEqual("Enabled", result.State.ToString());
        }

        [TestMethod]
        public async Task ResourceGroupsGetAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var expectedResponse = new ResourceGroup
            {
                Id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test-rg",
                Name = "test-rg",
                Location = "westus2"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            // Act
            var result = await client
                .ResourceGroupsGetAsync(
                    subscription: "00000000-0000-0000-0000-000000000000",
                    resourceGroup: "test-rg",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test-rg", result.Name);
            Assert.AreEqual("westus2", result.Location);
        }

        [TestMethod]
        public async Task ResourcesGetByIdAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Resource not found\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .ResourcesGetByIdAsync(
                            subscription: "00000000-0000-0000-0000-000000000000",
                            resourceGroup: "test-rg",
                            resourceProvider: "Microsoft.Compute",
                            shortResourceId: "virtualMachines/myVM",
                            clientApiVersion: "2023-03-01",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(404, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Resource not found", StringComparison.Ordinal));
        }

        [TestMethod]
        public void ConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new ConnectorException(
                connectorName: "arm",
                operation: "GET /subscriptions/sub-id/resourceGroups/test-rg",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.Status);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("GET /subscriptions/sub-id/resourceGroups/test-rg", StringComparison.Ordinal));
            Assert.IsTrue(exception.Message.Contains("403", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Subscription_JsonSerialization_RoundTrips()
        {
            // Arrange
            var subscription = new Subscription
            {
                Id = "/subscriptions/00000000-0000-0000-0000-000000000000",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                TenantId = "11111111-1111-1111-1111-111111111111",
                DisplayName = "My Subscription",
                State = "Enabled",
                AuthorizationSource = "RoleBased"
            };

            // Act
            var json = JsonSerializer.Serialize(subscription);
            var deserialized = JsonSerializer.Deserialize<Subscription>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("00000000-0000-0000-0000-000000000000", deserialized!.SubscriptionId);
            Assert.AreEqual("My Subscription", deserialized.DisplayName);
            Assert.AreEqual("Enabled", deserialized.State.ToString());
            Assert.AreEqual("11111111-1111-1111-1111-111111111111", deserialized.TenantId);
        }

        [TestMethod]
        public void ResourceGroup_JsonSerialization_RoundTrips()
        {
            // Arrange
            var resourceGroup = new ResourceGroup
            {
                Id = "/subscriptions/sub-id/resourceGroups/test-rg",
                Name = "test-rg",
                Location = "eastus",
                ManagedBy = "some-managed-id"
            };

            // Act
            var json = JsonSerializer.Serialize(resourceGroup);
            var deserialized = JsonSerializer.Deserialize<ResourceGroup>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("test-rg", deserialized!.Name);
            Assert.AreEqual("eastus", deserialized.Location);
            Assert.AreEqual("some-managed-id", deserialized.ManagedBy);
        }

        [TestMethod]
        public void GenericResource_JsonSerialization_RoundTrips()
        {
            // Arrange
            var resource = new GenericResource
            {
                Id = "/subscriptions/sub-id/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/myVM",
                Name = "myVM",
                Type = "Microsoft.Compute/virtualMachines",
                Location = "westus2",
                Kind = "Linux"
            };

            // Act
            var json = JsonSerializer.Serialize(resource);
            var deserialized = JsonSerializer.Deserialize<GenericResource>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("myVM", deserialized!.Name);
            Assert.AreEqual("Microsoft.Compute/virtualMachines", deserialized.Type);
            Assert.AreEqual("westus2", deserialized.Location);
            Assert.AreEqual("Linux", deserialized.Kind);
        }

        [TestMethod]
        public void DeploymentExtended_JsonSerialization_RoundTrips()
        {
            // Arrange
            var deployment = new DeploymentExtended
            {
                Id = "/subscriptions/sub-id/resourceGroups/test-rg/providers/Microsoft.Resources/deployments/my-deploy",
                Name = "my-deploy"
            };

            // Act
            var json = JsonSerializer.Serialize(deployment);
            var deserialized = JsonSerializer.Deserialize<DeploymentExtended>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("my-deploy", deserialized!.Name);
        }

        [TestMethod]
        public void TagDetails_JsonSerialization_RoundTrips()
        {
            // Arrange
            var tagDetails = new TagDetails
            {
                Id = "/subscriptions/sub-id/tagNames/environment",
                Name = "environment",
                Count = new TagCount
                {
                    Type = "Total",
                    Value = 5
                },
                Values = new List<TagValue>
                {
                    new TagValue
                    {
                        TagId = "tag-val-1",
                        TagValueValue = "production",
                        Count = new TagCount { Type = "Total", Value = 3 }
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(tagDetails);
            var deserialized = JsonSerializer.Deserialize<TagDetails>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("environment", deserialized!.Name);
            Assert.IsNotNull(deserialized.Count);
            Assert.AreEqual(5, deserialized.Count.Value);
            Assert.IsNotNull(deserialized.Values);
            Assert.AreEqual(1, deserialized.Values.Count);
            Assert.AreEqual("production", deserialized.Values[0].TagValueValue);
        }

        [TestMethod]
        public void Provider_JsonSerialization_RoundTrips()
        {
            // Arrange
            var provider = new Provider
            {
                Id = "/subscriptions/sub-id/providers/Microsoft.Compute",
                Namespace = "Microsoft.Compute",
                RegistrationState = "Registered"
            };

            // Act
            var json = JsonSerializer.Serialize(provider);
            var deserialized = JsonSerializer.Deserialize<Provider>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Microsoft.Compute", deserialized!.Namespace);
            Assert.AreEqual("Registered", deserialized.RegistrationState);
        }
    }
}
