//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Represents connection settings resolved from Azure Functions configuration.
    /// Settings use the <c>__</c> separator convention (e.g., <c>Office365Connection__aiGatewayName</c>).
    /// </summary>
    public class ConnectorConnectionOptions
    {
        /// <summary>
        /// Gets or sets the AI Gateway name (Format A).
        /// Resolved from <c>{connectionSettingName}__aiGatewayName</c> app setting.
        /// </summary>
        public string? AiGatewayName { get; set; }

        /// <summary>
        /// Gets or sets the connection name within the AI Gateway (Format A).
        /// Resolved from <c>{connectionSettingName}__connectionName</c> app setting.
        /// </summary>
        public string? ConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the direct connection runtime URL (Format B).
        /// Resolved from <c>{connectionSettingName}__connectionRuntimeUrl</c> app setting.
        /// </summary>
        public string? ConnectionRuntimeUrl { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is an AI Gateway connection (Format A).
        /// True when both <see cref="AiGatewayName"/> and <see cref="ConnectionName"/> are set.
        /// </summary>
        public bool IsAiGatewayConnection =>
            !string.IsNullOrWhiteSpace(this.AiGatewayName) &&
            !string.IsNullOrWhiteSpace(this.ConnectionName);

        /// <summary>
        /// Gets a value indicating whether this is a direct connection (Format B).
        /// True when <see cref="ConnectionRuntimeUrl"/> is set.
        /// </summary>
        public bool IsDirectConnection =>
            !string.IsNullOrWhiteSpace(this.ConnectionRuntimeUrl);
    }
}
