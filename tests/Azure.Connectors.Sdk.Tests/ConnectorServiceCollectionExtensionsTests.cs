//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Azure.Connectors.Sdk.Arm;
using Azure.Connectors.Sdk.AzureBlob;
using Azure.Connectors.Sdk.AzureMonitorLogs;
using Azure.Connectors.Sdk.Kusto;
using Azure.Connectors.Sdk.Mq;
using Azure.Connectors.Sdk.MsGraphGroupsAndUsers;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Office365Users;
using Azure.Connectors.Sdk.OneDriveForBusiness;
using Azure.Connectors.Sdk.SharePointOnline;
using Azure.Connectors.Sdk.Smtp;
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

            // Act & Assert — validation now happens eagerly at registration time.
            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => services.AddOffice365Client(configuration));
            StringAssert.Contains(exception.Message, "ConnectionRuntimeUrl");
            StringAssert.Contains(exception.Message, "office365");
            StringAssert.Contains(exception.Message, "missing or empty");
        }

        [TestMethod]
        public void AddOffice365Client_WithEmptyConnectionRuntimeUrl_ThrowsInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                connectionRuntimeUrl: "   ");

            // Act & Assert — validation now happens eagerly at registration time.
            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => services.AddOffice365Client(configuration));
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

        [TestMethod]
        public void AddOffice365Client_WithInvalidUri_ThrowsInvalidOperationException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                connectionRuntimeUrl: "not-a-valid-uri");

            // Act & Assert — validation now happens eagerly at registration time.
            var exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => services.AddOffice365Client(configuration));
            StringAssert.Contains(exception.Message, "not a valid absolute URI");
            StringAssert.Contains(exception.Message, "office365");
        }

        [TestMethod]
        public void AddOffice365Client_WithWhitespaceManagedIdentityClientId_TreatsAsSystemAssigned()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                ConnectorServiceCollectionExtensionsTests.TestConnectionRuntimeUrl,
                managedIdentityClientId: "   ");

            // Act
            services.AddOffice365Client(configuration);
            var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<Office365Client>();

            // Assert — whitespace should be treated as empty (system-assigned).
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void AllConnectorExtensionMethods_RegisterCorrectClientAndConnectorName()
        {
            // Arrange — register all 12 connectors.
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                "https://test.azure.com/apim/connector/abc123");

            services
                .AddArmClient(configuration)
                .AddAzureBlobClient(configuration)
                .AddAzureMonitorLogsClient(configuration)
                .AddKustoClient(configuration)
                .AddMqClient(configuration)
                .AddMsGraphGroupsAndUsersClient(configuration)
                .AddOffice365Client(configuration)
                .AddOffice365UsersClient(configuration)
                .AddOneDriveForBusinessClient(configuration)
                .AddSharePointOnlineClient(configuration)
                .AddSmtpClient(configuration)
                .AddTeamsClient(configuration);

            var provider = services.BuildServiceProvider();

            // Act & Assert — verify each client resolves with the correct ConnectorName.
            Assert.AreEqual("arm", provider.GetRequiredService<ArmClient>().ConnectorName);
            Assert.AreEqual("azureblob", provider.GetRequiredService<AzureBlobClient>().ConnectorName);
            Assert.AreEqual("azuremonitorlogs", provider.GetRequiredService<AzureMonitorLogsClient>().ConnectorName);
            Assert.AreEqual("kusto", provider.GetRequiredService<KustoClient>().ConnectorName);
            Assert.AreEqual("mq", provider.GetRequiredService<MqClient>().ConnectorName);
            Assert.AreEqual("msgraphgroupsanduser", provider.GetRequiredService<MsGraphGroupsAndUsersClient>().ConnectorName);
            Assert.AreEqual("office365", provider.GetRequiredService<Office365Client>().ConnectorName);
            Assert.AreEqual("office365users", provider.GetRequiredService<Office365UsersClient>().ConnectorName);
            Assert.AreEqual("onedriveforbusiness", provider.GetRequiredService<OneDriveForBusinessClient>().ConnectorName);
            Assert.AreEqual("sharepointonline", provider.GetRequiredService<SharePointOnlineClient>().ConnectorName);
            Assert.AreEqual("smtp", provider.GetRequiredService<SmtpClient>().ConnectorName);
            Assert.AreEqual("teams", provider.GetRequiredService<TeamsClient>().ConnectorName);
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
