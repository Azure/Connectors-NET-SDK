//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.Azure.Connectors.Sdk.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorClientBaseTests
    {
        [TestMethod]
        public void Constructor_WithValidTokenProvider_ShouldCreateInstance()
        {
            // Arrange
            var tokenProvider = new Mock<ITokenProvider>();

            // Act
            using var client = new TestConnectorClient(tokenProvider.Object);

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("TestConnector", client.ConnectorName);
        }

        [TestMethod]
        public void Constructor_WithNullTokenProvider_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new TestConnectorClient(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var tokenProvider = new Mock<ITokenProvider>();
            var client = new TestConnectorClient(tokenProvider.Object);

            // Act & Assert - should not throw
            client.Dispose();
        }

        [TestMethod]
        public void ResolveUrl_SameHost_ReturnsOriginalUrl()
        {
            // Arrange
            using var client = new TestConnectorClientWithUrl("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act
            var result = client.TestResolveUrl("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions?page=2");

            // Assert
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions?page=2", result);
        }

        [TestMethod]
        public void ResolveUrl_ForeignHost_RewritesThroughProxy()
        {
            // Arrange
            using var client = new TestConnectorClientWithUrl("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act
            var result = client.TestResolveUrl("https://management.azure.com/subscriptions/sub-id/resourceGroups?$skiptoken=abc");

            // Assert — path+query extracted, routed through connection runtime URL
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions/sub-id/resourceGroups?$skiptoken=abc", result);
        }

        [TestMethod]
        public void ResolveUrl_SameHostDifferentScheme_RewritesThroughProxy()
        {
            // Arrange
            using var client = new TestConnectorClientWithUrl("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act — http instead of https on same host must not send credentials over http
            var result = client.TestResolveUrl("http://proxy.azure-apihub.net/apim/arm/conn123/subscriptions");

            // Assert — rewritten through the secure connection runtime URL
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/apim/arm/conn123/subscriptions", result);
        }

        [TestMethod]
        public void ResolveUrl_RelativePath_PrependsConnectionRuntimeUrl()
        {
            // Arrange
            using var client = new TestConnectorClientWithUrl("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act
            var result = client.TestResolveUrl("/subscriptions");

            // Assert
            Assert.AreEqual("https://proxy.azure-apihub.net/apim/arm/conn123/subscriptions", result);
        }

        private class TestConnectorClient : ConnectorClientBase
        {
            public TestConnectorClient(ITokenProvider tokenProvider)
                : base(tokenProvider)
            {
            }

            public override string ConnectorName => "TestConnector";
        }

        private class TestConnectorClientWithUrl : ConnectorClientBase
        {
            public TestConnectorClientWithUrl(string connectionRuntimeUrl)
                : base(connectionRuntimeUrl)
            {
            }

            public override string ConnectorName => "TestConnector";

            public string TestResolveUrl(string path) => this.ResolveUrl(path);
        }
    }
}
