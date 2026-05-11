//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Azure.Connectors.Sdk.Arm;
using Azure.Connectors.Sdk.AzureBlob;
using Azure.Connectors.Sdk.AzureEventGrid;
using Azure.Connectors.Sdk.AzureMonitorLogs;
using Azure.Connectors.Sdk.Campfire;
using Azure.Connectors.Sdk.ClickSendSms;
using Azure.Connectors.Sdk.CloudmersiveConvert;
using Azure.Connectors.Sdk.Etsy;
using Azure.Connectors.Sdk.ExcelOnline;
using Azure.Connectors.Sdk.FormstackForms;
using Azure.Connectors.Sdk.FreshService;
using Azure.Connectors.Sdk.Infusionsoft;
using Azure.Connectors.Sdk.Insightly;
using Azure.Connectors.Sdk.Kusto;
using Azure.Connectors.Sdk.Mq;
using Azure.Connectors.Sdk.MsGraphGroupsAndUsers;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Office365Users;
using Azure.Connectors.Sdk.OneDriveForBusiness;
using Azure.Connectors.Sdk.Pipedrive;
using Azure.Connectors.Sdk.Plivo;
using Azure.Connectors.Sdk.Plumsail;
using Azure.Connectors.Sdk.Replicon;
using Azure.Connectors.Sdk.Revai;
using Azure.Connectors.Sdk.SharePointOnline;
using Azure.Connectors.Sdk.SigningHub;
using Azure.Connectors.Sdk.Smtp;
using Azure.Connectors.Sdk.Teams;
using Azure.Connectors.Sdk.UniversalPrint;
using Azure.Connectors.Sdk.Wdatp;
using Azure.Connectors.Sdk.Yammer;
using Azure.Connectors.Sdk.ZohoSign;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Connectors.Sdk
{
    /// <summary>
    /// Extension methods for registering connector clients in <see cref="IServiceCollection"/>.
    /// Each method binds connection settings from an <see cref="IConfiguration"/> section
    /// and registers the client as a singleton.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The configuration section must contain a <c>ConnectionRuntimeUrl</c> key.
    /// An optional <c>ManagedIdentityClientId</c> key selects the managed identity:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Not set (null) — resolves <see cref="TokenCredential"/> from DI, or defaults to system-assigned managed identity.</description></item>
    ///   <item><description>Empty string — system-assigned managed identity.</description></item>
    ///   <item><description>Non-empty string — user-assigned managed identity with that client ID.</description></item>
    /// </list>
    /// <para>
    /// Example configuration:
    /// </para>
    /// <code>
    /// {
    ///   "Connectors": {
    ///     "Office365": {
    ///       "ConnectionRuntimeUrl": "https://...azurewebsites.net/..."
    ///     }
    ///   }
    /// }
    /// </code>
    /// <para>
    /// Example registration:
    /// </para>
    /// <code>
    /// services.AddOffice365Client(configuration.GetSection("Connectors:Office365"));
    /// </code>
    /// </remarks>
    public static class ConnectorServiceCollectionExtensions
    {
        private const string ConnectionRuntimeUrlKey = "ConnectionRuntimeUrl";
        private const string ManagedIdentityClientIdKey = "ManagedIdentityClientId";

        /// <summary>
        /// Registers <see cref="ArmClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddArmClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<ArmClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Arm,
                factory: (connectionRuntimeUrl, credential) => new ArmClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="AzureBlobClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddAzureBlobClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<AzureBlobClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.AzureBlob,
                factory: (connectionRuntimeUrl, credential) => new AzureBlobClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="AzureEventGridClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddAzureEventGridClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<AzureEventGridClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.AzureEventGrid,
                factory: (connectionRuntimeUrl, credential) => new AzureEventGridClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="AzureMonitorLogsClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddAzureMonitorLogsClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<AzureMonitorLogsClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.AzureMonitorLogs,
                factory: (connectionRuntimeUrl, credential) => new AzureMonitorLogsClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="CampfireClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddCampfireClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<CampfireClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Campfire,
                factory: (connectionRuntimeUrl, credential) => new CampfireClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="ClickSendSmsClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddClickSendSmsClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<ClickSendSmsClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.ClickSendSms,
                factory: (connectionRuntimeUrl, credential) => new ClickSendSmsClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="CloudmersiveConvertClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddCloudmersiveConvertClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<CloudmersiveConvertClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.CloudmersiveConvert,
                factory: (connectionRuntimeUrl, credential) => new CloudmersiveConvertClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="EtsyClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddEtsyClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<EtsyClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Etsy,
                factory: (connectionRuntimeUrl, credential) => new EtsyClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="ExcelOnlineClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddExcelOnlineClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<ExcelOnlineClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.ExcelOnline,
                factory: (connectionRuntimeUrl, credential) => new ExcelOnlineClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="FormstackFormsClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddFormstackFormsClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<FormstackFormsClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.FormstackForms,
                factory: (connectionRuntimeUrl, credential) => new FormstackFormsClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="FreshServiceClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddFreshServiceClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<FreshServiceClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.FreshService,
                factory: (connectionRuntimeUrl, credential) => new FreshServiceClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="InfusionsoftClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddInfusionsoftClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<InfusionsoftClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Infusionsoft,
                factory: (connectionRuntimeUrl, credential) => new InfusionsoftClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="InsightlyClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddInsightlyClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<InsightlyClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Insightly,
                factory: (connectionRuntimeUrl, credential) => new InsightlyClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="KustoClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddKustoClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<KustoClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Kusto,
                factory: (connectionRuntimeUrl, credential) => new KustoClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="MqClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddMqClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<MqClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Mq,
                factory: (connectionRuntimeUrl, credential) => new MqClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="MsGraphGroupsAndUsersClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddMsGraphGroupsAndUsersClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<MsGraphGroupsAndUsersClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.MsGraphGroupsAndUsers,
                factory: (connectionRuntimeUrl, credential) => new MsGraphGroupsAndUsersClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="Office365Client"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddOffice365Client(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<Office365Client>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Office365,
                factory: (connectionRuntimeUrl, credential) => new Office365Client(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="Office365UsersClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddOffice365UsersClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<Office365UsersClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Office365Users,
                factory: (connectionRuntimeUrl, credential) => new Office365UsersClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="OneDriveForBusinessClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddOneDriveForBusinessClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<OneDriveForBusinessClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.OneDriveForBusiness,
                factory: (connectionRuntimeUrl, credential) => new OneDriveForBusinessClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="PipedriveClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddPipedriveClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<PipedriveClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Pipedrive,
                factory: (connectionRuntimeUrl, credential) => new PipedriveClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="PlivoClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddPlivoClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<PlivoClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Plivo,
                factory: (connectionRuntimeUrl, credential) => new PlivoClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="PlumsailClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddPlumsailClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<PlumsailClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Plumsail,
                factory: (connectionRuntimeUrl, credential) => new PlumsailClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="RepliconClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddRepliconClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<RepliconClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Replicon,
                factory: (connectionRuntimeUrl, credential) => new RepliconClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="RevaiClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddRevaiClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<RevaiClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Revai,
                factory: (connectionRuntimeUrl, credential) => new RevaiClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="SharePointOnlineClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddSharePointOnlineClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<SharePointOnlineClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.SharePointOnline,
                factory: (connectionRuntimeUrl, credential) => new SharePointOnlineClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="SigningHubClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddSigningHubClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<SigningHubClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.SigningHub,
                factory: (connectionRuntimeUrl, credential) => new SigningHubClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="SmtpClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddSmtpClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<SmtpClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Smtp,
                factory: (connectionRuntimeUrl, credential) => new SmtpClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="TeamsClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddTeamsClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<TeamsClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Teams,
                factory: (connectionRuntimeUrl, credential) => new TeamsClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="UniversalPrintClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddUniversalPrintClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<UniversalPrintClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.UniversalPrint,
                factory: (connectionRuntimeUrl, credential) => new UniversalPrintClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="WdatpClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddWdatpClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<WdatpClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Wdatp,
                factory: (connectionRuntimeUrl, credential) => new WdatpClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="YammerClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddYammerClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<YammerClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Yammer,
                factory: (connectionRuntimeUrl, credential) => new YammerClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="ZohoSignClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddZohoSignClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<ZohoSignClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.ZohoSign,
                factory: (connectionRuntimeUrl, credential) => new ZohoSignClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Common helper that validates configuration eagerly and registers a connector client singleton.
        /// </summary>
        private static IServiceCollection AddConnectorClient<TClient>(
            IServiceCollection services,
            IConfiguration configurationSection,
            string connectorName,
            Func<Uri, TokenCredential, TClient> factory)
            where TClient : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            // NOTE(daviburg): Validate ConnectionRuntimeUrl eagerly at registration time so
            // misconfiguration is caught at startup, not at first client resolution.
            var connectionRuntimeUrl = ConnectorServiceCollectionExtensions.ParseConnectionRuntimeUrl(configurationSection, connectorName);
            var managedIdentityClientId = configurationSection[ConnectorServiceCollectionExtensions.ManagedIdentityClientIdKey];

            services.AddSingleton<TClient>(serviceProvider =>
            {
                var credential = ConnectorServiceCollectionExtensions.ResolveCredential(serviceProvider, managedIdentityClientId);
                return factory(connectionRuntimeUrl, credential);
            });

            return services;
        }

        /// <summary>
        /// Parses and validates the connection runtime URL from configuration.
        /// Called at registration time for fail-fast behavior.
        /// </summary>
        private static Uri ParseConnectionRuntimeUrl(IConfiguration configurationSection, string connectorName)
        {
            var connectionRuntimeUrl = configurationSection[ConnectorServiceCollectionExtensions.ConnectionRuntimeUrlKey]?.Trim();

            if (string.IsNullOrWhiteSpace(connectionRuntimeUrl))
            {
                throw new InvalidOperationException(
                    message: $"Configuration value '{ConnectorServiceCollectionExtensions.ConnectionRuntimeUrlKey}' " +
                             $"is required for the '{connectorName}' connector but was missing or empty in the provided configuration section.");
            }

            if (!Uri.TryCreate(connectionRuntimeUrl, UriKind.Absolute, out var parsedUri))
            {
                throw new InvalidOperationException(
                    message: $"Configuration value '{ConnectorServiceCollectionExtensions.ConnectionRuntimeUrlKey}' " +
                             $"for the '{connectorName}' connector is not a valid absolute URI: '{connectionRuntimeUrl}'.");
            }

            return parsedUri;
        }

        /// <summary>
        /// Resolves the <see cref="TokenCredential"/> from configuration and DI.
        /// </summary>
        /// <remarks>
        /// Priority:
        /// <list type="number">
        ///   <item><description>If <paramref name="managedIdentityClientId"/> is non-null, creates a <see cref="ManagedIdentityCredential"/>
        ///   (empty string = system-assigned, GUID = user-assigned).</description></item>
        ///   <item><description>If a <see cref="TokenCredential"/> is registered in DI, uses it.</description></item>
        ///   <item><description>Otherwise, defaults to <see cref="ManagedIdentityCredential"/> with system-assigned identity.</description></item>
        /// </list>
        /// </remarks>
        private static TokenCredential ResolveCredential(
            IServiceProvider serviceProvider,
            string? managedIdentityClientId)
        {
            if (managedIdentityClientId != null)
            {
                var trimmedClientId = managedIdentityClientId.Trim();

                return string.IsNullOrEmpty(trimmedClientId)
                    ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
                    : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(trimmedClientId));
            }

            return serviceProvider.GetService<TokenCredential>()
                ?? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        }
    }
}
