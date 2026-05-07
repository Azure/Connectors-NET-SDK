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
using Azure.Connectors.Sdk.Smtp;
using Azure.Connectors.Sdk.Smtp.Models;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated SmtpClient class.
    /// </summary>
    [TestClass]
    public class SmtpClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new SmtpClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new SmtpClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new SmtpClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new SmtpClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act & Assert - calling Dispose twice should not throw (idempotent)
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task SendEmailAsync_WithMockedResponse_ShouldSucceed()
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
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(string.Empty)
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();
            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));
            options.Retry.MaxRetries = 0;

            using var client = new SmtpClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            var email = new Email
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Test Subject",
                Body = "Test Body"
            };

            // Act & Assert - should not throw
            await client
                .SendEmailAsync(input: email, cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task SendEmailAsync_WithErrorResponse_ThrowsConnectorException()
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

            using var client = new SmtpClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            var email = new Email
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Test Subject",
                Body = "Test Body"
            };

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .SendEmailAsync(input: email, cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Invalid request", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Email_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var email = new Email
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                CC = "cc@example.com",
                Bcc = "bcc@example.com",
                Subject = "Test Subject",
                Body = "Test Body",
                Importance = "High",
                ReadReceipt = "read@example.com",
                DeliveryReceipt = "delivery@example.com",
                Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        FileName = "test.txt",
                        ContentType = "text/plain",
                        ContentData = "dGVzdA==",
                        ContentId = "attachment-1"
                    }
                }
            };

            // Assert
            Assert.AreEqual("sender@example.com", email.From);
            Assert.AreEqual("recipient@example.com", email.To);
            Assert.AreEqual("cc@example.com", email.CC);
            Assert.AreEqual("bcc@example.com", email.Bcc);
            Assert.AreEqual("Test Subject", email.Subject);
            Assert.AreEqual("Test Body", email.Body);
            Assert.AreEqual("High", email.Importance);
            Assert.AreEqual(1, email.Attachments.Count);
            Assert.AreEqual("test.txt", email.Attachments[0].FileName);
        }

        [TestMethod]
        public void Email_JsonSerialization_RoundTrips()
        {
            // Arrange
            var email = new Email
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Round-trip Test",
                Body = "Body content",
                Importance = "Normal",
                Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        FileName = "doc.pdf",
                        ContentType = "application/pdf",
                        ContentData = "JVBER",
                        ContentId = "att-1"
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(email);
            var deserialized = JsonSerializer.Deserialize<Email>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(email.From, deserialized.From);
            Assert.AreEqual(email.To, deserialized.To);
            Assert.AreEqual(email.Subject, deserialized.Subject);
            Assert.AreEqual(email.Body, deserialized.Body);
            Assert.AreEqual(email.Importance, deserialized.Importance);
            Assert.IsNotNull(deserialized.Attachments);
            Assert.AreEqual(1, deserialized.Attachments.Count);
            Assert.AreEqual("doc.pdf", deserialized.Attachments[0].FileName);
        }

        [TestMethod]
        public void Attachment_JsonSerialization_RoundTrips()
        {
            // Arrange
            var attachment = new Attachment
            {
                FileName = "image.png",
                ContentType = "image/png",
                ContentData = "iVBORw0KGgo=",
                ContentId = "cid-123"
            };

            // Act
            var json = JsonSerializer.Serialize(attachment);
            var deserialized = JsonSerializer.Deserialize<Attachment>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(attachment.FileName, deserialized.FileName);
            Assert.AreEqual(attachment.ContentType, deserialized.ContentType);
            Assert.AreEqual(attachment.ContentData, deserialized.ContentData);
            Assert.AreEqual(attachment.ContentId, deserialized.ContentId);
        }
    }
}
