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
using global::Azure.Core.Pipeline;
using Microsoft.Azure.Connectors.Sdk.Teams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated TeamsClient class.
    /// </summary>
    [TestClass]
    public class TeamsClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new TeamsClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new TeamsClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new TeamsClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new TeamsClient(
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
            var client = new TeamsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task GetAllTeamsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new GetAllTeamsResponse
            {
                Context = "https://graph.microsoft.com/beta/$metadata#teams",
                TeamsList = new List<object> { "team1", "team2", "team3" }
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

            using var client = new TeamsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .GetAllTeamsAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.TeamsList.Count);
        }

        [TestMethod]
        public async Task CreateChannelAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"error\": \"Access denied\"}")
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

            using var client = new TeamsClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .CreateChannelAsync(
                            team: "test-team-id",
                            input: new CreateChannelInput { Name = "Test Channel", Description = "A test channel" },
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(403, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Access denied", StringComparison.Ordinal));
        }

        [TestMethod]
        public void NewMeetingResponse_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var meeting = new NewMeetingResponse
            {
                ID = "meeting-123",
                Subject = "Team Standup",
                Importance = "normal",
                IsOrganizer = true,
                HasAttachments = false,
                OnlineMeetingURL = "https://teams.microsoft.com/meet/123"
            };

            // Assert
            Assert.AreEqual("meeting-123", meeting.ID);
            Assert.AreEqual("Team Standup", meeting.Subject);
            Assert.AreEqual("normal", meeting.Importance);
            Assert.AreEqual(true, meeting.IsOrganizer);
            Assert.AreEqual("https://teams.microsoft.com/meet/123", meeting.OnlineMeetingURL);
        }

        [TestMethod]
        public void CreateChannelResponse_JsonSerialization_RoundTrips()
        {
            // Arrange
            var channel = new CreateChannelResponse
            {
                ID = "channel-abc",
                DisplayName = "General",
                Description = "General discussion channel"
            };

            // Act
            var json = JsonSerializer.Serialize(channel);
            var deserialized = JsonSerializer.Deserialize<CreateChannelResponse>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(channel.ID, deserialized.ID);
            Assert.AreEqual(channel.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(channel.Description, deserialized.Description);
        }

        [TestMethod]
        public void AssociatedTeamInfo_JsonSerialization_RoundTrips()
        {
            // Arrange
            var team = new AssociatedTeamInfo
            {
                TeamID = "team-456",
                DisplayName = "Engineering",
                TenantID = "tenant-789"
            };

            // Act
            var json = JsonSerializer.Serialize(team);
            var deserialized = JsonSerializer.Deserialize<AssociatedTeamInfo>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(team.TeamID, deserialized.TeamID);
            Assert.AreEqual(team.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(team.TenantID, deserialized.TenantID);
        }
    }
}
