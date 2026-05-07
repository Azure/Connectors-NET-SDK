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
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.Azure.Connectors.Sdk.Mq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated MqClient class.
    /// </summary>
    [TestClass]
    public class MqClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new MqClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new MqClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new MqClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new MqClient(
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
            var client = new MqClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task SendAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new SendResponse
            {
                ItemInternalId = "internal-1",
                MessageData = "Hello MQ",
                MessageId = "msg-001",
                CorrelationId = "corr-001"
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

            using var client = new MqClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .SendAsync(
                    new SendValidDataOptions { Message = "Hello MQ", Queue = "TEST.QUEUE" },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("msg-001", result.MessageId);
            Assert.AreEqual("Hello MQ", result.MessageData);
            Assert.AreEqual("corr-001", result.CorrelationId);
        }

        [TestMethod]
        public async Task ReadAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new Item
            {
                ItemInternalId = "internal-1",
                MessageData = "Test message content",
                MessageId = "msg-002",
                CorrelationId = "corr-002",
                UserIdentifier = "testuser",
                Format = "MQSTR",
                Priority = 4
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

            using var client = new MqClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .ReadAsync(
                    new SingleGetValidOptions { Queue = "TEST.QUEUE" },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("msg-002", result.MessageId);
            Assert.AreEqual("Test message content", result.MessageData);
            Assert.AreEqual("testuser", result.UserIdentifier);
            Assert.AreEqual(4, result.Priority);
        }

        [TestMethod]
        public async Task ReceiveAsync_WithErrorResponse_ThrowsConnectorException()
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
                    Content = new StringContent("{\"error\": \"Queue not found\"}")
                });

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new MqClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .ReceiveAsync(
                            new SingleGetValidOptions { Queue = "NONEXISTENT.QUEUE" },
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(400, exception.Status);
            Assert.IsTrue(exception.ResponseBody.Contains("Queue not found", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task ReadAllAsync_WithMockedResponse_ReturnsMultipleMessages()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new ItemsList
            {
                Value = new List<Item>
                {
                    new Item { MessageId = "msg-001", MessageData = "First message" },
                    new Item { MessageId = "msg-002", MessageData = "Second message" },
                    new Item { MessageId = "msg-003", MessageData = "Third message" }
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

            using var client = new MqClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .ReadAllAsync(
                    new MultipleGetValidOptions { Queue = "TEST.QUEUE", BatchSize = 3 },
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(3, result.Value.Count);
            Assert.AreEqual("msg-001", result.Value[0].MessageId);
            Assert.AreEqual("Second message", result.Value[1].MessageData);
        }

        [TestMethod]
        public void Item_JsonSerialization_RoundTrips()
        {
            // Arrange
            var item = new Item
            {
                ItemInternalId = "internal-abc",
                MessageData = "Test payload",
                MessageId = "msg-123",
                CorrelationId = "corr-456",
                PutDateTime = new DateTime(2026, 4, 29, 10, 0, 0, DateTimeKind.Utc),
                UserIdentifier = "testuser",
                PutApplicationName = "TestApp",
                PutApplicationType = "MQMT_APPL",
                Format = "MQSTR",
                Ccsid = 1208,
                Priority = 5,
                ReplyToQueue = "REPLY.QUEUE",
                ReplyToQueueManager = "QM1"
            };

            // Act
            var json = JsonSerializer.Serialize(item);
            var deserialized = JsonSerializer.Deserialize<Item>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(item.MessageId, deserialized.MessageId);
            Assert.AreEqual(item.MessageData, deserialized.MessageData);
            Assert.AreEqual(item.CorrelationId, deserialized.CorrelationId);
            Assert.AreEqual(item.UserIdentifier, deserialized.UserIdentifier);
            Assert.AreEqual(item.Format, deserialized.Format);
            Assert.AreEqual(item.Ccsid, deserialized.Ccsid);
            Assert.AreEqual(item.Priority, deserialized.Priority);
            Assert.AreEqual(item.ReplyToQueue, deserialized.ReplyToQueue);
        }

        [TestMethod]
        public void SendResponse_JsonSerialization_RoundTrips()
        {
            // Arrange
            var sendResponse = new SendResponse
            {
                ItemInternalId = "internal-xyz",
                MessageData = "Sent content",
                MessageId = "msg-sent-001",
                CorrelationId = "corr-sent-001"
            };

            // Act
            var json = JsonSerializer.Serialize(sendResponse);
            var deserialized = JsonSerializer.Deserialize<SendResponse>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(sendResponse.MessageId, deserialized.MessageId);
            Assert.AreEqual(sendResponse.MessageData, deserialized.MessageData);
            Assert.AreEqual(sendResponse.CorrelationId, deserialized.CorrelationId);
            Assert.AreEqual(sendResponse.ItemInternalId, deserialized.ItemInternalId);
        }

        [TestMethod]
        public void SendValidDataOptions_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var options = new SendValidDataOptions
            {
                Queue = "TEST.QUEUE",
                Message = "Hello from MQ",
                MessageType = "MQMT_DATAGRAM",
                CorrelationId = "corr-001",
                ReplyToQueue = "REPLY.QUEUE",
                ReplyToQueueManager = "QM1",
                Format = "MQSTR"
            };

            // Assert
            Assert.AreEqual("TEST.QUEUE", options.Queue);
            Assert.AreEqual("Hello from MQ", options.Message);
            Assert.AreEqual("MQMT_DATAGRAM", options.MessageType);
            Assert.AreEqual("corr-001", options.CorrelationId);
            Assert.AreEqual("REPLY.QUEUE", options.ReplyToQueue);
            Assert.AreEqual("QM1", options.ReplyToQueueManager);
            Assert.AreEqual("MQSTR", options.Format);
        }

        [TestMethod]
        public void SingleGetValidOptions_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var options = new SingleGetValidOptions
            {
                Queue = "TEST.QUEUE",
                MessageId = "bXNnLWlk",
                CorrelationId = "Y29yci1pZA==",
                IncludeInfo = "true",
                Timeout = "00:00:30"
            };

            // Assert
            Assert.AreEqual("TEST.QUEUE", options.Queue);
            Assert.AreEqual("bXNnLWlk", options.MessageId);
            Assert.AreEqual("Y29yci1pZA==", options.CorrelationId);
            Assert.AreEqual("true", options.IncludeInfo);
            Assert.AreEqual("00:00:30", options.Timeout);
        }
    }
}
