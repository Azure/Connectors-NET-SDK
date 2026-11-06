//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Net;
using global::Azure.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorClientBaseTests
    {
        [TestMethod]
        public void Constructor_WithUri_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new TestConnectorClientWithUri(new Uri("https://test.azure.com/connection"));

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("TestConnector", client.ConnectorName);
        }

        [TestMethod]
        public void Constructor_WithUriAndCredential_ShouldCreateInstance()
        {
            // Arrange
            var mockCredential = new Mock<TokenCredential>();

            // Act
            using var client = new TestConnectorClientWithUriAndCredential(
                new Uri("https://test.azure.com/connection"),
                mockCredential.Object);

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithString_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new TestConnectorClientWithString("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("TestConnector", client.ConnectorName);
        }

        [TestMethod]
        public void Constructor_WithNullUri_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new TestConnectorClientWithUri(null!));
        }

        [TestMethod]
        public void Constructor_WithNullString_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new TestConnectorClientWithString(null!));
        }

        [TestMethod]
        public void Constructor_WithNullCredential_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new TestConnectorClientWithUriAndCredential(
                new Uri("https://test.azure.com/connection"),
                null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new TestConnectorClientWithUri(new Uri("https://test.azure.com/connection"));

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void ResolveUrl_SameHost_ReturnsOriginalUrl()
        {
            // Arrange
            using var client = new TestConnectorClientWithString("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act
            var result = client.TestResolveUrl("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions?page=2");

            // Assert
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions?page=2", result);
        }

        [TestMethod]
        public void ResolveUrl_ForeignHost_RewritesThroughProxy()
        {
            // Arrange
            using var client = new TestConnectorClientWithString("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act
            var result = client.TestResolveUrl("https://management.azure.com/subscriptions/sub-id/resourceGroups?$skiptoken=abc");

            // Assert — path+query extracted, routed through connection runtime URL
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions/sub-id/resourceGroups?$skiptoken=abc", result);
        }

        [TestMethod]
        public void ResolveUrl_SameHostDifferentScheme_ThrowsInvalidOperation()
        {
            // Arrange
            using var client = new TestConnectorClientWithString("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act & Assert — http instead of https on same host must reject (credential leak risk)
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                client.TestResolveUrl("http://proxy.azure-apihub.net/apim/arm/conn123/subscriptions"));
        }

        [TestMethod]
        public void ResolveUrl_RelativePath_PrependsConnectionRuntimeUrl()
        {
            // Arrange
            using var client = new TestConnectorClientWithString("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act
            var result = client.TestResolveUrl("/subscriptions");

            // Assert
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions", result);
        }

        [TestMethod]
        public void ConnectorClientOptions_InheritsFromClientOptions()
        {
            // Arrange & Act
            var options = new ConnectorClientOptions();

            // Assert - should be an Azure.Core.ClientOptions
            Assert.IsInstanceOfType<global::Azure.Core.ClientOptions>(options);
        }

        [TestMethod]
        public void ConnectorClientOptions_ServiceVersion_DefaultsToV1()
        {
            // Arrange & Act
            var options = new ConnectorClientOptions();

            // Assert
            Assert.AreEqual(ConnectorClientOptions.ServiceVersion.V1, options.Version);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("ConnectorApp/1.0")]
        public async Task CallConnectorAsync_ApplicationId_SendsAzureSdkUserAgent(string? applicationId)
        {
            var (credential, options, getLastRequest) = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK));
            options.Diagnostics.ApplicationId = applicationId;
            using var client = new TestBinaryConnectorClient(
                new Uri("https://test.azure.com/connection"),
                credential,
                options);

            await client
                .TestSendBinaryAsync(Array.Empty<byte>(), CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = getLastRequest();
            Assert.IsNotNull(request);
            var userAgent = request.Headers.UserAgent.ToString();
            var expectedUserAgent = new TelemetryDetails(typeof(ConnectorClientOptions).Assembly, applicationId).ToString();
            Assert.AreEqual(expected: expectedUserAgent, actual: userAgent);
            var expectedPrefix = applicationId is null
                ? "azsdk-net-Connectors.Sdk/"
                : $"{applicationId} azsdk-net-Connectors.Sdk/";
            Assert.IsTrue(userAgent.StartsWith(expectedPrefix, StringComparison.Ordinal));
            Console.WriteLine($"User-Agent: {userAgent}");
        }

        [TestMethod]
        public async Task CallConnectorAsync_TelemetryDisabled_OmitsUserAgent()
        {
            var (credential, options, getLastRequest) = ConnectorTestHelpers.CreateCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK));
            options.Diagnostics.IsTelemetryEnabled = false;
            using var client = new TestBinaryConnectorClient(
                new Uri("https://test.azure.com/connection"),
                credential,
                options);

            await client
                .TestSendBinaryAsync(Array.Empty<byte>(), CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            var request = getLastRequest();
            Assert.IsNotNull(request);
            Assert.IsFalse(request.Headers.Contains("User-Agent"));
        }

        [TestMethod]
        public async Task CallConnectorAsync_BinaryBody_SendsRawBytesAndContentType()
        {
            var expectedBody = new byte[] { 0x00, 0x7F, 0xFF };
            var (credential, options, capture) = ConnectorTestHelpers.CreateContentCapturingClientSetup(
                () => new HttpResponseMessage(HttpStatusCode.OK));
            using var client = new TestBinaryConnectorClient(
                new Uri("https://test.azure.com/connection"),
                credential,
                options);

            await client
                .TestSendBinaryAsync(expectedBody, CancellationToken.None)
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual(expected: "application/octet-stream", actual: capture.ContentType);
            CollectionAssert.AreEqual(expected: expectedBody, actual: capture.Body);
        }

        private class TestConnectorClientWithUri : ConnectorClientBase
        {
            public TestConnectorClientWithUri(Uri connectionRuntimeUrl)
                : base(connectionRuntimeUrl)
            {
            }

            public override string ConnectorName => "TestConnector";

            public string TestResolveUrl(string path) => this.ResolveUrl(path);
        }

        private class TestConnectorClientWithUriAndCredential : ConnectorClientBase
        {
            public TestConnectorClientWithUriAndCredential(Uri connectionRuntimeUrl, TokenCredential credential)
                : base(connectionRuntimeUrl, credential)
            {
            }

            public override string ConnectorName => "TestConnector";
        }

        private class TestConnectorClientWithString : ConnectorClientBase
        {
            public TestConnectorClientWithString(string connectionRuntimeUrl)
                : base(connectionRuntimeUrl)
            {
            }

            public override string ConnectorName => "TestConnector";

            public string TestResolveUrl(string path) => this.ResolveUrl(path);
        }

        private class TestBinaryConnectorClient : ConnectorClientBase
        {
            public TestBinaryConnectorClient(Uri connectionRuntimeUrl, TokenCredential credential, ConnectorClientOptions options)
                : base(connectionRuntimeUrl, credential, options)
            {
            }

            public override string ConnectorName => "TestConnector";

            public Task TestSendBinaryAsync(byte[] body, CancellationToken cancellationToken)
            {
                return this.CallConnectorAsync(
                    HttpMethod.Post,
                    "/upload",
                    body,
                    System.Net.Mime.MediaTypeNames.Application.Octet,
                    cancellationToken);
            }
        }
    }
}
