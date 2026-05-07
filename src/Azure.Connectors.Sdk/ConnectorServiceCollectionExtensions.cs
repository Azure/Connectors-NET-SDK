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
    ///   <item><description>GUID value — user-assigned managed identity with that client ID.</description></item>
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<ArmClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Arm);

                return new ArmClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<AzureblobClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Azureblob);

                return new AzureblobClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<AzuremonitorlogsClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Azuremonitorlogs);

                return new AzuremonitorlogsClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<KustoClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Kusto);

                return new KustoClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<MqClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Mq);

                return new MqClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<MsgraphgroupsanduserClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Msgraphgroupsanduser);

                return new MsgraphgroupsanduserClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<Office365Client>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Office365);

                return new Office365Client(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<Office365usersClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Office365users);

                return new Office365usersClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<OnedriveforbusinessClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Onedriveforbusiness);

                return new OnedriveforbusinessClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<SharepointonlineClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Sharepointonline);

                return new SharepointonlineClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<SmtpClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Smtp);

                return new SmtpClient(connectionRuntimeUrl, credential);
            });

            return services;
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
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurationSection);

            services.AddSingleton<TeamsClient>(serviceProvider =>
            {
                var (connectionRuntimeUrl, credential) = ConnectorServiceCollectionExtensions.ResolveSettings(
                    serviceProvider,
                    configurationSection,
                    connectorName: ConnectorNames.Teams);

                return new TeamsClient(connectionRuntimeUrl, credential);
            });

            return services;
        }

        /// <summary>
        /// Resolves the connection runtime URL and credential from configuration and DI.
        /// </summary>
        private static (Uri ConnectionRuntimeUrl, TokenCredential Credential) ResolveSettings(
            IServiceProvider serviceProvider,
            IConfiguration configurationSection,
            string connectorName)
        {
            var connectionRuntimeUrl = configurationSection[ConnectorServiceCollectionExtensions.ConnectionRuntimeUrlKey];

            if (string.IsNullOrWhiteSpace(connectionRuntimeUrl))
            {
                throw new InvalidOperationException(
                    message: $"Configuration value '{ConnectorServiceCollectionExtensions.ConnectionRuntimeUrlKey}' " +
                             $"is required for the '{connectorName}' connector but was not found in the provided configuration section.");
            }

            var managedIdentityClientId = configurationSection[ConnectorServiceCollectionExtensions.ManagedIdentityClientIdKey];
            var credential = ConnectorServiceCollectionExtensions.ResolveCredential(serviceProvider, managedIdentityClientId);

            return (new Uri(connectionRuntimeUrl), credential);
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
                return string.IsNullOrEmpty(managedIdentityClientId)
                    ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
                    : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));
            }

            return serviceProvider.GetService(typeof(TokenCredential)) as TokenCredential
                ?? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        }
    }
}
