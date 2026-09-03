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
using Azure.Connectors.Sdk;
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

        private static AzureIoTCentralClient CreateMockedClient(HttpResponseMessage response, Action<HttpRequestMessage>? requestCallback = null)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response)
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestCallback?.Invoke(request))
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
        public async Task SchemaDevicePropertiesAsync_TargetsV1Route()
        {
            HttpRequestMessage? capturedRequest = null;
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage, request => capturedRequest = request);

            await client
                .SchemaDevicePropertiesAsync(application: "app-id", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                "/connection/api/v1/_internal/workflow/schema/DeviceProperties",
                capturedRequest!.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task SchemaDeviceTelemetryAsync_TargetsV1Route()
        {
            HttpRequestMessage? capturedRequest = null;
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            };

            using var client = CreateMockedClient(responseMessage, request => capturedRequest = request);

            await client
                .SchemaDeviceTelemetryAsync(application: "app-id", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                "/connection/api/v1/_internal/workflow/schema/DeviceTelemetry",
                capturedRequest!.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task DeviceTemplatesListAsync_TargetsV1Route()
        {
            var request = await CapturePageableRequestAsync(
                    client => client.DeviceTemplatesListAsync(
                        application: "app-id",
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/api/v1/deviceTemplates",
                actual: request.RequestUri!.AbsolutePath);
        }

        [TestMethod]
        public async Task DeviceTemplatesListV0Async_TargetsPreviewRoute()
        {
            var request = await CapturePageableRequestAsync(
                    client => client.DeviceTemplatesListV0Async(
                        application: "app-id",
                        cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(
                expected: "/connection/api/preview/deviceTemplates",
                actual: request.RequestUri!.AbsolutePath);
        }

        private static async Task<HttpRequestMessage> CapturePageableRequestAsync<TItem>(
            Func<AzureIoTCentralClient, AsyncPageable<TItem>> createPageable)
            where TItem : notnull
        {
            var clientSetup = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"value\":[]}")
                });
            using var client = new AzureIoTCentralClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: clientSetup.Credential,
                options: clientSetup.Options);

            await foreach (var _ in createPageable(client).ConfigureAwait(continueOnCapturedContext: false))
            {
                // NOTE(daviburg): Enumeration triggers the lazy pageable request for route capture.
            }

            var request = clientSetup.GetLastRequest();
            Assert.IsNotNull(request);
            return request!;
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
            Assert.IsInstanceOfType<IPageable<DeviceGroup>>(collection);
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
            Assert.IsInstanceOfType<IPageable<Application>>(collection);
            Assert.AreEqual(2, collection.Value.Count);
            Assert.IsNull(collection.NextLink);
        }
    }
}
