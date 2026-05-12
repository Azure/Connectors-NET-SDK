//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Azure.Connectors.Sdk
{
    /// <summary>
    /// Resolves connector connection settings from Azure Functions configuration.
    /// Reads settings using the <c>__</c> separator convention.
    /// </summary>
    public static class ConnectorConnectionResolver
    {
        private const string ConnectorGatewayNameSuffix = "__connectorGatewayName";
        private const string ConnectionNameSuffix = "__connectionName";
        private const string ConnectionRuntimeUrlSuffix = "__connectionRuntimeUrl";

        /// <summary>
        /// Resolves connection options from environment variables.
        /// </summary>
        /// <param name="connectionSettingName">The connection setting key name (e.g., "Office365Connection").</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionSettingName"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="connectionSettingName"/> is empty, whitespace, or when no valid connection format is found.</exception>
        public static ConnectorConnectionOptions Resolve(string connectionSettingName)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionSettingName);

            if (string.IsNullOrWhiteSpace(connectionSettingName))
            {
                throw new ArgumentException(
                    message: "Parameter 'connectionSettingName' cannot be whitespace.",
                    paramName: nameof(connectionSettingName));
            }

            var options = new ConnectorConnectionOptions
            {
                ConnectorGatewayName = Environment.GetEnvironmentVariable(
                    connectionSettingName + ConnectorConnectionResolver.ConnectorGatewayNameSuffix),
                ConnectionName = Environment.GetEnvironmentVariable(
                    connectionSettingName + ConnectorConnectionResolver.ConnectionNameSuffix),
                ConnectionRuntimeUrl = Environment.GetEnvironmentVariable(
                    connectionSettingName + ConnectorConnectionResolver.ConnectionRuntimeUrlSuffix),
            };

            if (!options.IsConnectorGatewayConnection && !options.IsDirectConnection)
            {
                // NOTE(daviburg): Differentiate between no settings and partial settings for better diagnostics.
                var hasPartialConnectorGateway =
                    !string.IsNullOrWhiteSpace(options.ConnectorGatewayName) ||
                    !string.IsNullOrWhiteSpace(options.ConnectionName);

                var message = hasPartialConnectorGateway
                    ? $"Partial Connector Namespace connection settings found for '{connectionSettingName}'. " +
                      $"Both '{connectionSettingName}{ConnectorConnectionResolver.ConnectorGatewayNameSuffix}' and " +
                      $"'{connectionSettingName}{ConnectorConnectionResolver.ConnectionNameSuffix}' must be set together."
                    : $"No connection settings found for '{connectionSettingName}'. " +
                      $"Expected either '{connectionSettingName}{ConnectorConnectionResolver.ConnectorGatewayNameSuffix}' + " +
                      $"'{connectionSettingName}{ConnectorConnectionResolver.ConnectionNameSuffix}' " +
                      $"(Connector Namespace connection) or " +
                      $"'{connectionSettingName}{ConnectorConnectionResolver.ConnectionRuntimeUrlSuffix}' (direct connection).";

                throw new ArgumentException(
                    message: message,
                    paramName: nameof(connectionSettingName));
            }

            return options;
        }
    }
}
