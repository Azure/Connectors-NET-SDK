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
using Azure.Connectors.Sdk.Wdatp;
using Azure.Connectors.Sdk.Wdatp.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated WdatpClient class (Microsoft Defender ATP).
    /// </summary>
    [TestClass]
    public class WdatpClientTests
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

        private static WdatpClient CreateMockedClient(HttpResponseMessage response)
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

            return new WdatpClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new WdatpClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new WdatpClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new WdatpClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new WdatpClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void Alert_Serialization_RoundTrips()
        {
            var alert = new Alert
            {
                Id = "alert-1",
                Title = "Suspicious activity",
                Description = "Unusual process execution detected",
                MachineId = "machine-1",
            };

            var json = JsonSerializer.Serialize(alert);
            var deserialized = JsonSerializer.Deserialize<Alert>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("alert-1", deserialized!.Id);
            Assert.AreEqual("Suspicious activity", deserialized.Title);
            Assert.AreEqual("machine-1", deserialized.MachineId);
        }

        [TestMethod]
        public void Investigation_Serialization_RoundTrips()
        {
            var investigation = new Investigation
            {
                Id = "inv-1",
                StatusDetails = "Running",
                ComputerDnsName = "DESKTOP-ABC123",
                MachineId = "machine-1",
            };

            var json = JsonSerializer.Serialize(investigation);
            var deserialized = JsonSerializer.Deserialize<Investigation>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("inv-1", deserialized!.Id);
            Assert.AreEqual("DESKTOP-ABC123", deserialized.ComputerDnsName);
        }

        [TestMethod]
        public async Task AdvancedHuntingAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new AdvancedHuntingResponse
            {
                Results = new List<object> { new { DeviceName = "DESKTOP-1" } }
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .AdvancedHuntingAsync(
                    input: new AdvancedHuntingInput { Query = "DeviceInfo | take 1" },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Results.Count);
        }

        [TestMethod]
        public async Task AdvancedHuntingAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Invalid query\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.AdvancedHuntingAsync(
                    input: new AdvancedHuntingInput { Query = "invalid" },
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
