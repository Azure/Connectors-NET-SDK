//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorClientBaseTests
    {
        [TestMethod]
        public void Constructor_WithConnectionRuntimeUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            using var client = new TestConnectorClientWithUrl("https://test.azure.com/connection");

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("TestConnector", client.ConnectorName);
        }

        [TestMethod]
        public void Constructor_WithNullConnectionRuntimeUrl_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => new TestConnectorClientWithUrl(null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var client = new TestConnectorClientWithUrl("https://test.azure.com/connection");

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
        public void ResolveUrl_SameHostDifferentScheme_ThrowsInvalidOperation()
        {
            // Arrange
            using var client = new TestConnectorClientWithUrl("https://proxy.azure-apihub.net/apim/arm/conn123");

            // Act & Assert — http instead of https on same host must reject (credential leak risk)
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                client.TestResolveUrl("http://proxy.azure-apihub.net/apim/arm/conn123/subscriptions"));
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
