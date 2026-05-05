//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Connectors.Sdk.Teams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for <see cref="DynamicSchemaAttribute"/> and <c>[JsonExtensionData]</c> on dynamic schema types.
    /// </summary>
    [TestClass]
    public class DynamicSchemaTests
    {
        [TestMethod]
        public void DynamicSchemaAttribute_WithValidOperationId_StoresId()
        {
            // Arrange & Act
            var attribute = new DynamicSchemaAttribute("GetMessageSchema");

            // Assert
            Assert.AreEqual("GetMessageSchema", attribute.OperationId);
        }

        [TestMethod]
        public void DynamicSchemaAttribute_WithNullOperationId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new DynamicSchemaAttribute(operationId: null!));
        }

        [TestMethod]
        public void DynamicSchemaAttribute_WithEmptyOperationId_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new DynamicSchemaAttribute(operationId: string.Empty));
        }

        [TestMethod]
        public void DynamicSchemaType_SerializesAdditionalProperties()
        {
            // Arrange
            var request = new DynamicPostMessageRequest
            {
                AdditionalProperties = new Dictionary<string, JsonElement>
                {
                    ["recipient"] = JsonSerializer.SerializeToElement(new { groupId = "team-123", channelId = "channel-456" }),
                    ["messageBody"] = JsonSerializer.SerializeToElement("<p>Hello from SDK</p>"),
                }
            };

            // Act
            var json = JsonSerializer.Serialize(request);

            // Assert
            Assert.IsTrue(json.Contains("recipient", StringComparison.Ordinal), message: "Serialized JSON should contain the dynamic 'recipient' property.");
            Assert.IsTrue(json.Contains("messageBody", StringComparison.Ordinal), message: "Serialized JSON should contain the dynamic 'messageBody' property.");
            Assert.IsTrue(json.Contains("team-123", StringComparison.Ordinal), message: "Serialized JSON should contain nested values.");
        }

        [TestMethod]
        public void DynamicSchemaType_DeserializesArbitraryProperties()
        {
            // Arrange
            var json = """{"subject":"Test","body":"Hello","importance":"normal"}""";

            // Act
            var result = JsonSerializer.Deserialize<DynamicPostMessageRequest>(json);

            // Assert
            Assert.IsNotNull(result!.AdditionalProperties, message: "AdditionalProperties should capture deserialized dynamic properties.");
            Assert.AreEqual(3, result.AdditionalProperties!.Count, message: "All three properties should be captured.");
            Assert.AreEqual("Test", result.AdditionalProperties["subject"].GetString());
            Assert.AreEqual("Hello", result.AdditionalProperties["body"].GetString());
        }

        [TestMethod]
        public void DynamicSchemaType_RoundTripsAllData()
        {
            // Arrange
            var original = """{"subject":"Test","body":{"contentType":"html","content":"<p>Hello</p>"},"importance":"normal"}""";

            // Act
            var deserialized = JsonSerializer.Deserialize<DynamicPostMessageRequest>(original);
            var reserialized = JsonSerializer.Serialize(deserialized);

            // Assert — verify no data is lost during round-trip
            using var originalDoc = JsonDocument.Parse(original);
            using var reserializedDoc = JsonDocument.Parse(reserialized);

            Assert.AreEqual(
                originalDoc.RootElement.GetProperty("subject").GetString(),
                reserializedDoc.RootElement.GetProperty("subject").GetString(),
                message: "Subject should survive round-trip.");
            Assert.AreEqual(
                originalDoc.RootElement.GetProperty("importance").GetString(),
                reserializedDoc.RootElement.GetProperty("importance").GetString(),
                message: "Importance should survive round-trip.");
            Assert.IsTrue(
                reserializedDoc.RootElement.TryGetProperty("body", out _),
                message: "Nested body object should survive round-trip.");
        }

        [TestMethod]
        public void DynamicSchemaType_HasDynamicSchemaAttribute()
        {
            // Arrange & Act
            var attribute = typeof(DynamicPostMessageRequest)
                .GetCustomAttribute<DynamicSchemaAttribute>();

            // Assert
            Assert.IsNotNull(attribute, message: "DynamicPostMessageRequest should have [DynamicSchema] attribute.");
            Assert.IsFalse(
                string.IsNullOrEmpty(attribute.OperationId),
                message: "DynamicSchema attribute should have a non-empty operationId.");
        }

        [TestMethod]
        public void DynamicSchemaType_HasJsonExtensionDataProperty()
        {
            // Arrange & Act
            var property = typeof(DynamicPostMessageRequest).GetProperty("AdditionalProperties");

            // Assert
            Assert.IsNotNull(property, message: "DynamicPostMessageRequest should have an AdditionalProperties property.");

            var extensionDataAttr = property.GetCustomAttribute<JsonExtensionDataAttribute>();
            Assert.IsNotNull(extensionDataAttr, message: "AdditionalProperties should be annotated with [JsonExtensionData].");
        }
    }
}
