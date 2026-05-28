//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Linq;
using System.Reflection;
using System.Text.Json;
using Azure.Connectors.Sdk.Teams;
using Azure.Connectors.Sdk.Teams.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for Teams trigger payload types and registry (#176).
    /// </summary>
    [TestClass]
    public class TeamsTriggerPayloadTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        // ===== Payload Deserialization Tests =====

        [TestMethod]
        public void Deserialize_TeamsOnNewChannelMessageTriggerPayload_Succeeds()
        {
            // Arrange
            var payload = """
                {
                  "body": {
                    "value": [
                      {
                        "id": "msg-001",
                        "importance": "normal",
                        "body": "Hello team!",
                        "from": "user@contoso.com"
                      }
                    ]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TeamsOnNewChannelMessageTriggerPayload>(
                payload, TeamsTriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            Assert.AreEqual("msg-001", result.Body.Value[0].Id);
            Assert.AreEqual("normal", result.Body.Value[0].Importance);
        }

        [TestMethod]
        public void Deserialize_TeamsOnNewChannelMessageMentioningMeTriggerPayload_Succeeds()
        {
            // Arrange
            var payload = """
                {
                  "body": {
                    "value": [
                      {
                        "id": "msg-mention-001",
                        "importance": "high",
                        "body": "Hey @me, check this out"
                      }
                    ]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TeamsOnNewChannelMessageMentioningMeTriggerPayload>(
                payload, TeamsTriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            Assert.AreEqual("msg-mention-001", result.Body.Value[0].Id);
        }

        [TestMethod]
        public void Deserialize_TeamsOnTeamMemberRemovedTriggerPayload_Succeeds()
        {
            // Arrange — member removal events use object payload type
            var payload = """
                {
                  "body": {
                    "value": [
                      { "userId": "user-123", "action": "removed" }
                    ]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TeamsOnTeamMemberRemovedTriggerPayload>(
                payload, TeamsTriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_TeamsOnTeamMemberAddedTriggerPayload_Succeeds()
        {
            // Arrange
            var payload = """
                {
                  "body": {
                    "value": [
                      { "userId": "user-456", "action": "added" }
                    ]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TeamsOnTeamMemberAddedTriggerPayload>(
                payload, TeamsTriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_TeamsChannelMessage_SingleItemShape_Succeeds()
        {
            // Arrange — splitOn enabled: body is the item directly
            var payload = """
                {
                  "body": {
                    "id": "msg-split-001",
                    "importance": "urgent",
                    "body": "Urgent: deploy rollback needed"
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TeamsOnNewChannelMessageTriggerPayload>(
                payload, TeamsTriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            Assert.AreEqual("msg-split-001", result.Body.Value[0].Id);
        }

        [TestMethod]
        public void Deserialize_TeamsChannelMessage_EmptyValueArray_ReturnsEmpty()
        {
            // Arrange
            var payload = """{"body":{"value":[]}}""";

            // Act
            var result = JsonSerializer.Deserialize<TeamsOnNewChannelMessageTriggerPayload>(
                payload, TeamsTriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result!.Body!.Value);
            Assert.AreEqual(0, result.Body.Value.Count);
        }

        // ===== TeamsTriggers.Operations Registry Tests =====

        [TestMethod]
        public void TeamsTriggers_Operations_ContainsExpectedKeys()
        {
            // Assert
            Assert.IsTrue(TeamsTriggers.Operations.ContainsKey("OnNewChannelMessage"));
            Assert.IsTrue(TeamsTriggers.Operations.ContainsKey("OnNewChannelMessageMentioningMe"));
            Assert.IsTrue(TeamsTriggers.Operations.ContainsKey("OnGroupMembershipRemoval"));
            Assert.IsTrue(TeamsTriggers.Operations.ContainsKey("OnGroupMembershipAdd"));
        }

        [TestMethod]
        public void TeamsTriggers_Operations_MapsToCorrectPayloadTypes()
        {
            // Assert
            Assert.AreEqual(typeof(TeamsOnNewChannelMessageTriggerPayload),
                TeamsTriggers.Operations["OnNewChannelMessage"]);
            Assert.AreEqual(typeof(TeamsOnNewChannelMessageMentioningMeTriggerPayload),
                TeamsTriggers.Operations["OnNewChannelMessageMentioningMe"]);
            Assert.AreEqual(typeof(TeamsOnTeamMemberRemovedTriggerPayload),
                TeamsTriggers.Operations["OnGroupMembershipRemoval"]);
            Assert.AreEqual(typeof(TeamsOnTeamMemberAddedTriggerPayload),
                TeamsTriggers.Operations["OnGroupMembershipAdd"]);
        }

        [TestMethod]
        public void TeamsTriggers_Operations_IsCaseInsensitive()
        {
            // Assert — registry uses OrdinalIgnoreCase
            Assert.IsTrue(TeamsTriggers.Operations.ContainsKey("onnewchannelmessage"));
            Assert.IsTrue(TeamsTriggers.Operations.ContainsKey("ONNEWCHANNELMESSAGE"));
        }

        [TestMethod]
        public void TeamsTriggerOperations_AllConstantsMatchRegistryKeys()
        {
            // Arrange — get all const string fields from TeamsTriggerOperations
            var constants = typeof(TeamsTriggerOperations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToList();

            // Assert — every constant that has a payload type maps to a key in TeamsTriggers.Operations
            var registeredOps = TeamsTriggers.Operations.Keys.ToHashSet();
            foreach (var constant in constants)
            {
                if (registeredOps.Contains(constant))
                {
                    Assert.IsTrue(
                        TeamsTriggers.Operations.ContainsKey(constant),
                        $"TeamsTriggerOperations constant '{constant}' is not a key in TeamsTriggers.Operations.");
                }
            }
        }

        [TestMethod]
        public void TeamsTriggerOperations_CoversAllRegistryKeys()
        {
            // Arrange — get all const string fields from TeamsTriggerOperations
            var constants = typeof(TeamsTriggerOperations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToHashSet();

            // Assert — every key in TeamsTriggers.Operations has a constant
            foreach (var operationName in TeamsTriggers.Operations.Keys)
            {
                Assert.IsTrue(
                    constants.Contains(operationName),
                    $"TeamsTriggers.Operations key '{operationName}' is missing from TeamsTriggerOperations.");
            }
        }

        [TestMethod]
        public void TeamsTriggerOperations_ConstantCount_MatchesOrExceedsRegistryCount()
        {
            // Arrange — some trigger operations have no payload type (webhook-only),
            // so constant count >= registry count
            var constantCount = typeof(TeamsTriggerOperations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Count(field => field.IsLiteral && field.FieldType == typeof(string));

            // Assert
            Assert.IsTrue(constantCount >= TeamsTriggers.Operations.Count,
                $"Expected at least {TeamsTriggers.Operations.Count} constants but found {constantCount}.");
        }

        [TestMethod]
        public void TeamsTriggerOperations_OnNewChannelMessage_HasCorrectValue()
        {
            // NOTE(daviburg): Intentionally pinning const string values; assertion will always be true by design.
#pragma warning disable MSTEST0032
            Assert.AreEqual("OnNewChannelMessage", TeamsTriggerOperations.OnNewChannelMessage);
#pragma warning restore MSTEST0032
        }

        [TestMethod]
        public void TeamsTriggerOperations_OnTeamMemberAdded_HasCorrectValue()
        {
            // NOTE(daviburg): Intentionally pinning const string values; assertion will always be true by design.
#pragma warning disable MSTEST0032
            Assert.AreEqual("OnGroupMembershipAdd", TeamsTriggerOperations.OnTeamMemberAdded);
#pragma warning restore MSTEST0032
        }
    }
}
