//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Reflection;
using Azure.Identity;
using Microsoft.Azure.Connectors.Sdk.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ManagedIdentityTokenProviderTests
    {
        [TestMethod]
        public void Constructor_WithNullClientId_ShouldCreateInstance()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: null);

            // Assert
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Constructor_WithNoArgs_ShouldCreateInstance()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider();

            // Assert
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Constructor_WithEmptyClientId_ShouldCreateInstance()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: string.Empty);

            // Assert
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Constructor_WithClientId_ShouldCreateInstance()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: "12345678-1234-1234-1234-123456789012");

            // Assert
            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void Constructor_WithNullClientId_ShouldUseManagedIdentityCredential()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: null);

            // Assert — verify the private _credential field is ManagedIdentityCredential, not DefaultAzureCredential
            var credentialField = typeof(ManagedIdentityTokenProvider)
                .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(credentialField);

            var credential = credentialField.GetValue(provider);
            Assert.IsInstanceOfType<ManagedIdentityCredential>(credential);
        }

        [TestMethod]
        public void Constructor_WithClientId_ShouldUseManagedIdentityCredential()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: "12345678-1234-1234-1234-123456789012");

            // Assert — verify user-assigned path also uses ManagedIdentityCredential
            var credentialField = typeof(ManagedIdentityTokenProvider)
                .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(credentialField);

            var credential = credentialField.GetValue(provider);
            Assert.IsInstanceOfType<ManagedIdentityCredential>(credential);
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_WithNullScopes_ShouldThrowArgumentNullException()
        {
            // Arrange
            var provider = new ManagedIdentityTokenProvider();

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => provider.GetAccessTokenAsync(scopes: null!))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        [TestMethod]
        public async Task GetAccessTokenAsync_WithEmptyScopes_ShouldThrowArgumentException()
        {
            // Arrange
            var provider = new ManagedIdentityTokenProvider();

            // Act & Assert
            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => provider.GetAccessTokenAsync(scopes: Array.Empty<string>()))
                .ConfigureAwait(continueOnCapturedContext: false);

            Assert.AreEqual("scopes", exception.ParamName);
        }

        [TestMethod]
        public void Constructor_Default_ShouldImplementITokenProvider()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider();

            // Assert
            Assert.IsInstanceOfType<ITokenProvider>(provider);
        }
    }
}
