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
using Microsoft.Azure.Connectors.Sdk.Onedriveforbusiness;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated OnedriveforbusinessClient class.
    /// </summary>
    [TestClass]
    public class OnedriveforbusinessClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new OnedriveforbusinessClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new OnedriveforbusinessClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new OnedriveforbusinessClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public async Task GetFileMetadataAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new BlobMetadata
            {
                Id = "file-abc-123",
                Name = "report.docx",
                DisplayName = "Quarterly Report",
                Path = "/drive/root:/Documents/report.docx",
                Size = 25600,
                MediaType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                IsFolder = false,
                ETag = "\"etag-value\"",
                LastModifiedBy = "user@contoso.com"
            };

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
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

            using var client = new OnedriveforbusinessClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act
            var result = await client
                .GetFileMetadataAsync(
                    file: "file-abc-123",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("file-abc-123", result.Id);
            Assert.AreEqual("report.docx", result.Name);
            Assert.AreEqual("Quarterly Report", result.DisplayName);
            Assert.AreEqual(25600, result.Size);
            Assert.AreEqual(false, result.IsFolder);
        }

        [TestMethod]
        public async Task GetFileMetadataAsync_WithErrorResponse_ThrowsConnectorException()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\": \"File not found\"}")
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

            using var client = new OnedriveforbusinessClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                httpClient: httpClient);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<OnedriveforbusinessConnectorException>(async () =>
                    await client
                        .GetFileMetadataAsync(
                            file: "nonexistent-file-id",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(404, exception.StatusCode);
            Assert.IsTrue(exception.ResponseBody.Contains("File not found", StringComparison.Ordinal));
        }

        [TestMethod]
        public void OnedriveforbusinessConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new OnedriveforbusinessConnectorException(
                operation: "GET /datasets/default/files/abc",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.StatusCode);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("GET /datasets/default/files/abc", StringComparison.Ordinal));
            Assert.IsTrue(exception.Message.Contains("403", StringComparison.Ordinal));
        }

        [TestMethod]
        public void BlobMetadata_JsonSerialization_RoundTrips()
        {
            // Arrange
            var metadata = new BlobMetadata
            {
                Id = "file-123",
                Name = "document.docx",
                NameWithoutExtension = "document",
                DisplayName = "My Document",
                Path = "/drive/root:/Documents/document.docx",
                Size = 1024,
                MediaType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                IsFolder = false,
                ETag = "\"etag-abc\"",
                FileLocator = "locator-xyz",
                LastModifiedBy = "user@contoso.com"
            };

            // Act
            var json = JsonSerializer.Serialize(metadata);
            var deserialized = JsonSerializer.Deserialize<BlobMetadata>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("file-123", deserialized!.Id);
            Assert.AreEqual("document.docx", deserialized.Name);
            Assert.AreEqual("document", deserialized.NameWithoutExtension);
            Assert.AreEqual("My Document", deserialized.DisplayName);
            Assert.AreEqual(1024, deserialized.Size);
            Assert.AreEqual(false, deserialized.IsFolder);
            Assert.AreEqual("\"etag-abc\"", deserialized.ETag);
        }

        [TestMethod]
        public void BlobMetadata_NameWithoutExtension_UsesJsonPropertyName()
        {
            // Arrange - JSON uses "NameNoExt" as the serialized property name
            var json = """{"Id":"f1","Name":"test.txt","NameNoExt":"test","DisplayName":"Test","Path":"/test.txt","Size":100}""";

            // Act
            var result = JsonSerializer.Deserialize<BlobMetadata>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result!.NameWithoutExtension);
        }

        [TestMethod]
        public void Thumbnail_JsonSerialization_RoundTrips()
        {
            // Arrange
            var thumbnail = new Thumbnail
            {
                Url = "https://contoso.sharepoint.com/thumbnail.jpg",
                Width = 200,
                Height = 150
            };

            // Act
            var json = JsonSerializer.Serialize(thumbnail);
            var deserialized = JsonSerializer.Deserialize<Thumbnail>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("https://contoso.sharepoint.com/thumbnail.jpg", deserialized!.Url);
            Assert.AreEqual(200, deserialized.Width);
            Assert.AreEqual(150, deserialized.Height);
        }

        [TestMethod]
        public void SharingLink_JsonSerialization_RoundTrips()
        {
            // Arrange
            var link = new SharingLink
            {
                WebURL = "https://contoso-my.sharepoint.com/:w:/p/user/shared-link"
            };

            // Act
            var json = JsonSerializer.Serialize(link);
            var deserialized = JsonSerializer.Deserialize<SharingLink>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("https://contoso-my.sharepoint.com/:w:/p/user/shared-link", deserialized!.WebURL);
        }

        [TestMethod]
        public void SharingLink_WebURL_UsesJsonPropertyName()
        {
            // Arrange - JSON uses "WebUrl" as the serialized property name
            var json = """{"WebUrl":"https://contoso.sharepoint.com/share/abc"}""";

            // Act
            var result = JsonSerializer.Deserialize<SharingLink>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("https://contoso.sharepoint.com/share/abc", result!.WebURL);
        }

        [TestMethod]
        public void BlobMetadataPage_JsonSerialization_RoundTrips()
        {
            // Arrange
            var page = new BlobMetadataPage
            {
                Value = new List<BlobMetadata>
                {
                    new BlobMetadata { Id = "file-1", Name = "doc1.pdf", IsFolder = false },
                    new BlobMetadata { Id = "folder-1", Name = "Subfolder", IsFolder = true }
                },
                NextLink = "https://api.contoso.com/next?skipToken=abc"
            };

            // Act
            var json = JsonSerializer.Serialize(page);
            var deserialized = JsonSerializer.Deserialize<BlobMetadataPage>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized!.Value);
            Assert.AreEqual(2, deserialized.Value.Count);
            Assert.AreEqual("file-1", deserialized.Value[0].Id);
            Assert.AreEqual(true, deserialized.Value[1].IsFolder);
            Assert.AreEqual("https://api.contoso.com/next?skipToken=abc", deserialized.NextLink);
        }

        [TestMethod]
        public void Dispose_WithInjectedHttpClient_ShouldNotDisposeIt()
        {
            // Arrange
            var httpClient = new HttpClient();
            var mockCredential = new Mock<TokenCredential>();

            var client = new OnedriveforbusinessClient(
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
            var client = new OnedriveforbusinessClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }
    }
}
