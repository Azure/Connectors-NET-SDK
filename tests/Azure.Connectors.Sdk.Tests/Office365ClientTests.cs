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
using Azure.Connectors.Sdk.Office365;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated Office365Client class.
    /// </summary>
    [TestClass]
    public class Office365ClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new Office365Client("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new Office365Client(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new Office365Client("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new Office365Client(
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
            var client = new Office365Client(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task GetOutlookCategoryNamesAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new List<GraphOutlookCategory>
            {
                new GraphOutlookCategory
                {
                    Id = "category-1",
                    DisplayName = "Red Category"
                },
                new GraphOutlookCategory
                {
                    Id = "category-2",
                    DisplayName = "Blue Category"
                }
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365Client(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .GetOutlookCategoryNamesAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Red Category", result[0].DisplayName);
            Assert.AreEqual("Blue Category", result[1].DisplayName);
        }

        [TestMethod]
        public async Task GetEmailAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\": \"Invalid request\"}")
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new Office365Client(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .GetEmailAsync(messageId: "test-message-id", cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Invalid request"));
        }

        [TestMethod]
        public void GraphCalendarEventClientReceive_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var calendarEvent = new GraphCalendarEventClientReceive
            {
                Subject = "Test Meeting",
                StartTime = "2024-01-15T10:00:00Z",
                EndTime = "2024-01-15T11:00:00Z",
                Id = "event-123",
                Organizer = "organizer@test.com",
                Location = "Conference Room A"
            };

            // Assert
            Assert.AreEqual("Test Meeting", calendarEvent.Subject);
            Assert.AreEqual("event-123", calendarEvent.Id);
            Assert.AreEqual("Conference Room A", calendarEvent.Location);
            Assert.AreEqual("organizer@test.com", calendarEvent.Organizer);
        }

        [TestMethod]
        public void GraphOutlookCategory_JsonSerialization_RoundTrips()
        {
            // Arrange
            var category = new GraphOutlookCategory
            {
                Id = "category-abc",
                DisplayName = "Important"
            };

            // Act
            var json = JsonSerializer.Serialize(category);
            var deserialized = JsonSerializer.Deserialize<GraphOutlookCategory>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(category.DisplayName, deserialized.DisplayName);
            Assert.AreEqual(category.Id, deserialized.Id);
        }

        [TestMethod]
        public void GraphClientReceiveMessage_JsonSerialization_RoundTrips()
        {
            // Arrange
            var message = new GraphClientReceiveMessage
            {
                MessageId = "msg-123",
                Subject = "Test Subject",
                From = "sender@test.com",
                To = "recipient@test.com",
                Body = "This is a test message body.",
                IsHTML = false,
                IsRead = true,
                HasAttachment = false,
                Importance = "normal"
            };

            // Act
            var json = JsonSerializer.Serialize(message);
            var deserialized = JsonSerializer.Deserialize<GraphClientReceiveMessage>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(message.MessageId, deserialized.MessageId);
            Assert.AreEqual(message.Subject, deserialized.Subject);
            Assert.AreEqual(message.From, deserialized.From);
            Assert.AreEqual(message.To, deserialized.To);
            Assert.AreEqual(message.Body, deserialized.Body);
            Assert.AreEqual(message.IsRead, deserialized.IsRead);
        }
    }
}
