//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Teams;
using global::Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Azure.Connectors.Sdk.Tests
{
    /// <summary>
    /// Tests for <see cref="ConnectorServiceCollectionExtensions"/>.
    /// </summary>
    [TestClass]
    public class ConnectorServiceCollectionExtensionsTests
    {
        private const string TestConnectionRuntimeUrl = "https://test.azure.com/apim/office365/abc123";

        [TestMethod]
        public void AddOffice365Client_WithValidConfig_RegistersSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl);

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();
            var client = provider.GetService<Office365Client>();

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("office365", client.ConnectorName);
        }

        [TestMethod]
        public void AddOffice365Client_ReturnsSameInstance_WhenResolvedTwice()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl);

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();
            var client1 = provider.GetRequiredService<Office365Client>();
            var client2 = provider.GetRequiredService<Office365Client>();

            // Assert
            Assert.AreSame(client1, client2);
        }

        [TestMethod]
        public void AddTeamsClient_WithValidConfig_RegistersSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                "https://test.azure.com/apim/teams/abc123");

            // Act
            services.AddTeamsClient(configuration);
            var provider = services.BuildServiceProvider();
            var client = provider.GetService<TeamsClient>();

            // Assert
            Assert.IsNotNull(client);
            Assert.AreEqual("teams", client.ConnectorName);
        }

        [TestMethod]
        public void AddOffice365Client_WithMissingConnectionRuntimeUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                connectionRuntimeUrl: null);

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();

            // Assert
            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => provider.GetRequiredService<Office365Client>());
            StringAssert.Contains(exception.Message, "ConnectionRuntimeUrl");
            StringAssert.Contains(exception.Message, "office365");
        }

        [TestMethod]
        public void AddOffice365Client_WithEmptyConnectionRuntimeUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                connectionRuntimeUrl: "   ");

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();

            // Assert
            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => provider.GetRequiredService<Office365Client>());
            StringAssert.Contains(exception.Message, "ConnectionRuntimeUrl");
        }

        [TestMethod]
        public void AddOffice365Client_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection services = null!;
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl);

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => services.AddOffice365Client(configuration));
        }

        [TestMethod]
        public void AddOffice365Client_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => services.AddOffice365Client(null!));
        }

        [TestMethod]
        public void AddOffice365Client_UsesTokenCredentialFromDI_WhenRegistered()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockCredential = new Mock<TokenCredential>();
            services.AddSingleton<TokenCredential>(mockCredential.Object);

            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl);

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<Office365Client>();

            // Assert — client was created successfully using the DI credential.
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void AddOffice365Client_WithManagedIdentityClientId_CreatesClient()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl,
                managedIdentityClientId: "12345678-1234-1234-1234-123456789012");

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<Office365Client>();

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void AddOffice365Client_WithEmptyManagedIdentityClientId_UsesSystemAssigned()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl,
                managedIdentityClientId: "");

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<Office365Client>();

            // Assert
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void AddOffice365Client_ReturnsServiceCollection_ForChaining()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl);

            // Act
            var result = services.AddOffice365Client(configuration);

            // Assert
            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void MultipleConnectors_CanBeRegisteredTogether()
        {
            // Arrange
            var services = new ServiceCollection();
            var office365Config = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                "https://test.azure.com/apim/office365/abc123");
            var teamsConfig = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                "https://test.azure.com/apim/teams/abc123");

            // Act
            services
                .AddOffice365Client(office365Config)
                .AddTeamsClient(teamsConfig);

            var provider = services.BuildServiceProvider();
            var office365Client = provider.GetRequiredService<Office365Client>();
            var teamsClient = provider.GetRequiredService<TeamsClient>();

            // Assert
            Assert.IsNotNull(office365Client);
            Assert.IsNotNull(teamsClient);
            Assert.AreEqual("office365", office365Client.ConnectorName);
            Assert.AreEqual("teams", teamsClient.ConnectorName);
        }

        private static IConfiguration BuildMockConfiguration(
            string? connectionRuntimeUrl,
            string? managedIdentityClientId = null)
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(config => config["ConnectionRuntimeUrl"]).Returns(connectionRuntimeUrl!);
            mockConfig.Setup(config => config["ManagedIdentityClientId"]).Returns(managedIdentityClientId!);
            return mockConfig.Object;
        }
    }
}
