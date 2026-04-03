//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk.Tests
{
    using Microsoft.Azure.Workflows.Connectors.Sdk.Authentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

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
            Assert.ThrowsException<ArgumentNullException>(() => new TestConnectorClient(null!));
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

        private class TestConnectorClient : ConnectorClientBase
        {
            public TestConnectorClient(ITokenProvider tokenProvider)
                : base(tokenProvider)
            {
            }

            public override string ConnectorName => "TestConnector";
        }
    }
}
