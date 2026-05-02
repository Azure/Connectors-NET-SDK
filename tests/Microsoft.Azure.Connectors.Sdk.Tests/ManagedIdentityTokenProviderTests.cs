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
        /// Extracts the <c>ManagedIdentityId.ToString()</c> value from a <see cref="ManagedIdentityTokenProvider"/>
        /// by reflecting into its private <c>_credential</c> field and the credential's internal
        /// <c>Client.ManagedIdentityId</c> property. Returns values like <c>"SystemAssigned"</c>
        /// or <c>"ClientId {clientId}"</c>.
        /// </summary>
        private static string GetManagedIdentityIdString(ManagedIdentityTokenProvider provider)
        {
            // ManagedIdentityTokenProvider._credential (ManagedIdentityCredential)
            var credentialField = typeof(ManagedIdentityTokenProvider)
                .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(credentialField, "Expected private field '_credential' on ManagedIdentityTokenProvider.");
            var credential = credentialField.GetValue(provider);
            Assert.IsNotNull(credential);
            Assert.IsInstanceOfType<ManagedIdentityCredential>(credential);

            // ManagedIdentityCredential.<Client>k__BackingField (ManagedIdentityClient)
            var clientProperty = credential.GetType()
                .GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clientProperty, "Expected internal property 'Client' on ManagedIdentityCredential.");
            var client = clientProperty.GetValue(credential);
            Assert.IsNotNull(client);

            // ManagedIdentityClient.<ManagedIdentityId>k__BackingField (ManagedIdentityId)
            var identityIdProperty = client.GetType()
                .GetProperty("ManagedIdentityId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(identityIdProperty, "Expected property 'ManagedIdentityId' on ManagedIdentityClient.");
            var identityId = identityIdProperty.GetValue(client);
            Assert.IsNotNull(identityId);

            return identityId.ToString()!;
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

            // Assert — empty string should be treated the same as null (system-assigned)
            var identityIdString = GetManagedIdentityIdString(provider);
            Assert.AreEqual("SystemAssigned", identityIdString);
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

            // Assert — verify the credential is ManagedIdentityCredential with system-assigned identity
            var identityIdString = GetManagedIdentityIdString(provider);
            Assert.AreEqual("SystemAssigned", identityIdString);
        }

        [TestMethod]
        public void Constructor_WithClientId_ShouldUseManagedIdentityCredential()
        {
            // Arrange & Act
            const string clientId = "12345678-1234-1234-1234-123456789012";
            var provider = new ManagedIdentityTokenProvider(clientId: clientId);

            // Assert — verify user-assigned path stores the provided client ID
            var identityIdString = GetManagedIdentityIdString(provider);
            Assert.AreEqual($"ClientId {clientId}", identityIdString);
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
