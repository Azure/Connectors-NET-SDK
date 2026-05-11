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
using Azure.Connectors.Sdk.Yammer;
using Azure.Connectors.Sdk.Yammer.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated YammerClient class.
    /// </summary>
    [TestClass]
    public class YammerClientTests
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

        private static YammerClient CreateMockedClient(HttpResponseMessage response)
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

            return new YammerClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new YammerClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new YammerClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new YammerClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new YammerClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void Network_Serialization_RoundTrips()
        {
            var network = new Network
            {
                ID = "net-1",
                Name = "Test Network",
            };

            var json = JsonSerializer.Serialize(network);
            var deserialized = JsonSerializer.Deserialize<Network>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("net-1", deserialized!.ID);
            Assert.AreEqual("Test Network", deserialized.Name);
        }

        [TestMethod]
        public void User_Serialization_RoundTrips()
        {
            var user = new User
            {
                Username = "jdoe",
                FullName = "John Doe",
                Email = "jdoe@contoso.com",
                JobTitle = "Engineer",
            };

            var json = JsonSerializer.Serialize(user);
            var deserialized = JsonSerializer.Deserialize<User>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("jdoe", deserialized!.Username);
            Assert.AreEqual("John Doe", deserialized.FullName);
            Assert.AreEqual("jdoe@contoso.com", deserialized.Email);
        }

        [TestMethod]
        public async Task GetNetworksAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new List<Network>
            {
                new Network { ID = "1", Name = "Contoso" },
                new Network { ID = "2", Name = "Fabrikam" }
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetNetworksAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Contoso", result[0].Name);
        }

        [TestMethod]
        public async Task GetNetworksAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\": \"Unauthorized\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetNetworksAsync(cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
