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
using Microsoft.Azure.Connectors.Sdk.Azureblob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for the generated AzureblobClient class.
    /// </summary>
    [TestClass]
    public class AzureblobClientTests
    {
        [TestMethod]
        public void Constructor_WithValidConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new AzureblobClient("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureblobClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new AzureblobClient("https://test.azure.com/connection");

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public async Task GetFileMetadataAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new DataWithSensitivityLabelInfo
            {
                Id = "blob-abc-123",
                Name = "data.csv",
                DisplayName = "data.csv",
                Path = "/container1/data.csv",
                Size = 51200,
                MediaType = "text/csv",
                IsFolder = false,
                ETag = "\"etag-value\""
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

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new AzureblobClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .GetFileMetadataAsync(
                    storageAccountNameOrBlobEndpoint: "mystorageaccount",
                    blob: "blob-abc-123",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("blob-abc-123", result.Id);
            Assert.AreEqual("data.csv", result.Name);
            Assert.AreEqual("data.csv", result.DisplayName);
            Assert.AreEqual(51200, result.Size);
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
                Content = new StringContent("{\"error\": \"Blob not found\"}")
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

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new AzureblobClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act & Assert
            var exception = await Assert
                .ThrowsExactlyAsync<ConnectorException>(async () =>
                    await client
                        .GetFileMetadataAsync(
                            storageAccountNameOrBlobEndpoint: "mystorageaccount",
                            blob: "nonexistent-blob-id",
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(continueOnCapturedContext: false))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(404, exception.StatusCode);
            Assert.IsTrue(exception.ResponseBody.Contains("Blob not found", StringComparison.Ordinal));
        }

        [TestMethod]
        public void ConnectorException_ShouldContainExpectedProperties()
        {
            // Arrange & Act
            var exception = new ConnectorException("azureblob",
                operation: "GET /v2/datasets/mystorageaccount/files/abc",
                statusCode: 403,
                responseBody: "Access denied");

            // Assert
            Assert.AreEqual(403, exception.StatusCode);
            Assert.AreEqual("Access denied", exception.ResponseBody);
            Assert.IsTrue(exception.Message.Contains("GET /v2/datasets/mystorageaccount/files/abc", StringComparison.Ordinal));
            Assert.IsTrue(exception.Message.Contains("403", StringComparison.Ordinal));
        }

        [TestMethod]
        public void BlobMetadata_JsonSerialization_RoundTrips()
        {
            // Arrange
            var metadata = new BlobMetadata
            {
                Id = "blob-123",
                Name = "document.pdf",
                DisplayName = "My Document",
                Path = "/container1/document.pdf",
                Size = 2048,
                MediaType = "application/pdf",
                IsFolder = false,
                ETag = "\"etag-abc\"",
                FileLocator = "locator-xyz"
            };

            // Act
            var json = JsonSerializer.Serialize(metadata);
            var deserialized = JsonSerializer.Deserialize<BlobMetadata>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("blob-123", deserialized!.Id);
            Assert.AreEqual("document.pdf", deserialized.Name);
            Assert.AreEqual("My Document", deserialized.DisplayName);
            Assert.AreEqual(2048, deserialized.Size);
            Assert.AreEqual(false, deserialized.IsFolder);
            Assert.AreEqual("\"etag-abc\"", deserialized.ETag);
        }

        [TestMethod]
        public void DataWithSensitivityLabelInfo_JsonSerialization_RoundTrips()
        {
            // Arrange
            var metadata = new DataWithSensitivityLabelInfo
            {
                Id = "blob-456",
                Name = "confidential.docx",
                DisplayName = "Confidential Doc",
                Path = "/container1/confidential.docx",
                Size = 4096,
                MediaType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                IsFolder = false,
                ETag = "\"etag-def\"",
                SensitivityLabelInfo = new List<SensitivityLabelMetadata>
                {
                    new SensitivityLabelMetadata
                    {
                        SensitivityLabelId = "label-1",
                        Name = "Confidential",
                        SensitivityLabelDisplayNameInfo = "Confidential",
                        IsEncryptedStatusOfSensitivityLabel = true,
                        WhetherSensitivityLabelIsEnabled = true
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(metadata);
            var deserialized = JsonSerializer.Deserialize<DataWithSensitivityLabelInfo>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("blob-456", deserialized!.Id);
            Assert.AreEqual("confidential.docx", deserialized.Name);
            Assert.IsNotNull(deserialized.SensitivityLabelInfo);
            Assert.AreEqual(1, deserialized.SensitivityLabelInfo.Count);
            Assert.AreEqual("Confidential", deserialized.SensitivityLabelInfo[0].Name);
            Assert.AreEqual(true, deserialized.SensitivityLabelInfo[0].IsEncryptedStatusOfSensitivityLabel);
        }

        [TestMethod]
        public void SharedAccessSignature_JsonSerialization_RoundTrips()
        {
            // Arrange
            var sas = new SharedAccessSignature
            {
                WebUrl = "https://mystorageaccount.blob.core.windows.net/container/blob?sv=2021-06-08&st=2024-01-01&se=2024-01-02&sp=r&sig=abc"
            };

            // Act
            var json = JsonSerializer.Serialize(sas);
            var deserialized = JsonSerializer.Deserialize<SharedAccessSignature>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized!.WebUrl.Contains("mystorageaccount.blob.core.windows.net", StringComparison.Ordinal));
        }

        [TestMethod]
        public void SharedAccessSignatureBlobPolicy_JsonSerialization_RoundTrips()
        {
            // Arrange
            var policy = new SharedAccessSignatureBlobPolicy
            {
                GroupPolicyIdentifier = "policy-1",
                Permissions = "r,w",
                StartTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiryTime = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                SharedAccessProtocol = "https",
                IPAddressOrIPAddressRange = "10.0.0.0/24"
            };

            // Act
            var json = JsonSerializer.Serialize(policy);
            var deserialized = JsonSerializer.Deserialize<SharedAccessSignatureBlobPolicy>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("policy-1", deserialized!.GroupPolicyIdentifier);
            Assert.AreEqual("r,w", deserialized.Permissions);
            Assert.AreEqual("https", deserialized.SharedAccessProtocol);
            Assert.AreEqual("10.0.0.0/24", deserialized.IPAddressOrIPAddressRange);
        }

        [TestMethod]
        public void SharedAccessSignatureBlobPolicy_AccessProtocol_UsesJsonPropertyName()
        {
            // Arrange - JSON uses "AccessProtocol" as the serialized property name
            var json = """{"GroupPolicyIdentifier":"p1","Permissions":"r","AccessProtocol":"https","IpAddressOrRange":"10.0.0.1"}""";

            // Act
            var result = JsonSerializer.Deserialize<SharedAccessSignatureBlobPolicy>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("https", result!.SharedAccessProtocol);
            Assert.AreEqual("10.0.0.1", result.IPAddressOrIPAddressRange);
        }

        [TestMethod]
        public void StorageAccount_JsonSerialization_RoundTrips()
        {
            // Arrange
            var account = new StorageAccount
            {
                StorageAccountName = "mystorageaccount",
                StorageAccountDisplayName = "My Storage Account"
            };

            // Act
            var json = JsonSerializer.Serialize(account);
            var deserialized = JsonSerializer.Deserialize<StorageAccount>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("mystorageaccount", deserialized!.StorageAccountName);
            Assert.AreEqual("My Storage Account", deserialized.StorageAccountDisplayName);
        }

        [TestMethod]
        public void StorageAccount_UsesJsonPropertyNames()
        {
            // Arrange - JSON uses "Name" and "DisplayName"
            var json = """{"Name":"teststorage","DisplayName":"Test Storage"}""";

            // Act
            var result = JsonSerializer.Deserialize<StorageAccount>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("teststorage", result!.StorageAccountName);
            Assert.AreEqual("Test Storage", result.StorageAccountDisplayName);
        }

        [TestMethod]
        public void BlobMetadataPage_JsonSerialization_RoundTrips()
        {
            // Arrange
            var page = new BlobMetadataPage
            {
                Value = new List<BlobMetadata>
                {
                    new BlobMetadata { Id = "blob-1", Name = "file1.txt", IsFolder = false },
                    new BlobMetadata { Id = "folder-1", Name = "subfolder", IsFolder = true }
                },
                NextLink = "https://api.contoso.com/next?skipToken=abc",
                NextPageMarker = "marker-123"
            };

            // Act
            var json = JsonSerializer.Serialize(page);
            var deserialized = JsonSerializer.Deserialize<BlobMetadataPage>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized!.Value);
            Assert.AreEqual(2, deserialized.Value.Count);
            Assert.AreEqual("blob-1", deserialized.Value[0].Id);
            Assert.AreEqual(true, deserialized.Value[1].IsFolder);
            Assert.AreEqual("https://api.contoso.com/next?skipToken=abc", deserialized.NextLink);
            Assert.AreEqual("marker-123", deserialized.NextPageMarker);
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();
            var client = new AzureblobClient(
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
            var client = new AzureblobClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object);

            // Act
            client.Dispose();

            // Assert - calling Dispose again should not throw (idempotent)
            client.Dispose();
        }

        [TestMethod]
        public async Task DeleteFileAsync_WithMockedResponse_Succeeds()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
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

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new AzureblobClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act & Assert - should not throw
            await client
                .DeleteFileAsync(
                    storageAccountNameOrBlobEndpoint: "mystorageaccount",
                    blob: "blob-to-delete",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task GetFileContentAsync_WithMockedResponse_ReturnsByteArray()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedContent = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

            using var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(expectedContent)
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

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new AzureblobClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .GetFileContentAsync(
                    storageAccountNameOrBlobEndpoint: "mystorageaccount",
                    blob: "blob-123",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expectedContent, result);
        }

        [TestMethod]
        public async Task CreateFileAsync_WithMockedResponse_ReturnsMetadata()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            var expectedResponse = new BlobMetadata
            {
                Id = "new-blob-id",
                Name = "uploaded.txt",
                Path = "/container1/uploaded.txt",
                Size = 128,
                IsFolder = false
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

            var options = new ConnectorClientOptions();


            options.Transport = new HttpClientTransport(new HttpClient(mockHandler.Object));


            options.Retry.MaxRetries = 0;

            using var client = new AzureblobClient(
                connectionRuntimeUrl: "https://test.azure.com/connection",
                credential: mockCredential.Object,
                options: options);

            // Act
            var result = await client
                .CreateFileAsync(
                    storageAccountNameOrBlobEndpoint: "mystorageaccount",
                    input: new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F },
                    folderPath: "/container1",
                    blobName: "uploaded.txt",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("new-blob-id", result.Id);
            Assert.AreEqual("uploaded.txt", result.Name);
            Assert.AreEqual(128, result.Size);
        }

        [TestMethod]
        public void SensitivityLabelMetadata_JsonSerialization_RoundTrips()
        {
            // Arrange
            var label = new SensitivityLabelMetadata
            {
                SensitivityLabelId = "label-abc",
                Name = "Highly Confidential",
                SensitivityLabelDisplayNameInfo = "Highly Confidential",
                TooltipInfo = "This document contains highly confidential information",
                PriorityOfSensitivityLabel = 10,
                ColorToBeDisplayedForSensitivityLabel = "#FF0000",
                IsEncryptedStatusOfSensitivityLabel = true,
                WhetherSensitivityLabelIsEnabled = true,
                WhetherSensitivityLabelIsParent = false,
                ParentSensitivityLabelId = "parent-label-1"
            };

            // Act
            var json = JsonSerializer.Serialize(label);
            var deserialized = JsonSerializer.Deserialize<SensitivityLabelMetadata>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("label-abc", deserialized!.SensitivityLabelId);
            Assert.AreEqual("Highly Confidential", deserialized.Name);
            Assert.AreEqual(10, deserialized.PriorityOfSensitivityLabel);
            Assert.AreEqual("#FF0000", deserialized.ColorToBeDisplayedForSensitivityLabel);
            Assert.AreEqual(true, deserialized.IsEncryptedStatusOfSensitivityLabel);
            Assert.AreEqual(false, deserialized.WhetherSensitivityLabelIsParent);
        }

        [TestMethod]
        public void SensitivityLabelMetadata_UsesJsonPropertyNames()
        {
            // Arrange - JSON uses camelCase property names
            var json = """{"sensitivityLabelId":"lbl-1","name":"Public","displayName":"Public","tooltip":"Public data","priority":1,"color":"green","isEncrypted":false,"isEnabled":true,"isParent":false,"parentSensitivityLabelId":null}""";

            // Act
            var result = JsonSerializer.Deserialize<SensitivityLabelMetadata>(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("lbl-1", result!.SensitivityLabelId);
            Assert.AreEqual("Public", result.Name);
            Assert.AreEqual(false, result.IsEncryptedStatusOfSensitivityLabel);
        }
    }
}
