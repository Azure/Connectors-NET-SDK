//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Azure.Connectors.Sdk.AzureAutomation;
using global::Azure.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class AzureAutomationClientTests
    {
        private static readonly Mock<TokenCredential> SharedMockCredential = CreateMockCredential();

        private static Mock<TokenCredential> CreateMockCredential()
        {
            var mock = new Mock<TokenCredential>();
            mock.Setup(credential => credential.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
            return mock;
        }

        [TestMethod]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            using var client = new AzureAutomationClient("https://test.azure.com/conn");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithNull_ShouldThrow()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AzureAutomationClient((string)null!));
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            var client = new AzureAutomationClient("https://test.azure.com/conn");
            client.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            var client = new AzureAutomationClient(new Uri("https://test.azure.com/conn"), SharedMockCredential.Object);
            client.Dispose();
            client.Dispose();
        }

    }
}
