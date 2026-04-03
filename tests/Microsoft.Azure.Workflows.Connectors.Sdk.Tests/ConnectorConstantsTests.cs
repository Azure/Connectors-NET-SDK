//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Tests
{
    using System.Linq;
    using System.Reflection;
    using Microsoft.Azure.Connectors.DirectClient.Office365;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for <see cref="ConnectorNames"/> and <see cref="Office365TriggerOperations"/> constants.
    /// Verifies that the string constants are consistent with the existing generated types and registries.
    /// </summary>
    [TestClass]
    public class ConnectorConstantsTests
    {
        // ===== ConnectorNames Tests =====

        [TestMethod]
        public void ConnectorNames_Office365_MatchesRegisteredConnector()
        {
            // Assert — constant matches the string used in DirectClientConnectors.AvailableConnectors
            Assert.AreEqual("office365", ConnectorNames.Office365);
            CollectionAssert.Contains(
                Microsoft.Azure.Connectors.DirectClient.DirectClientConnectors.AvailableConnectors,
                ConnectorNames.Office365);
        }

        [TestMethod]
        public void ConnectorNames_SharePointOnline_MatchesRegisteredConnector()
        {
            Assert.AreEqual("sharepointonline", ConnectorNames.Sharepointonline);
            CollectionAssert.Contains(
                Microsoft.Azure.Connectors.DirectClient.DirectClientConnectors.AvailableConnectors,
                ConnectorNames.Sharepointonline);
        }

        [TestMethod]
        public void ConnectorNames_Teams_MatchesRegisteredConnector()
        {
            Assert.AreEqual("teams", ConnectorNames.Teams);
            CollectionAssert.Contains(
                Microsoft.Azure.Connectors.DirectClient.DirectClientConnectors.AvailableConnectors,
                ConnectorNames.Teams);
        }

        [TestMethod]
        public void ConnectorNames_CoversAllRegisteredConnectors()
        {
            // Arrange — get all const string fields from ConnectorNames
            var constants = typeof(ConnectorNames)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToHashSet();

            // Assert — every registered connector has a constant
            foreach (var connector in Microsoft.Azure.Connectors.DirectClient.DirectClientConnectors.AvailableConnectors)
            {
                Assert.IsTrue(
                    constants.Contains(connector),
                    $"Connector '{connector}' is in DirectClientConnectors.AvailableConnectors but missing from ConnectorNames.");
            }
        }

        [TestMethod]
        public void ConnectorNames_AllConstantsAreRegistered()
        {
            // Arrange — get all const string fields from ConnectorNames
            var constants = typeof(ConnectorNames)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToList();

            var registered = Microsoft.Azure.Connectors.DirectClient.DirectClientConnectors.AvailableConnectors;

            // Assert — every constant in ConnectorNames maps to a registered connector
            foreach (var constant in constants)
            {
                CollectionAssert.Contains(
                    registered,
                    constant,
                    $"ConnectorNames has constant '{constant}' but it's not in DirectClientConnectors.AvailableConnectors.");
            }
        }

        [TestMethod]
        public void ConnectorNames_ValuesAreLowercase()
        {
            // Assert — connector API names are lowercase identifiers
            Assert.AreEqual(ConnectorNames.Office365, ConnectorNames.Office365.ToLowerInvariant());
            Assert.AreEqual(ConnectorNames.Sharepointonline, ConnectorNames.Sharepointonline.ToLowerInvariant());
            Assert.AreEqual(ConnectorNames.Teams, ConnectorNames.Teams.ToLowerInvariant());
        }

        // ===== Office365TriggerOperations Tests =====

        [TestMethod]
        public void Office365TriggerOperations_AllConstantsMatchRegistryKeys()
        {
            // Arrange — get all const string fields from Office365TriggerOperations
            var constants = typeof(Office365TriggerOperations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToList();

            // Assert — every constant maps to a key in Office365Triggers.Operations
            foreach (var constant in constants)
            {
                Assert.IsTrue(
                    Office365Triggers.Operations.ContainsKey(constant),
                    $"Office365TriggerOperations constant '{constant}' is not a key in Office365Triggers.Operations.");
            }
        }

        [TestMethod]
        public void Office365TriggerOperations_CoversAllRegistryKeys()
        {
            // Arrange — get all const string fields from Office365TriggerOperations
            var constants = typeof(Office365TriggerOperations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .ToHashSet();

            // Assert — every key in Office365Triggers.Operations has a constant
            foreach (var operationName in Office365Triggers.Operations.Keys)
            {
                Assert.IsTrue(
                    constants.Contains(operationName),
                    $"Office365Triggers.Operations key '{operationName}' is missing from Office365TriggerOperations.");
            }
        }

        [TestMethod]
        public void Office365TriggerOperations_ConstantCount_MatchesRegistryCount()
        {
            // Arrange
            var constantCount = typeof(Office365TriggerOperations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Count(field => field.IsLiteral && field.FieldType == typeof(string));

            // Assert — constant count matches the registry count
            Assert.AreEqual(Office365Triggers.Operations.Count, constantCount);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnNewEmailV3_HasCorrectValue()
        {
            Assert.AreEqual("OnNewEmailV3", Office365TriggerOperations.OnNewEmailV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnUpcomingEventsV3_HasCorrectValue()
        {
            Assert.AreEqual("OnUpcomingEventsV3", Office365TriggerOperations.OnUpcomingEventsV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnFlaggedEmailV3_HasCorrectValue()
        {
            Assert.AreEqual("OnFlaggedEmailV3", Office365TriggerOperations.OnFlaggedEmailV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnFlaggedEmailV4_HasCorrectValue()
        {
            Assert.AreEqual("OnFlaggedEmailV4", Office365TriggerOperations.OnFlaggedEmailV4);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnNewMentionMeEmailV3_HasCorrectValue()
        {
            Assert.AreEqual("OnNewMentionMeEmailV3", Office365TriggerOperations.OnNewMentionMeEmailV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_SharedMailboxOnNewEmailV2_HasCorrectValue()
        {
            Assert.AreEqual("SharedMailboxOnNewEmailV2", Office365TriggerOperations.SharedMailboxOnNewEmailV2);
        }

        [TestMethod]
        public void Office365TriggerOperations_CalendarGetOnNewItemsV3_HasCorrectValue()
        {
            Assert.AreEqual("CalendarGetOnNewItemsV3", Office365TriggerOperations.CalendarGetOnNewItemsV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_CalendarGetOnUpdatedItemsV3_HasCorrectValue()
        {
            Assert.AreEqual("CalendarGetOnUpdatedItemsV3", Office365TriggerOperations.CalendarGetOnUpdatedItemsV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_CalendarGetOnChangedItemsV3_HasCorrectValue()
        {
            Assert.AreEqual("CalendarGetOnChangedItemsV3", Office365TriggerOperations.CalendarGetOnChangedItemsV3);
        }

        [TestMethod]
        public void Office365TriggerOperations_ConstantsUsableInSwitch()
        {
            // Verify constants are compile-time constants usable in switch statements
            var operationName = Office365TriggerOperations.OnNewEmailV3;
            string result = operationName switch
            {
                Office365TriggerOperations.OnNewEmailV3 => "email",
                Office365TriggerOperations.OnUpcomingEventsV3 => "calendar",
                Office365TriggerOperations.CalendarGetOnChangedItemsV3 => "calendar-changed",
                _ => "unknown",
            };

            Assert.AreEqual("email", result);
        }
    }
}
