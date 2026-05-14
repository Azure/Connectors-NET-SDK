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
using Azure.Connectors.Sdk.AzureAD;
using Azure.Connectors.Sdk.AzureAD.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzureADClient class.
    /// </summary>
    [TestClass]
    public class AzureADClientTests
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

        private static AzureADClient CreateMockedClient(HttpResponseMessage response)
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

            return new AzureADClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            using var client = new AzureADClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureADClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new AzureADClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new AzureADClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetUserAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new GetUserResponse
            {
                DisplayName = "John Doe",
                Mail = "john.doe@example.com",
                UserPrincipalName = "john.doe@example.com"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetUserAsync(userIdOrPrincipalName: "john.doe@example.com", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("John Doe", result.DisplayName);
            Assert.AreEqual("john.doe@example.com", result.Mail);
        }

        [TestMethod]
        public async Task GetUserAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Not Found\"}")
            };

            using var client = CreateMockedClient(responseMessage);

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetUserAsync(userIdOrPrincipalName: "nonexistent@example.com", cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task GetGroupAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            var expectedResponse = new GetGroupResponse
            {
                DisplayName = "Engineering",
                Description = "Engineering team group"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            using var client = CreateMockedClient(responseMessage);

            var result = await client
                .GetGroupAsync(groupId: "group-id-123", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
            Assert.AreEqual("Engineering", result.DisplayName);
        }

        [TestMethod]
        public void GetGroupMembersResponse_ImplementsIPageable()
        {
            // Arrange & Act
            var response = new GetGroupMembersResponse
            {
                Value = new List<GetUserResponse>
                {
                    new GetUserResponse { DisplayName = "User 1" },
                    new GetUserResponse { DisplayName = "User 2" }
                },
                NextLink = "https://test.azure.com/nextpage"
            };

            // Assert
            Assert.AreEqual(2, response.Value.Count);
            Assert.AreEqual("https://test.azure.com/nextpage", response.NextLink);
        }

        [TestMethod]
        public void GetGroupMembersResponse_SerializationRoundTrip()
        {
            var original = new GetGroupMembersResponse
            {
                Value = new List<GetUserResponse>
                {
                    new GetUserResponse { DisplayName = "Alice", Mail = "alice@example.com" }
                },
                NextLink = "https://test.azure.com/groups/members?page=2"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<GetGroupMembersResponse>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.Value.Count);
            Assert.AreEqual("Alice", deserialized.Value[0].DisplayName);
            Assert.AreEqual("https://test.azure.com/groups/members?page=2", deserialized.NextLink);
        }

        [TestMethod]
        public void GetUserResponse_SerializationRoundTrip()
        {
            var original = new GetUserResponse
            {
                DisplayName = "Bob Smith",
                Mail = "bob.smith@example.com",
                UserPrincipalName = "bob.smith@example.com",
                GivenName = "Bob",
                Surname = "Smith"
            };

            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<GetUserResponse>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual("Bob Smith", deserialized.DisplayName);
            Assert.AreEqual("bob.smith@example.com", deserialized.Mail);
            Assert.AreEqual("Bob", deserialized.GivenName);
            Assert.AreEqual("Smith", deserialized.Surname);
        }
    }
}
