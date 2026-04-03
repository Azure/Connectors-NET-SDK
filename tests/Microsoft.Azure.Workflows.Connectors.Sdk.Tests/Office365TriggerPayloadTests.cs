//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Tests
{
    using System.Text.Json;
    using Microsoft.Azure.Connectors.DirectClient.Office365;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for per-trigger convenience payload types and the trigger registry.
    /// </summary>
    [TestClass]
    public class Office365TriggerPayloadTests
    {
        /// <summary>
        /// Captured AI Gateway trigger callback payload from production (2026-03-16).
        /// </summary>
        private const string CapturedEmailTriggerPayload = """
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

        private const string CalendarEventTriggerPayload = """
            {
              "body": {
                "value": [
                  {
                    "subject": "Team standup",
                    "start": "2026-03-17T09:00:00.0000000",
                    "end": "2026-03-17T09:30:00.0000000",
                    "body": "Daily sync",
                    "isHtml": false,
                    "responseType": "accepted",
                    "organizer": "manager@microsoft.com"
                  }
                ]
              }
            }
            """;

        private const string CalendarChangedEventTriggerPayload = """
            {
              "body": {
                "value": [
                  {
                    "actionType": "updated",
                    "isAdded": false,
                    "isUpdated": true,
                    "subject": "Rescheduled meeting",
                    "start": "2026-03-17T14:00:00.0000000",
                    "end": "2026-03-17T15:00:00.0000000"
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
        public void Deserialize_OnNewEmailV3TriggerPayload_MatchesGenericPayload()
        {
            // Act
            var typed = JsonSerializer.Deserialize<Office365OnNewEmailV3TriggerPayload>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            var generic = JsonSerializer.Deserialize<TriggerCallbackPayload<GraphClientReceiveMessage>>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert — typed convenience type produces identical result
            Assert.IsNotNull(typed);
            Assert.IsNotNull(typed.Body);
            Assert.IsNotNull(typed.Body.Value);
            Assert.IsNotNull(generic);
            Assert.IsNotNull(generic.Body);
            Assert.IsNotNull(generic.Body.Value);
            Assert.AreEqual(generic.Body.Value.Count, typed.Body.Value.Count);
            Assert.AreEqual(generic.Body.Value[0].Subject, typed.Body.Value[0].Subject);
            Assert.AreEqual(generic.Body.Value[0].From, typed.Body.Value[0].From);
            Assert.AreEqual(generic.Body.Value[0].MessageId, typed.Body.Value[0].MessageId);
        }

        [TestMethod]
        public void Deserialize_OnNewEmailV3TriggerPayload_ParsesEmailFields()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365OnNewEmailV3TriggerPayload>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            var email = result.Body.Value[0];

            // Assert
            Assert.AreEqual("AAMkADlmOTA3NWNm", email.MessageId);
            Assert.AreEqual("Test email for trigger callback", email.Subject);
            Assert.AreEqual("sender@microsoft.com", email.From);
            Assert.AreEqual("normal", email.Importance);
            Assert.AreEqual(false, email.IsRead);
        }

        [TestMethod]
        public void Deserialize_OnFlaggedEmailV3TriggerPayload_Works()
        {
            // Act — OnFlaggedEmailV3 uses the same response type (GraphClientReceiveMessage)
            var result = JsonSerializer.Deserialize<Office365OnFlaggedEmailV3TriggerPayload>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            Assert.AreEqual("Test email for trigger callback", result.Body.Value[0].Subject);
        }

        [TestMethod]
        public void Deserialize_OnFlaggedEmailV4TriggerPayload_Works()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365OnFlaggedEmailV4TriggerPayload>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_OnNewMentionMeEmailV3TriggerPayload_Works()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365OnNewMentionMeEmailV3TriggerPayload>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_SharedMailboxOnNewEmailV2TriggerPayload_Works()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365SharedMailboxOnNewEmailV2TriggerPayload>(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_OnUpcomingEventsV3TriggerPayload_ParsesCalendarFields()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365OnUpcomingEventsV3TriggerPayload>(
                Office365TriggerPayloadTests.CalendarEventTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            var calendarEvent = result.Body.Value[0];

            // Assert
            Assert.AreEqual("Team standup", calendarEvent.Subject);
            Assert.AreEqual("Daily sync", calendarEvent.Body);
            Assert.AreEqual("accepted", calendarEvent.ResponseType);
            Assert.AreEqual("manager@microsoft.com", calendarEvent.Organizer);
        }

        [TestMethod]
        public void Deserialize_CalendarGetOnNewItemsV3TriggerPayload_Works()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365CalendarGetOnNewItemsV3TriggerPayload>(
                Office365TriggerPayloadTests.CalendarEventTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            Assert.AreEqual("Team standup", result.Body.Value[0].Subject);
        }

        [TestMethod]
        public void Deserialize_CalendarGetOnUpdatedItemsV3TriggerPayload_Works()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365CalendarGetOnUpdatedItemsV3TriggerPayload>(
                Office365TriggerPayloadTests.CalendarEventTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
        }

        [TestMethod]
        public void Deserialize_CalendarGetOnChangedItemsV3TriggerPayload_ParsesActionType()
        {
            // Act
            var result = JsonSerializer.Deserialize<Office365CalendarGetOnChangedItemsV3TriggerPayload>(
                Office365TriggerPayloadTests.CalendarChangedEventTriggerPayload,
                Office365TriggerPayloadTests.JsonOptions);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Body);
            Assert.IsNotNull(result.Body.Value);
            Assert.AreEqual(1, result.Body.Value.Count);
            var calendarEvent = result.Body.Value[0];

            // Assert
            Assert.AreEqual("updated", calendarEvent.ActionType);
            Assert.AreEqual(false, calendarEvent.IsAdded);
            Assert.AreEqual(true, calendarEvent.IsUpdated);
            Assert.AreEqual("Rescheduled meeting", calendarEvent.Subject);
        }

        [TestMethod]
        public void Office365Triggers_Operations_ContainsAllTriggers()
        {
            // Assert — all 9 trigger operations are registered
            Assert.AreEqual(9, Office365Triggers.Operations.Count);
        }

        [TestMethod]
        public void Office365Triggers_Operations_ContainsEmailTriggers()
        {
            // Assert
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("OnNewEmailV3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("OnFlaggedEmailV3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("OnFlaggedEmailV4"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("OnNewMentionMeEmailV3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("SharedMailboxOnNewEmailV2"));
        }

        [TestMethod]
        public void Office365Triggers_Operations_ContainsCalendarTriggers()
        {
            // Assert
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("OnUpcomingEventsV3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("CalendarGetOnNewItemsV3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("CalendarGetOnUpdatedItemsV3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("CalendarGetOnChangedItemsV3"));
        }

        [TestMethod]
        public void Office365Triggers_Operations_MapsToCorrectTypes()
        {
            // Assert — email triggers map to GraphClientReceiveMessage-based payloads
            Assert.AreEqual(typeof(Office365OnNewEmailV3TriggerPayload), Office365Triggers.Operations["OnNewEmailV3"]);
            Assert.AreEqual(typeof(Office365OnFlaggedEmailV3TriggerPayload), Office365Triggers.Operations["OnFlaggedEmailV3"]);

            // Assert — calendar triggers map to GraphCalendarEventClientReceive-based payloads
            Assert.AreEqual(typeof(Office365OnUpcomingEventsV3TriggerPayload), Office365Triggers.Operations["OnUpcomingEventsV3"]);

            // Assert — changed items trigger maps to GraphCalendarEventClientWithActionType
            Assert.AreEqual(typeof(Office365CalendarGetOnChangedItemsV3TriggerPayload), Office365Triggers.Operations["CalendarGetOnChangedItemsV3"]);
        }

        [TestMethod]
        public void Office365Triggers_Operations_IsCaseInsensitive()
        {
            // Assert — dictionary uses OrdinalIgnoreCase
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("onnewemailv3"));
            Assert.IsTrue(Office365Triggers.Operations.ContainsKey("ONNEWEMAILV3"));
        }

        [TestMethod]
        public void Office365Triggers_Operations_CanDeserializeDynamically()
        {
            // Arrange — simulate dynamic lookup by operation name
            var operationName = "OnNewEmailV3";
            var payloadType = Office365Triggers.Operations[operationName];

            // Act — deserialize using the dynamically-resolved type
            var result = JsonSerializer.Deserialize(
                Office365TriggerPayloadTests.CapturedEmailTriggerPayload,
                payloadType,
                Office365TriggerPayloadTests.JsonOptions);

            // Assert — the result is the correct typed payload
            Assert.IsInstanceOfType(result, typeof(Office365OnNewEmailV3TriggerPayload));
            var typed = (Office365OnNewEmailV3TriggerPayload)result!;
            Assert.AreEqual(1, typed.Body!.Value!.Count);
            Assert.AreEqual("Test email for trigger callback", typed.Body.Value[0].Subject);
        }

        [TestMethod]
        public void TriggerPayload_IsAssignableToGenericBase()
        {
            // Assert — all trigger payload types are subtypes of TriggerCallbackPayload<T>
            Assert.IsTrue(typeof(TriggerCallbackPayload<GraphClientReceiveMessage>)
                .IsAssignableFrom(typeof(Office365OnNewEmailV3TriggerPayload)));
            Assert.IsTrue(typeof(TriggerCallbackPayload<GraphCalendarEventClientReceive>)
                .IsAssignableFrom(typeof(Office365OnUpcomingEventsV3TriggerPayload)));
            Assert.IsTrue(typeof(TriggerCallbackPayload<GraphCalendarEventClientWithActionType>)
                .IsAssignableFrom(typeof(Office365CalendarGetOnChangedItemsV3TriggerPayload)));
        }
    }
}
