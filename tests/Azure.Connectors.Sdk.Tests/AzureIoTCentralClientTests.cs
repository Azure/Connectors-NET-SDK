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
using Azure.Connectors.Sdk.AzureIoTCentral;
using Azure.Connectors.Sdk.AzureIoTCentral.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzureIoTCentralClient class.
    /// </summary>
    [TestClass]
    public class AzureIoTCentralClientTests
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

        private static AzureIoTCentralClient CreateMockedClient(HttpResponseMessage response)
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

            return new AzureIoTCentralClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new AzureIoTCentralClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureIoTCentralClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new AzureIoTCentralClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new AzureIoTCentralClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task DeviceGroupsGetAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new DeviceGroup
            {
                DisplayName = "My Device Group",
                Description = "A test device group",
                Filter = "SELECT * FROM devices"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .DeviceGroupsGetAsync(deviceGroupId: "group-123", application: "app-id", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("My Device Group", result.DisplayName);
        }

        [TestMethod]
        public async Task DeviceGroupsGetAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Not Found\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.DeviceGroupsGetAsync(deviceGroupId: "nonexistent", application: "app-id", cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public void DeviceGroupCollection_ImplementsIPageable()
        {
            // Arrange & Act
            var collection = new DeviceGroupCollection
            {
                Value = new List<DeviceGroup>
                {
                    new DeviceGroup { DisplayName = "Group 1" },
                    new DeviceGroup { DisplayName = "Group 2" }
                },
                NextLink = "https://test.azure.com/deviceGroups?page=2"
            };

            // Assert
            Assert.AreEqual(2, collection.Value.Count);
            Assert.AreEqual("https://test.azure.com/deviceGroups?page=2", collection.NextLink);
        }

        [TestMethod]
        public void DeviceGroupCollection_SerializationRoundTrip()
        {
            var original = new DeviceGroupCollection
            {
                Value = new List<DeviceGroup>
                {
                    new DeviceGroup { DisplayName = "Test Group", Filter = "SELECT * FROM devices" }
                },
                NextLink = "https://test.azure.com/deviceGroups?page=2"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<DeviceGroupCollection>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.Value.Count);
            Assert.AreEqual("Test Group", deserialized.Value[0].DisplayName);
            Assert.AreEqual("https://test.azure.com/deviceGroups?page=2", deserialized.NextLink);
        }

        [TestMethod]
        public void ApplicationCollection_ImplementsIPageable()
        {
            // Arrange & Act
            var collection = new ApplicationCollection
            {
                Value = new List<Application>
                {
                    new Application { ApplicationName = "App 1" },
                    new Application { ApplicationName = "App 2" }
                },
                NextLink = null
            };

            // Assert
            Assert.AreEqual(2, collection.Value.Count);
            Assert.IsNull(collection.NextLink);
        }
    }
}
