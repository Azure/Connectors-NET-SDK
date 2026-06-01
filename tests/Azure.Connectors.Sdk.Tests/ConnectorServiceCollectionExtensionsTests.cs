//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Azure.Connectors.Sdk.Arm;
using Azure.Connectors.Sdk.AzureAutomation;
using Azure.Connectors.Sdk.AzureBlob;
using Azure.Connectors.Sdk.AzureDataFactory;
using Azure.Connectors.Sdk.AzureDigitalTwins;
using Azure.Connectors.Sdk.AzureEventGrid;
using Azure.Connectors.Sdk.AzureMonitorLogs;
using Azure.Connectors.Sdk.AzureVM;
using Azure.Connectors.Sdk.Campfire;
using Azure.Connectors.Sdk.ClickSendSms;
using Azure.Connectors.Sdk.CloudmersiveConvert;
using Azure.Connectors.Sdk.Docuware;
using Azure.Connectors.Sdk.ElfsquadData;
using Azure.Connectors.Sdk.Etsy;
using Azure.Connectors.Sdk.ExcelOnline;
using Azure.Connectors.Sdk.FormstackForms;
using Azure.Connectors.Sdk.FreshService;
using Azure.Connectors.Sdk.Impexium;
using Azure.Connectors.Sdk.Infusionsoft;
using Azure.Connectors.Sdk.Insightly;
using Azure.Connectors.Sdk.JedoxOdataHub;
using Azure.Connectors.Sdk.KeyVault;
using Azure.Connectors.Sdk.Kusto;
using Azure.Connectors.Sdk.MeetingRoomMap;
using Azure.Connectors.Sdk.MicrosoftBookings;
using Azure.Connectors.Sdk.Mq;
using Azure.Connectors.Sdk.MsGraphGroupsAndUsers;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Office365Groups;
using Azure.Connectors.Sdk.Office365GroupsMail;
using Azure.Connectors.Sdk.Office365Users;
using Azure.Connectors.Sdk.OneDriveForBusiness;
using Azure.Connectors.Sdk.Onenote;
using Azure.Connectors.Sdk.Orderful;
using Azure.Connectors.Sdk.PdfCo;
using Azure.Connectors.Sdk.Pipedrive;
using Azure.Connectors.Sdk.Planner;
using Azure.Connectors.Sdk.Plivo;
using Azure.Connectors.Sdk.Plumsail;
using Azure.Connectors.Sdk.PowerBI;
using Azure.Connectors.Sdk.Projectplace;
using Azure.Connectors.Sdk.Replicon;
using Azure.Connectors.Sdk.Revai;
using Azure.Connectors.Sdk.SeismicPlanner;
using Azure.Connectors.Sdk.SharePointOnline;
using Azure.Connectors.Sdk.Shifts;
using Azure.Connectors.Sdk.SigningHub;
using Azure.Connectors.Sdk.Smtp;
using Azure.Connectors.Sdk.Starmind;
using Azure.Connectors.Sdk.StarrezRestV1;
using Azure.Connectors.Sdk.Tallyfy;
using Azure.Connectors.Sdk.Teams;
using Azure.Connectors.Sdk.TextRequest;
using Azure.Connectors.Sdk.Ticketmaster;
using Azure.Connectors.Sdk.Todo;
using Azure.Connectors.Sdk.UniversalPrint;
using Azure.Connectors.Sdk.Waywedo;
using Azure.Connectors.Sdk.Wdatp;
using Azure.Connectors.Sdk.Yammer;
using Azure.Connectors.Sdk.ZohoSign;
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
            // Arrange — register all 60 connectors.
            var services = new ServiceCollection();
            var configuration = ConnectorServiceCollectionExtensionsTests.BuildMockConfiguration(
                "https://test.azure.com/apim/connector/abc123");

            services
                .AddArmClient(configuration)
                .AddAzureBlobClient(configuration)
                .AddAzureEventGridClient(configuration)
                .AddAzureMonitorLogsClient(configuration)
                .AddCampfireClient(configuration)
                .AddClickSendSmsClient(configuration)
                .AddCloudmersiveConvertClient(configuration)
                .AddEtsyClient(configuration)
                .AddExcelOnlineClient(configuration)
                .AddFormstackFormsClient(configuration)
                .AddFreshServiceClient(configuration)
                .AddInfusionsoftClient(configuration)
                .AddInsightlyClient(configuration)
                .AddKustoClient(configuration)
                .AddMqClient(configuration)
                .AddMsGraphGroupsAndUsersClient(configuration)
                .AddOffice365Client(configuration)
                .AddOffice365UsersClient(configuration)
                .AddOneDriveForBusinessClient(configuration)
                .AddPipedriveClient(configuration)
                .AddPlivoClient(configuration)
                .AddPlumsailClient(configuration)
                .AddRepliconClient(configuration)
                .AddRevaiClient(configuration)
                .AddSharePointOnlineClient(configuration)
                .AddSigningHubClient(configuration)
                .AddSmtpClient(configuration)
                .AddTeamsClient(configuration)
                .AddUniversalPrintClient(configuration)
                .AddWdatpClient(configuration)
                .AddYammerClient(configuration)
                .AddZohoSignClient(configuration)
                .AddDocuwareClient(configuration)
                .AddElfsquadDataClient(configuration)
                .AddImpexiumClient(configuration)
                .AddJedoxOdataHubClient(configuration)
                .AddMeetingRoomMapClient(configuration)
                .AddOrderfulClient(configuration)
                .AddPdfCoClient(configuration)
                .AddProjectplaceClient(configuration)
                .AddSeismicPlannerClient(configuration)
                .AddStarmindClient(configuration)
                .AddStarrezRestV1Client(configuration)
                .AddTallyfyClient(configuration)
                .AddTextRequestClient(configuration)
                .AddTicketmasterClient(configuration)
                .AddWaywedoClient(configuration)
                .AddAzureAutomationClient(configuration)
                .AddAzureDataFactoryClient(configuration)
                .AddAzureDigitalTwinsClient(configuration)
                .AddAzureVMClient(configuration)
                .AddKeyVaultClient(configuration)
                .AddMicrosoftBookingsClient(configuration)
                .AddOffice365GroupsClient(configuration)
                .AddOffice365GroupsMailClient(configuration)
                .AddOnenoteClient(configuration)
                .AddPlannerClient(configuration)
                .AddPowerBIClient(configuration)
                .AddShiftsClient(configuration)
                .AddTodoClient(configuration);

            var provider = services.BuildServiceProvider();

            // Act & Assert — verify each client resolves with the correct ConnectorName.
            Assert.AreEqual("arm", provider.GetRequiredService<ArmClient>().ConnectorName);
            Assert.AreEqual("azureblob", provider.GetRequiredService<AzureBlobClient>().ConnectorName);
            Assert.AreEqual("azureeventgrid", provider.GetRequiredService<AzureEventGridClient>().ConnectorName);
            Assert.AreEqual("azuremonitorlogs", provider.GetRequiredService<AzureMonitorLogsClient>().ConnectorName);
            Assert.AreEqual("campfire", provider.GetRequiredService<CampfireClient>().ConnectorName);
            Assert.AreEqual("clicksendsms", provider.GetRequiredService<ClickSendSmsClient>().ConnectorName);
            Assert.AreEqual("cloudmersiveconvert", provider.GetRequiredService<CloudmersiveConvertClient>().ConnectorName);
            Assert.AreEqual("docuware", provider.GetRequiredService<DocuwareClient>().ConnectorName);
            Assert.AreEqual("elfsquaddata", provider.GetRequiredService<ElfsquadDataClient>().ConnectorName);
            Assert.AreEqual("etsy", provider.GetRequiredService<EtsyClient>().ConnectorName);
            Assert.AreEqual("excelonline", provider.GetRequiredService<ExcelOnlineClient>().ConnectorName);
            Assert.AreEqual("formstackforms", provider.GetRequiredService<FormstackFormsClient>().ConnectorName);
            Assert.AreEqual("freshservice", provider.GetRequiredService<FreshServiceClient>().ConnectorName);
            Assert.AreEqual("impexium", provider.GetRequiredService<ImpexiumClient>().ConnectorName);
            Assert.AreEqual("infusionsoft", provider.GetRequiredService<InfusionsoftClient>().ConnectorName);
            Assert.AreEqual("insightly", provider.GetRequiredService<InsightlyClient>().ConnectorName);
            Assert.AreEqual("jedoxodatahub", provider.GetRequiredService<JedoxOdataHubClient>().ConnectorName);
            Assert.AreEqual("kusto", provider.GetRequiredService<KustoClient>().ConnectorName);
            Assert.AreEqual("meetingroommap", provider.GetRequiredService<MeetingRoomMapClient>().ConnectorName);
            Assert.AreEqual("mq", provider.GetRequiredService<MqClient>().ConnectorName);
            Assert.AreEqual("msgraphgroupsanduser", provider.GetRequiredService<MsGraphGroupsAndUsersClient>().ConnectorName);
            Assert.AreEqual("office365", provider.GetRequiredService<Office365Client>().ConnectorName);
            Assert.AreEqual("office365users", provider.GetRequiredService<Office365UsersClient>().ConnectorName);
            Assert.AreEqual("onedriveforbusiness", provider.GetRequiredService<OneDriveForBusinessClient>().ConnectorName);
            Assert.AreEqual("orderful", provider.GetRequiredService<OrderfulClient>().ConnectorName);
            Assert.AreEqual("pdfco", provider.GetRequiredService<PdfCoClient>().ConnectorName);
            Assert.AreEqual("pipedrive", provider.GetRequiredService<PipedriveClient>().ConnectorName);
            Assert.AreEqual("plivo", provider.GetRequiredService<PlivoClient>().ConnectorName);
            Assert.AreEqual("plumsail", provider.GetRequiredService<PlumsailClient>().ConnectorName);
            Assert.AreEqual("projectplace", provider.GetRequiredService<ProjectplaceClient>().ConnectorName);
            Assert.AreEqual("replicon", provider.GetRequiredService<RepliconClient>().ConnectorName);
            Assert.AreEqual("revai", provider.GetRequiredService<RevaiClient>().ConnectorName);
            Assert.AreEqual("seismicplanner", provider.GetRequiredService<SeismicPlannerClient>().ConnectorName);
            Assert.AreEqual("sharepointonline", provider.GetRequiredService<SharePointOnlineClient>().ConnectorName);
            Assert.AreEqual("signinghub", provider.GetRequiredService<SigningHubClient>().ConnectorName);
            Assert.AreEqual("smtp", provider.GetRequiredService<SmtpClient>().ConnectorName);
            Assert.AreEqual("starmind", provider.GetRequiredService<StarmindClient>().ConnectorName);
            Assert.AreEqual("starrezrestv1", provider.GetRequiredService<StarrezRestV1Client>().ConnectorName);
            Assert.AreEqual("tallyfy", provider.GetRequiredService<TallyfyClient>().ConnectorName);
            Assert.AreEqual("teams", provider.GetRequiredService<TeamsClient>().ConnectorName);
            Assert.AreEqual("textrequest", provider.GetRequiredService<TextRequestClient>().ConnectorName);
            Assert.AreEqual("ticketmaster", provider.GetRequiredService<TicketmasterClient>().ConnectorName);
            Assert.AreEqual("universalprint", provider.GetRequiredService<UniversalPrintClient>().ConnectorName);
            Assert.AreEqual("waywedo", provider.GetRequiredService<WaywedoClient>().ConnectorName);
            Assert.AreEqual("azureautomation", provider.GetRequiredService<AzureAutomationClient>().ConnectorName);
            Assert.AreEqual("azuredatafactory", provider.GetRequiredService<AzureDataFactoryClient>().ConnectorName);
            Assert.AreEqual("azuredigitaltwins", provider.GetRequiredService<AzureDigitalTwinsClient>().ConnectorName);
            Assert.AreEqual("azurevm", provider.GetRequiredService<AzureVMClient>().ConnectorName);
            Assert.AreEqual("keyvault", provider.GetRequiredService<KeyVaultClient>().ConnectorName);
            Assert.AreEqual("microsoftbookings", provider.GetRequiredService<MicrosoftBookingsClient>().ConnectorName);
            Assert.AreEqual("office365groups", provider.GetRequiredService<Office365GroupsClient>().ConnectorName);
            Assert.AreEqual("office365groupsmail", provider.GetRequiredService<Office365GroupsMailClient>().ConnectorName);
            Assert.AreEqual("onenote", provider.GetRequiredService<OnenoteClient>().ConnectorName);
            Assert.AreEqual("planner", provider.GetRequiredService<PlannerClient>().ConnectorName);
            Assert.AreEqual("powerbi", provider.GetRequiredService<PowerBIClient>().ConnectorName);
            Assert.AreEqual("shifts", provider.GetRequiredService<ShiftsClient>().ConnectorName);
            Assert.AreEqual("todo", provider.GetRequiredService<TodoClient>().ConnectorName);
            Assert.AreEqual("wdatp", provider.GetRequiredService<WdatpClient>().ConnectorName);
            Assert.AreEqual("yammer", provider.GetRequiredService<YammerClient>().ConnectorName);
            Assert.AreEqual("zohosign", provider.GetRequiredService<ZohoSignClient>().ConnectorName);
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
