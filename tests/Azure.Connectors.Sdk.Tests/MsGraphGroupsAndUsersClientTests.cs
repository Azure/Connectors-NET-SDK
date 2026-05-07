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
using Azure.Connectors.Sdk.MsGraphGroupsAndUsers;
using Azure.Connectors.Sdk.MsGraphGroupsAndUsers.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated MsGraphGroupsAndUsersClient class.
    /// </summary>
    [TestClass]
    public class MsGraphGroupsAndUsersClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new MsGraphGroupsAndUsersClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new MsGraphGroupsAndUsersClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new MsGraphGroupsAndUsersClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new MsGraphGroupsAndUsersClient(
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
            var client = new MsGraphGroupsAndUsersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task ListUsersAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new ListUsersResponse
            {
                Context = "https://graph.microsoft.com/v1.0/$metadata#users",
                Value = new List<object> { "user1", "user2" }
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new MsGraphGroupsAndUsersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .ListUsersAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Value.Count);
        }

        [TestMethod]
        public async Task GetGroupPropertiesAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Group not found\"}")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new MsGraphGroupsAndUsersClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .GetGroupPropertiesAsync(objectIDOfTheMicrosoftEntraIDGroup: "test-group-id", cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(404, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Group not found", StringComparison.Ordinal));
        }

        [TestMethod]
        public void GetGroupPropertiesResponse_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var group = new GetGroupPropertiesResponse
            {
                Id = "group-123",
                DisplayName = "Engineering Team",
                Description = "The engineering team group",
                Mail = "engineering@contoso.com",
                MailEnabled = true,
                SecurityEnabled = true,
                Visibility = "Private"
            };

            // Assert
            Assert.AreEqual("group-123", group.Id);
            Assert.AreEqual("Engineering Team", group.DisplayName);
            Assert.AreEqual("engineering@contoso.com", group.Mail);
            Assert.AreEqual(true, group.MailEnabled);
            Assert.AreEqual(true, group.SecurityEnabled);
        }

        [TestMethod]
        public void GetGroupPropertiesResponse_JsonSerialization_RoundTrips()
        {
            // Arrange
            var group = new GetGroupPropertiesResponse
            {
                Id = "group-abc",
                DisplayName = "Test Group",
                Description = "A test group",
                Mail = "test@contoso.com",
                MailEnabled = false,
                SecurityEnabled = true,
                Visibility = "Public"
            };

            // Act
            var json = JsonSerializer.Serialize(group);
            var deserialized = JsonSerializer.Deserialize<GetGroupPropertiesResponse>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(group.Id, deserialized.Id);
            Assert.AreEqual(group.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(group.Description, deserialized.Description);
            Assert.AreEqual(group.Mail, deserialized.Mail);
            Assert.AreEqual(group.MailEnabled, deserialized.MailEnabled);
            Assert.AreEqual(group.SecurityEnabled, deserialized.SecurityEnabled);
        }

        [TestMethod]
        public void GetMemberGroupsInput_JsonSerialization_RoundTrips()
        {
            // Arrange
            var input = new GetMemberGroupsInput
            {
                SecurityEnabledOnly = true
            };

            // Act
            var json = JsonSerializer.Serialize(input);
            var deserialized = JsonSerializer.Deserialize<GetMemberGroupsInput>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(input.SecurityEnabledOnly, deserialized.SecurityEnabledOnly);
        }
    }
}
