//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Office365.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for trigger callback payload deserialization.
    /// </summary>
    [TestClass]
    public class TriggerCallbackPayloadTests
    {
        /// <summary>
        /// Captured Connector Gateway trigger callback payload from production (2026-03-16).
        /// </summary>
        private const string CapturedTriggerPayload = """
            {
              "body": {
                "value": [
                  {
                    "id": "AAMkADlmOTA3NWNm",
                    "receivedDateTime": "2026-03-16T21:26:21+00:00",
                    "hasAttachments": false,
                    "subject": "Test email for trigger callback",
                    "bodyPreview": "This is a test email preview.",
                    "importance": "normal",
                    "isRead": false,
                    "isHtml": true,
                    "body": "<html><body>Hello</body></html>",
                    "from": "sender@microsoft.com",
                    "toRecipients": "recipient1@microsoft.com;recipient2@microsoft.com",
                    "ccRecipients": "cc@microsoft.com",
                    "bccRecipients": null,
                    "replyTo": null,
                    "attachments": []
                  }
                ]
              }
            }
            """;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [TestMethod]
        public void Deserialize_CapturedPayload_ReturnsEnvelopeWithOneEmail()
        {
            // Arrange & Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                TriggerCallbackPayloadTests.CapturedTriggerPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_CapturedPayload_ParsesEmailFields()
        {
            // Arrange & Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                TriggerCallbackPayloadTests.CapturedTriggerPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.IsTrue(result.Body.Value.Count > 0, "Expected at least one email in the value array.");
            var email = result.Body.Value[0];

            // Assert
            Assert.AreEqual("AAMkADlmOTA3NWNm", email.MessageId);
            Assert.AreEqual("Test email for trigger callback", email.Subject);
            Assert.AreEqual("sender@microsoft.com", email.From);
            Assert.AreEqual("recipient1@microsoft.com;recipient2@microsoft.com", email.To);
            Assert.AreEqual("cc@microsoft.com", email.CC);
            Assert.AreEqual("normal", email.Importance);
            Assert.AreEqual("This is a test email preview.", email.BodyPreview);
            Assert.AreEqual(false, email.HasAttachment);
            Assert.AreEqual(false, email.IsRead);
            Assert.AreEqual(true, email.IsHTML);
            Assert.AreEqual("<html><body>Hello</body></html>", email.Body);
        }

        [TestMethod]
        public void Deserialize_MultipleEmails_ParsesAll()
        {
            // Arrange
            var multiPayload = """
                {
                  "body": {
                    "value": [
                      { "subject": "Email 1", "from": "a@test.com" },
                      { "subject": "Email 2", "from": "b@test.com" },
                      { "subject": "Email 3", "from": "c@test.com" }
                    ]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                multiPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.AreEqual(3, result!.Body!.Value!.Count);
            Assert.AreEqual("Email 1", result.Body.Value[0].Subject);
            Assert.AreEqual("Email 3", result.Body.Value[2].Subject);
        }

        [TestMethod]
        public void Deserialize_EmptyValueArray_ReturnsEmptyList()
        {
            // Arrange
            var emptyPayload = """{"body":{"value":[]}}""";

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                emptyPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result!.Body!.Value);
            Assert.AreEqual(0, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_NullBody_ReturnsNullBody()
        {
            // Arrange
            var nullBodyPayload = """{"body":null}""";

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                nullBodyPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.IsNull(result!.Body);
        }

        [TestMethod]
        public void Deserialize_WithAttachments_ParsesAttachmentMetadata()
        {
            // Arrange
            var payload = """
                {
                  "body": {
                    "value": [
                      {
                        "subject": "Email with attachment",
                        "hasAttachments": true,
                        "attachments": [
                          {
                            "@odata.type": "#microsoft.graph.fileAttachment",
                            "name": "document.pdf",
                            "contentType": "application/pdf",
                            "size": 12345
                          }
                        ]
                      }
                    ]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                payload,
                TriggerCallbackPayloadTests.JsonOptions);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.IsTrue(result.Body.Value.Count > 0, "Expected at least one email in the value array.");
            var email = result.Body.Value[0];

            // Assert
            Assert.AreEqual(true, email.HasAttachment);
            Assert.IsNotNull(email.Attachments);
            Assert.AreEqual(1, email.Attachments.Count);
            Assert.AreEqual("document.pdf", email.Attachments[0].Name);
            Assert.AreEqual("application/pdf", email.Attachments[0].ContentType);
            Assert.AreEqual(12345L, email.Attachments[0].Size);
        }

        [TestMethod]
        public void Deserialize_GenericType_WorksWithDictionary()
        {
            // Arrange — demonstrate that the envelope works with arbitrary types
            var payload = """{"body":{"value":[{"key":"value1"},{"key":"value2"}]}}""";

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<Dictionary<string, string>>>(
                payload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.AreEqual(2, result!.Body!.Value!.Count);
            Assert.AreEqual("value1", result.Body.Value[0]["key"]);
            Assert.AreEqual("value2", result.Body.Value[1]["key"]);
        }

        #region Single-item shape (splitOn enabled)

        /// <summary>
        /// Captured single-item callback from production when splitOn is enabled on the trigger config.
        /// The body contains the item directly instead of a {"value":[...]} array.
        /// </summary>
        private const string SingleItemTriggerPayload = """
            {
              "body": {
                "id": "AAMkADlmOTA3NWNm",
                "receivedDateTime": "2026-05-14T16:06:00+00:00",
                "hasAttachments": false,
                "subject": "Single-item callback test",
                "bodyPreview": "This is a single-item callback.",
                "importance": "normal",
                "isRead": false,
                "isHtml": true,
                "body": "<html><body>Single item</body></html>",
                "from": "sender@microsoft.com",
                "toRecipients": "recipient@microsoft.com",
                "ccRecipients": null,
                "bccRecipients": null,
                "replyTo": null,
                "attachments": []
              }
            }
            """;

        [TestMethod]
        public void Deserialize_SingleItemShape_WrapsInList()
        {
            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                TriggerCallbackPayloadTests.SingleItemTriggerPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count, "Single-item shape should be wrapped in a one-element list.");
        }

        [TestMethod]
        public void Deserialize_SingleItemShape_ParsesEmailFields()
        {
            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                TriggerCallbackPayloadTests.SingleItemTriggerPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            var email = result!.Body!.Value![0];

            // Assert
            Assert.AreEqual("AAMkADlmOTA3NWNm", email.MessageId);
            Assert.AreEqual("Single-item callback test", email.Subject);
            Assert.AreEqual("sender@microsoft.com", email.From);
            Assert.AreEqual("recipient@microsoft.com", email.To);
            Assert.AreEqual("normal", email.Importance);
            Assert.AreEqual(false, email.HasAttachment);
        }

        [TestMethod]
        public void Deserialize_SingleItemShape_WithTypedAlias()
        {
            // Act — use the generated typed alias
            var result = JsonSerializer.Deserialize<Office365OnNewEmailTriggerPayload>(
                TriggerCallbackPayloadTests.SingleItemTriggerPayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Body!.Value!.Count);
            Assert.AreEqual("Single-item callback test", result.Body.Value[0].Subject);
        }

        [TestMethod]
        public void Deserialize_SingleItemShape_GenericDictionary()
        {
            // Arrange — single-item shape with a dictionary type
            var payload = """{"body":{"key1":"value1","key2":"value2"}}""";

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<Dictionary<string, string>>>(
                payload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert
            Assert.AreEqual(1, result!.Body!.Value!.Count);
            Assert.AreEqual("value1", result.Body.Value[0]["key1"]);
            Assert.AreEqual("value2", result.Body.Value[0]["key2"]);
        }

        [TestMethod]
        public void Deserialize_BothShapes_ProduceIdenticalResult()
        {
            // Arrange
            var batchPayload = """
                {
                  "body": {
                    "value": [
                      { "subject": "Test email", "from": "sender@test.com" }
                    ]
                  }
                }
                """;

            var singlePayload = """
                {
                  "body": {
                    "subject": "Test email",
                    "from": "sender@test.com"
                  }
                }
                """;

            // Act
            var batchResult = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                batchPayload,
                TriggerCallbackPayloadTests.JsonOptions);
            var singleResult = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                singlePayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert — both produce identical Value lists
            Assert.AreEqual(batchResult!.Body!.Value!.Count, singleResult!.Body!.Value!.Count);
            Assert.AreEqual(batchResult.Body.Value[0].Subject, singleResult.Body.Value[0].Subject);
            Assert.AreEqual(batchResult.Body.Value[0].From, singleResult.Body.Value[0].From);
        }

        [TestMethod]
        public void Serialize_RoundTrip_AlwaysProducesBatchShape()
        {
            // Arrange — deserialize from single-item shape
            var singlePayload = """{"body":{"subject":"Test email","from":"sender@test.com"}}""";
            var deserialized = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                singlePayload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Act — serialize back
            var json = JsonSerializer.Serialize(deserialized, TriggerCallbackPayloadTests.JsonOptions);

            // Assert — output always uses batch shape with "value" array
            Assert.IsTrue(json.Contains("\"value\"", StringComparison.Ordinal), "Serialized output should contain 'value' array.");
            Assert.IsTrue(json.Contains("[", StringComparison.Ordinal), "Serialized output should contain an array.");

            // Round-trip: re-deserialize produces same result
            var roundTripped = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                json,
                TriggerCallbackPayloadTests.JsonOptions);
            Assert.AreEqual(1, roundTripped!.Body!.Value!.Count);
            Assert.AreEqual("Test email", roundTripped.Body.Value[0].Subject);
        }

        #endregion Single-item shape (splitOn enabled)

        #region Discriminator correctness

        [TestMethod]
        public void Deserialize_SingleItemWithValueArrayProperty_IsNotMistakenForBatch()
        {
            // Arrange — single-item T that has a "value" array field PLUS other fields.
            // The discriminator must NOT misclassify multi-property objects as batch envelopes
            // just because one field happens to be named "value" and is an array.
            var payload = """
                {
                  "body": {
                    "subject": "Item with value field",
                    "from": "sender@test.com",
                    "value": ["extra", "data"]
                  }
                }
                """;

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                payload,
                TriggerCallbackPayloadTests.JsonOptions);

            // Assert — treated as single-item because the body has more than one property,
            // so the full object is deserialized as T and wrapped in a one-element list
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Body!.Value!.Count, "Multi-property body must be treated as single-item, not batch.");
            Assert.AreEqual("Item with value field", result.Body.Value[0].Subject);
        }

        [TestMethod]
        public void Deserialize_CaseInsensitiveOptions_BatchShapeWithUppercaseValueRecognized()
        {
            // Arrange — batch payload using "Value" (capital V) wire name.
            // With PropertyNameCaseInsensitive the discriminator must find it.
            var payload = """{"body":{"Value":[{"subject":"Email 1"},{"subject":"Email 2"}]}}""";
            var caseInsensitiveOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                payload,
                caseInsensitiveOptions);

            // Assert — recognized as batch because "Value" matches "value" case-insensitively
            Assert.AreEqual(2, result!.Body!.Value!.Count, "Case-insensitive options must recognize uppercase 'Value' as the batch envelope property.");
        }

        [TestMethod]
        public void Deserialize_CaseSensitiveOptions_BatchShapeWithUppercaseValueIsSingleItem()
        {
            // Arrange — batch payload using "Value" (capital V), but case-SENSITIVE options.
            // "Value" ≠ "value" under Ordinal comparison, so the discriminator must treat
            // the body as a single-item T rather than the batch wrapper.
            var payload = """{"body":{"Value":[{"subject":"Email 1"}]}}""";
            var caseSensitiveOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                payload,
                caseSensitiveOptions);

            // Assert — treated as a single-item (the whole body is deserialized as T)
            // because the property name "Value" does not match the wire name "value" strictly.
            Assert.AreEqual(1, result!.Body!.Value!.Count, "Case-sensitive options must NOT treat 'Value' (capital V) as the batch envelope property.");
        }

        #endregion Discriminator correctness

        #region Serializer null-handling

        [TestMethod]
        public void Serialize_NullValue_WhenWritingNullOption_OmitsValueProperty()
        {
            // Arrange — TriggerCallbackBody<T> with null Value, using WhenWritingNull options
            // (matching the SDK default in ConnectorClientBase / ConnectorJsonSerializer).
            var body = new TriggerCallbackBody<GraphClientReceiveMessage> { Value = null };
            var nullIgnoreOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
            };
            nullIgnoreOptions.Converters.Add(new TriggerCallbackBodyConverterFactory());

            // Act
            var json = JsonSerializer.Serialize(body, nullIgnoreOptions);

            // Assert — "value" property must be omitted when Value is null and WhenWritingNull is set
            Assert.IsFalse(json.Contains("\"value\"", StringComparison.Ordinal),
                $"WhenWritingNull must suppress the 'value' property when Value is null. Got: {json}");
            Assert.AreEqual("{}", json, "Serialized body with null Value should be an empty object when WhenWritingNull is configured.");
        }

        [TestMethod]
        public void Serialize_NullValue_WhenWritingDefaultNullOption_OmitsValueProperty()
        {
            // Arrange — WhenWritingDefault also omits null reference types.
            var body = new TriggerCallbackBody<GraphClientReceiveMessage> { Value = null };
            var defaultIgnoreOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNameCaseInsensitive = true,
            };
            defaultIgnoreOptions.Converters.Add(new TriggerCallbackBodyConverterFactory());

            // Act
            var json = JsonSerializer.Serialize(body, defaultIgnoreOptions);

            // Assert
            Assert.IsFalse(json.Contains("\"value\"", StringComparison.Ordinal),
                $"WhenWritingDefault must suppress the 'value' property when Value is null. Got: {json}");
        }

        [TestMethod]
        public void Serialize_NullValue_NeverIgnoreOption_WritesNullProperty()
        {
            // Arrange — JsonIgnoreCondition.Never means null values are always written.
            var body = new TriggerCallbackBody<GraphClientReceiveMessage> { Value = null };
            var neverIgnoreOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNameCaseInsensitive = true,
            };
            neverIgnoreOptions.Converters.Add(new TriggerCallbackBodyConverterFactory());

            // Act
            var json = JsonSerializer.Serialize(body, neverIgnoreOptions);

            // Assert — "value" property must be present with a null value
            Assert.IsTrue(json.Contains("\"value\"", StringComparison.Ordinal),
                $"Never-ignore option must write the 'value' property even when null. Got: {json}");
            Assert.IsTrue(json.Contains("null", StringComparison.Ordinal),
                $"Value must be serialized as JSON null. Got: {json}");
        }

        [TestMethod]
        public void Deserialize_NullValueRoundTrip_PreservesNullList()
        {
            // Arrange — {"value":null} round-trips correctly: the sole "value":null property
            // must be recognized as the batch envelope (not a single-item T), and Value is
            // preserved as null rather than wrapping the object as a single-item list.
            var payload = """{"body":{"value":null}}""";
            var neverIgnoreOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNameCaseInsensitive = true,
            };

            // Act
            var result = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                payload,
                neverIgnoreOptions);

            // Assert — body detected as batch envelope; Value is null (not a 1-element list)
            Assert.IsNotNull(result);
            Assert.IsNull(result.Body!.Value,
                "A sole 'value':null property must be treated as the batch envelope with a null list, not as a single-item T.");
        }

        #endregion Serializer null-handling
    }
}
