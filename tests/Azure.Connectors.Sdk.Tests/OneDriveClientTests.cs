//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Connectors.Sdk.OneDrive;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class OneDriveClientTests
    {
        private static OneDriveClient CreateMockedClient(Func<HttpResponseMessage> responseFactory)
        {
            var (credential, options) = ConnectorTestHelpers.CreateMockedClientSetup(responseFactory);
            return new OneDriveClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: credential,
                options: options);
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new OneDriveClient("https://test.azure.com/connection");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OneDriveClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new OneDriveClient("https://test.azure.com/connection");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var mockCredential = new Mock<TokenCredential>();
            var client = new OneDriveClient(
                connectionRuntimeUrl: new Uri("https://test.azure.com/connection"),
                credential: mockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public async Task GetFileMetadataAsync_WithMockedResponse_ReturnsExpected()
        {
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

            var result = await client
                .GetFileMetadataAsync(file: "file1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetFileMetadataAsync_WithErrorResponse_ThrowsConnectorException()
        {
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Bad request\"}")
            });

            await Assert.ThrowsExactlyAsync<ConnectorException>(() =>
                client.GetFileMetadataAsync(file: "file1",
                    cancellationToken: CancellationToken.None))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task ListRootFolderAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{\"Id\":\"file-1\",\"Name\":\"doc.pdf\",\"DisplayName\":\"doc.pdf\",\"IsFolder\":false}]")
            });

            // Act
            var result = await client
                .ListRootFolderAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("file-1", result[0].Id);
            Assert.AreEqual("doc.pdf", result[0].Name);
        }

        [TestMethod]
        public async Task CreateFileAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Id\":\"new-file-1\",\"Name\":\"upload.txt\",\"DisplayName\":\"upload.txt\",\"Size\":512}")
            });

            // Act
            var result = await client
                .CreateFileAsync(
                    input: System.Text.Encoding.UTF8.GetBytes("hello"),
                    folderPath: "/Documents",
                    fileName: "upload.txt",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("new-file-1", result.Id);
            Assert.AreEqual("upload.txt", result.Name);
        }

        [TestMethod]
        public async Task GetFileTagsAsync_WithMockedResponse_ReturnsExpectedResult()
        {
            // Arrange
            using var client = CreateMockedClient(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"Tags\":[\"design\",\"urgent\"]}")
            });

            // Act
            var result = await client
                .GetFileTagsAsync(
                    file: "file-1",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.TagsValue);
            Assert.AreEqual(2, result.TagsValue.Count);
        }

    }
}
