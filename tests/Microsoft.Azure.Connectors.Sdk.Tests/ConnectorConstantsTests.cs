//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Linq;
using System.Reflection;
using Microsoft.Azure.Connectors.DirectClient.Office365;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
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
            Assert.AreEqual(ConnectorNames.Kusto, ConnectorNames.Kusto.ToLowerInvariant());
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
        public void Office365TriggerOperations_OnNewEmail_HasCorrectValue()
        {
            Assert.AreEqual("OnNewEmailV3", Office365TriggerOperations.OnNewEmail);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnUpcomingEvents_HasCorrectValue()
        {
            Assert.AreEqual("OnUpcomingEventsV3", Office365TriggerOperations.OnUpcomingEvents);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnFlaggedEmail_HasCorrectValue()
        {
            Assert.AreEqual("OnFlaggedEmailV4", Office365TriggerOperations.OnFlaggedEmail);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnNewEmailMentioningMe_HasCorrectValue()
        {
            Assert.AreEqual("OnNewMentionMeEmailV3", Office365TriggerOperations.OnNewEmailMentioningMe);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnSharedMailboxNewEmail_HasCorrectValue()
        {
            Assert.AreEqual("SharedMailboxOnNewEmailV2", Office365TriggerOperations.OnSharedMailboxNewEmail);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnCalendarNewItems_HasCorrectValue()
        {
            Assert.AreEqual("CalendarGetOnNewItemsV3", Office365TriggerOperations.OnCalendarNewItems);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnCalendarUpdatedItems_HasCorrectValue()
        {
            Assert.AreEqual("CalendarGetOnUpdatedItemsV3", Office365TriggerOperations.OnCalendarUpdatedItems);
        }

        [TestMethod]
        public void Office365TriggerOperations_OnCalendarChangedItems_HasCorrectValue()
        {
            Assert.AreEqual("CalendarGetOnChangedItemsV3", Office365TriggerOperations.OnCalendarChangedItems);
        }

        [TestMethod]
        public void Office365TriggerOperations_ConstantsUsableInSwitch()
        {
            // Verify constants are compile-time constants usable in switch statements
            var operationName = Office365TriggerOperations.OnNewEmail;
            string result = operationName switch
            {
                Office365TriggerOperations.OnNewEmail => "email",
                Office365TriggerOperations.OnUpcomingEvents => "calendar",
                Office365TriggerOperations.OnCalendarChangedItems => "calendar-changed",
                _ => "unknown",
            };

            Assert.AreEqual("email", result);
        }
    }
}
