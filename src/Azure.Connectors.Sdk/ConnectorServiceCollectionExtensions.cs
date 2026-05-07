//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Azure.Connectors.Sdk.Arm;
using Azure.Connectors.Sdk.Azureblob;
using Azure.Connectors.Sdk.Azuremonitorlogs;
using Azure.Connectors.Sdk.Kusto;
using Azure.Connectors.Sdk.Mq;
using Azure.Connectors.Sdk.Msgraphgroupsanduser;
using Azure.Connectors.Sdk.Office365;
using Azure.Connectors.Sdk.Office365users;
using Azure.Connectors.Sdk.Onedriveforbusiness;
using Azure.Connectors.Sdk.Sharepointonline;
using Azure.Connectors.Sdk.Smtp;
using Azure.Connectors.Sdk.Teams;
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
        /// Registers <see cref="AzureblobClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddAzureblobClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<AzureblobClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Azureblob,
                factory: (connectionRuntimeUrl, credential) => new AzureblobClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="AzuremonitorlogsClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddAzuremonitorlogsClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<AzuremonitorlogsClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Azuremonitorlogs,
                factory: (connectionRuntimeUrl, credential) => new AzuremonitorlogsClient(connectionRuntimeUrl, credential));
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
        /// Registers <see cref="MsgraphgroupsanduserClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddMsgraphgroupsanduserClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<MsgraphgroupsanduserClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Msgraphgroupsanduser,
                factory: (connectionRuntimeUrl, credential) => new MsgraphgroupsanduserClient(connectionRuntimeUrl, credential));
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
        /// Registers <see cref="Office365usersClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddOffice365usersClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<Office365usersClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Office365users,
                factory: (connectionRuntimeUrl, credential) => new Office365usersClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="OnedriveforbusinessClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddOnedriveforbusinessClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<OnedriveforbusinessClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Onedriveforbusiness,
                factory: (connectionRuntimeUrl, credential) => new OnedriveforbusinessClient(connectionRuntimeUrl, credential));
        }

        /// <summary>
        /// Registers <see cref="SharepointonlineClient"/> as a singleton using connection settings from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">Configuration section containing <c>ConnectionRuntimeUrl</c> and optional <c>ManagedIdentityClientId</c>.</param>
        public static IServiceCollection AddSharepointonlineClient(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            return ConnectorServiceCollectionExtensions.AddConnectorClient<SharepointonlineClient>(
                services,
                configurationSection,
                connectorName: ConnectorNames.Sharepointonline,
                factory: (connectionRuntimeUrl, credential) => new SharepointonlineClient(connectionRuntimeUrl, credential));
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
