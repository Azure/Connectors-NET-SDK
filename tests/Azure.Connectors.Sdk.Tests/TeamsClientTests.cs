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
using Azure.Connectors.Sdk.Teams;
using Azure.Connectors.Sdk.Teams.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated TeamsClient class.
    /// </summary>
    [TestClass]
    public class TeamsClientTests
    {
        private static TeamsClient CreateMockedClient(Func<HttpResponseMessage> responseFactory)
        {
            var (credential, options) = ConnectorTestHelpers.CreateMockedClientSetup(responseFactory);
            return new TeamsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: credential,
                options: options);
        }

        [TestMethod]
        [Owner("daviburg")]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AIInsightOperations_WithMockedResponse_PreserveWireContract(bool list)
        {
            var (credential, options, getLastRequest) = ConnectorTestHelpers.CreateCapturingClientSetup(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(list
                    ? """{"@odata.context":"https://graph.microsoft.com/metadata","value":[{"id":"insight-1","callId":"call-1"}]}"""
                    : """{"id":"insight-1","callId":"call-1"}"""),
            });
            using var client = new TeamsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: credential,
                options: options);

            if (list)
            {
                AIInsightCollectionResponse result = await client
                    .ListAIInsightsAsync(meetingId: "meeting-1")
                    .ConfigureAwait(continueOnCapturedContext: false);
                var insights = result.AIInsights;
                Assert.IsNotNull(insights);
                Assert.AreEqual(expected: 1, actual: insights.Count);
                Assert.AreEqual(expected: "insight-1", actual: insights[0].AIInsightId);
                Assert.AreEqual(expected: "call-1", actual: insights[0].CallId);
                Assert.AreEqual(expected: "https://graph.microsoft.com/metadata", actual: result.Context);
            }
            else
            {
                AIInsightResponse result = await client
                    .GetAIInsightAsync(meetingId: "meeting-1", aIInsightId: "insight-1")
                    .ConfigureAwait(continueOnCapturedContext: false);
                Assert.AreEqual(expected: "insight-1", actual: result.AIInsightId);
                Assert.AreEqual(expected: "call-1", actual: result.CallId);
            }

            var request = getLastRequest() ?? throw new AssertFailedException("Expected an AI insight request.");
            Assert.AreEqual(expected: HttpMethod.Get, actual: request.Method);
            Assert.AreEqual(
                expected: new Uri("https://test.azure.com/connection/v1.0/copilot/me/onlineMeetings/meeting-1/aiInsights" + (list ? string.Empty : "/insight-1")),
                actual: request.RequestUri);
        }

        [TestMethod]
        [Owner("daviburg")]
        public void AIInsightCollectionResponse_ModelFactory_PreservesWireNames()
        {
            var model = TeamsModelFactory.AIInsightCollectionResponse(
                context: "https://graph.microsoft.com/metadata",
                aiInsights: new List<AIInsightResponse>
                {
                    TeamsModelFactory.AIInsightResponse(aiInsightId: "insight-1", callId: "call-1"),
                });
            var json = JsonSerializer.Serialize(model);
            using var document = JsonDocument.Parse(json);
            Assert.AreEqual(expected: "https://graph.microsoft.com/metadata", actual: document.RootElement.GetProperty("@odata.context").GetString());
            Assert.AreEqual(expected: "insight-1", actual: document.RootElement.GetProperty("value")[0].GetProperty("id").GetString());
            var restored = JsonSerializer.Deserialize<AIInsightCollectionResponse>(json);
            var insights = restored?.AIInsights;
            Assert.IsNotNull(insights);
            Assert.AreEqual(expected: 1, actual: insights.Count);
            Assert.AreEqual(expected: "insight-1", actual: insights[0].AIInsightId);
            Assert.AreEqual(expected: "call-1", actual: insights[0].CallId);
        }

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
            Assert.ThrowsExactly<ArgumentNullException>(() => new TeamsClient((string)null!));
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
            var client = new TeamsClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task GetAllTeamsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange — use JSON string so List<JsonElement?> deserializes with actual items
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"@odata.context\":\"https://graph.microsoft.com/beta/$metadata#teams\",\"value\":[{\"id\":\"1\"},{\"id\":\"2\"},{\"id\":\"3\"}]}")
            });

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
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"error\": \"Access denied\"}")
            });

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
                Id = "meeting-123",
                Subject = "Team Standup",
                Importance = "normal",
                IsOrganizer = true,
                HasAttachments = false,
                OnlineMeetingURL = "https://teams.microsoft.com/meet/123"
            };

            // Assert
            Assert.AreEqual("meeting-123", meeting.Id);
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
                Id = "channel-abc",
                DisplayName = "General",
                Description = "General discussion channel"
            };

            // Act
            var json = JsonSerializer.Serialize(channel);
            var deserialized = JsonSerializer.Deserialize<CreateChannelResponse>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(channel.Id, deserialized.Id);
            Assert.AreEqual(channel.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(channel.Description, deserialized.Description);
        }

        [TestMethod]
        public void AssociatedTeamInfo_JsonSerialization_RoundTrips()
        {
            // Arrange
            var team = new AssociatedTeamInfo
            {
                TeamId = "team-456",
                DisplayName = "Engineering",
                TenantId = "tenant-789"
            };

            // Act
            var json = JsonSerializer.Serialize(team);
            var deserialized = JsonSerializer.Deserialize<AssociatedTeamInfo>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(team.TeamId, deserialized.TeamId);
            Assert.AreEqual(team.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(team.TenantId, deserialized.TenantId);
        }

        [TestMethod]
        public async Task GetChannelAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"chan-123\",\"displayName\":\"General\",\"description\":\"General discussion\"}")
            });

            // Act
            var result = await client
                .GetChannelAsync(
                    team: "team-123",
                    channel: "chan-123",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("chan-123", result.ChannelId);
            Assert.AreEqual("General", result.DisplayName);
        }

        [TestMethod]
        public async Task GetChannelsForGroupAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"@odata.context\":\"https://graph.microsoft.com/beta\",\"value\":[{\"id\":\"chan-1\",\"displayName\":\"General\"}]}")
            });

            // Act
            var result = await client
                .GetChannelsForGroupAsync(
                    team: "team-123",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ChannelList);
            Assert.AreEqual(1, result.ChannelList.Count);
            Assert.AreEqual("chan-1", result.ChannelList[0].ChannelId);
        }

        [TestMethod]
        public async Task CreateTeamsMeetingAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"error\": \"Access denied\"}")
            });

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client.CreateTeamsMeetingAsync(
                        calendarId: "cal-123",
                        input: new NewMeeting(),
                        cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual((int)HttpStatusCode.Forbidden, exception.Status);
        }

        [TestMethod]
        public void ArchiveChannelInput_JsonSerialization_RoundTrips()
        {
            var input = new ArchiveChannelInput { SetSharePointSiteToReadOnlyForMembers = true };

            var json = JsonSerializer.Serialize(input);
            var deserialized = JsonSerializer.Deserialize<ArchiveChannelInput>(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(expected: true, actual: deserialized!.SetSharePointSiteToReadOnlyForMembers);
        }

        [TestMethod]
        public void Status_ExtensibleEnum_InProgress_RoundTrips()
        {
            var status = Status.InProgress;

            var json = JsonSerializer.Serialize(status);
            var deserialized = JsonSerializer.Deserialize<Status>(json);

            Assert.AreEqual(Status.InProgress, deserialized);
            Assert.AreEqual("inProgress", deserialized.ToString());
        }
    }
}
