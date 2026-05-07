//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Text.Json;
using Microsoft.Azure.Connectors.Sdk.Azureblob;
using Microsoft.Azure.Connectors.Sdk.Office365;
using Microsoft.Azure.Connectors.Sdk.Sharepointonline;
using Microsoft.Azure.Connectors.Sdk.Teams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests that verify model factories can construct instances with output-only properties.
    /// Validates issue #106: internal set + model factories for mocking.
    /// </summary>
    [TestClass]
    public class ModelFactoryTests
    {
        [TestMethod]
        public void Office365ModelFactory_OutlookReceiveMessage_SetsAllProperties()
        {
            // Arrange
            var created = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
            var received = new DateTime(2025, 6, 15, 10, 0, 5, DateTimeKind.Utc);

            // Act
            var message = Office365ModelFactory.OutlookReceiveMessage(
                id: "msg-123",
                subject: "Test Subject",
                createdDateTime: created,
                receivedDateTime: received,
                lastModifiedDateTime: received,
                hasAttachments: false,
                isRead: true,
                importance: "normal");

            // Assert
            Assert.AreEqual("msg-123", message.Id);
            Assert.AreEqual("Test Subject", message.Subject);
            Assert.AreEqual(created, message.CreatedDateTime);
            Assert.AreEqual(received, message.ReceivedDateTime);
            Assert.AreEqual(received, message.LastModifiedDateTime);
            Assert.AreEqual(false, message.HasAttachments);
            Assert.AreEqual(true, message.IsRead);
            Assert.AreEqual("normal", message.Importance);
        }

        [TestMethod]
        public void Office365ModelFactory_GraphOutlookCategory_SetsIdAndDisplayName()
        {
            // Act
            var category = Office365ModelFactory.GraphOutlookCategory(
                id: "cat-1",
                displayName: "Red Category");

            // Assert
            Assert.AreEqual("cat-1", category.Id);
            Assert.AreEqual("Red Category", category.DisplayName);
        }

        [TestMethod]
        public void Office365ModelFactory_GraphCalendarEvent_SetsOutputOnlyProperties()
        {
            // Arrange
            var created = new DateTime(2025, 1, 15, 8, 0, 0, DateTimeKind.Utc);
            var modified = new DateTime(2025, 1, 15, 9, 0, 0, DateTimeKind.Utc);

            // Act
            var calendarEvent = Office365ModelFactory.GraphCalendarEventClientReceive(
                id: "event-456",
                subject: "Team Standup",
                createdTime: created,
                lastModifiedTime: modified,
                startTime: "2025-01-15T10:00:00Z",
                endTime: "2025-01-15T10:30:00Z",
                organizer: "manager@test.com",
                location: "Conference Room B");

            // Assert
            Assert.AreEqual("event-456", calendarEvent.Id);
            Assert.AreEqual("Team Standup", calendarEvent.Subject);
            Assert.AreEqual(created, calendarEvent.CreatedTime);
            Assert.AreEqual(modified, calendarEvent.LastModifiedTime);
            Assert.AreEqual("2025-01-15T10:00:00Z", calendarEvent.StartTime);
            Assert.AreEqual("Conference Room B", calendarEvent.Location);
        }

        [TestMethod]
        public void TeamsModelFactory_GetChannelResponse_SetsOutputOnlyProperties()
        {
            // Arrange
            var creationTime = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var channel = TeamsModelFactory.GetChannelResponse(
                channelID: "channel-789",
                displayName: "General",
                channelCreationTime: creationTime);

            // Assert
            Assert.AreEqual("channel-789", channel.ChannelID);
            Assert.AreEqual("General", channel.DisplayName);
            Assert.AreEqual(creationTime, channel.ChannelCreationTime);
        }

        [TestMethod]
        public void AzureblobModelFactory_BlobMetadata_SetsOutputOnlyProperties()
        {
            // Arrange
            var lastModified = new DateTime(2025, 5, 20, 14, 30, 0, DateTimeKind.Utc);

            // Act
            var blob = AzureblobModelFactory.BlobMetadata(
                id: "/container/test-blob.txt",
                name: "test-blob.txt",
                eTag: "\"0x8D9F3A4B5C6D7E8\"",
                lastModified: lastModified,
                path: "/container/test-blob.txt");

            // Assert
            Assert.AreEqual("/container/test-blob.txt", blob.Id);
            Assert.AreEqual("test-blob.txt", blob.Name);
            Assert.AreEqual("\"0x8D9F3A4B5C6D7E8\"", blob.ETag);
            Assert.AreEqual(lastModified, blob.LastModified);
        }

        [TestMethod]
        public void SharepointonlineModelFactory_BlobMetadata_SetsETagAndLastModified()
        {
            // Arrange
            var lastModified = new DateTime(2025, 4, 10, 8, 0, 0, DateTimeKind.Utc);

            // Act
            var blob = SharepointonlineModelFactory.BlobMetadata(
                id: "/sites/test/file.docx",
                eTag: "\"etag-value\"",
                lastModified: lastModified);

            // Assert
            Assert.AreEqual("/sites/test/file.docx", blob.Id);
            Assert.AreEqual("\"etag-value\"", blob.ETag);
            Assert.AreEqual(lastModified, blob.LastModified);
        }

        [TestMethod]
        public void Office365ModelFactory_DefaultParameters_CreateEmptyInstance()
        {
            // Act — call with no arguments, all default
            var message = Office365ModelFactory.OutlookReceiveMessage();

            // Assert — everything is default
            Assert.IsNull(message.Id);
            Assert.IsNull(message.Subject);
            Assert.IsNull(message.CreatedDateTime);
            Assert.IsNull(message.ReceivedDateTime);
            Assert.IsNull(message.HasAttachments);
        }

        [TestMethod]
        public void ModelFactory_OutputOnlyProperty_RoundTripsViaJson()
        {
            // Arrange — construct with factory (sets internal setter)
            var original = Office365ModelFactory.GraphOutlookCategory(
                id: "cat-roundtrip",
                displayName: "Test Roundtrip");

            // Act — serialize to JSON, then deserialize back
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<GraphOutlookCategory>(json);

            // Assert — deserialized instance has same values
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("cat-roundtrip", deserialized.Id);
            Assert.AreEqual("Test Roundtrip", deserialized.DisplayName);
        }

        [TestMethod]
        public void TeamsModelFactory_CreateATeamResponse_SetsNewTeamID()
        {
            // Act
            var response = TeamsModelFactory.CreateATeamResponse(
                newTeamID: "team-new-123");

            // Assert
            Assert.AreEqual("team-new-123", response.NewTeamID);
        }

        [TestMethod]
        public void TeamsModelFactory_NewMeetingResponse_SetsTimestamps()
        {
            // Act
            var meeting = TeamsModelFactory.NewMeetingResponse(
                iD: "meeting-001",
                createdTimestamp: "2025-06-01T10:00:00Z",
                lastModifiedTimestamp: "2025-06-01T10:05:00Z");

            // Assert
            Assert.AreEqual("meeting-001", meeting.ID);
            Assert.AreEqual("2025-06-01T10:00:00Z", meeting.CreatedTimestamp);
            Assert.AreEqual("2025-06-01T10:05:00Z", meeting.LastModifiedTimestamp);
        }
    }
}
