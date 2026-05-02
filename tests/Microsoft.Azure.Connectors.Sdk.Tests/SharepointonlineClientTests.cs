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
using Microsoft.Azure.Connectors.Sdk.Sharepointonline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated SharepointonlineClient class.
    /// </summary>
    [TestClass]
    public class SharepointonlineClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new SharepointonlineClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new SharepointonlineClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new SharepointonlineClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public async Task GetAllTablesAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new TablesList
            {
                Value = new List<Table>
                {
                    new Table
                    {
                        Name = "Documents",
                        DisplayName = "Documents Library"
                    },
                    new Table
                    {
                        Name = "Contacts",
                        DisplayName = "Contacts List"
                    }
                }
            };

            using var responseMessage = new HttpResponseMessage
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Callback(() => { })
                .Verifiable();

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new SharepointonlineClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var result = await client
                .GetAllTablesAsync(siteAddress: "https://contoso.sharepoint.com/sites/test", cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(2, result.Value.Count);
            Assert.AreEqual("Documents", result.Value[0].Name);
            Assert.AreEqual("Contacts List", result.Value[1].DisplayName);
        }

        [TestMethod]
        public async Task GetAllTablesAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"Site not found\"}")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Callback(() => { })
                .Verifiable();

            var mockCredential = new Mock<TokenCredential>();
            mockCredential
                .Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));

            var httpClient = new HttpClient(mockHandler.Object);

            using var client = new SharepointonlineClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<SharepointonlineConnectorException>(async () =>
                    await client
                        .GetAllTablesAsync(siteAddress: "https://contoso.sharepoint.com/sites/nonexistent", cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(404, exception.StatusCode);
            Assert.IsTrue(exception.ResponseBody.Contains("Site not found", StringComparison.Ordinal));
        }

        [TestMethod]
        public void SharepointonlineConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new SharepointonlineConnectorException(
                operation: "GET /test",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.StatusCode);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("GET /test", StringComparison.Ordinal));
            Assert.IsTrue(exception.Message.Contains("403", StringComparison.Ordinal));
        }

        [TestMethod]
        public void TablesList_JsonSerialization_RoundTrips()
        {
            // Arrange
            var tablesList = new TablesList
            {
                Value = new List<Table>
                {
                    new Table
                    {
                        Name = "TestList",
                        DisplayName = "Test List"
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(tablesList);
            var deserialized = JsonSerializer.Deserialize<TablesList>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Value);
            Assert.AreEqual(1, deserialized.Value.Count);
            Assert.AreEqual("TestList", deserialized.Value[0].Name);
        }

        [TestMethod]
        public void Dispose_WithInjectedHttpClient_ShouldNotDisposeIt()
        {
            // Arrange
            var httpClient = new HttpClient();
            var mockCredential = new Mock<TokenCredential>();

            var client = new SharepointonlineClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            client.Dispose();

            // Assert - injected HttpClient should still be usable (not disposed)
            httpClient.DefaultRequestHeaders.Add("X-Test-Header", "TestValue");
            Assert.IsTrue(httpClient.DefaultRequestHeaders.Contains("X-Test-Header"));
        }

        [TestMethod]
        public void Dispose_WithInternallyCreatedHttpClient_ShouldDisposeIt()
        {
            // Arrange - no httpClient provided, so client creates its own
            var mockCredential = new Mock<TokenCredential>();
            var client = new SharepointonlineClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public void BlobMetadata_ShouldHaveExpectedProperties()
        {
            // Arrange & Act
            var metadata = new BlobMetadata
            {
                Id = "file-123",
                Name = "document.docx",
                DisplayName = "My Document",
                Path = "/sites/test/Shared Documents/document.docx",
                Size = 1024,
                MediaType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                IsFolder = false
            };

            // Assert
            Assert.AreEqual("file-123", metadata.Id);
            Assert.AreEqual("document.docx", metadata.Name);
            Assert.AreEqual("/sites/test/Shared Documents/document.docx", metadata.Path);
            Assert.AreEqual(1024, metadata.Size);
            Assert.IsFalse(metadata.IsFolder);
        }
    }
}
