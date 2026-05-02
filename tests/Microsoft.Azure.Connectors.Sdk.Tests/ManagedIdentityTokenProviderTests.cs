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
        /// <summary>
        /// Extracts the private <c>_credential</c> field from a <see cref="ManagedIdentityTokenProvider"/>
        /// and returns it cast to <see cref="ManagedIdentityCredential"/>.
        /// Only reflects into our own type's private field — not into Azure.Identity internals.
        /// </summary>
        private static ManagedIdentityCredential GetCredential(ManagedIdentityTokenProvider provider)
        {
            var credentialField = typeof(ManagedIdentityTokenProvider)
                .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(credentialField, "Expected private field '_credential' on ManagedIdentityTokenProvider.");
            var credential = credentialField.GetValue(provider);
            Assert.IsNotNull(credential);
            Assert.IsInstanceOfType<ManagedIdentityCredential>(credential);
            return (ManagedIdentityCredential)credential;
        }
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
        public void Constructor_WithEmptyClientId_ShouldUseSystemAssignedIdentity()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: string.Empty);

            // Assert — empty string should produce a ManagedIdentityCredential (same as null)
            var credential = GetCredential(provider);
            Assert.IsNotNull(credential);
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

            // Assert — verify the credential is ManagedIdentityCredential, not DefaultAzureCredential
            var credential = GetCredential(provider);
            Assert.IsNotNull(credential);
        }

        [TestMethod]
        public void Constructor_WithClientId_ShouldUseManagedIdentityCredential()
        {
            // Arrange & Act
            var provider = new ManagedIdentityTokenProvider(clientId: "12345678-1234-1234-1234-123456789012");

            // Assert — verify user-assigned path also uses ManagedIdentityCredential
            var credential = GetCredential(provider);
            Assert.IsNotNull(credential);
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
