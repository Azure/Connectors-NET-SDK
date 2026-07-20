//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using global::Azure.Core;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Options for <see cref="ConnectorNamespaceTriggerConfigManagementResolver"/>.
/// </summary>
public sealed class ConnectorNamespaceTriggerConfigManagementResolverOptions : ClientOptions
{
    /// <summary>
    /// The default Connector Namespace management API version.
    /// </summary>
    public const string DefaultApiVersion = "2026-05-01-preview";

    /// <summary>
    /// The default ARM management endpoint.
    /// </summary>
    public static readonly Uri DefaultManagementEndpoint = new("https://management.azure.com");

    /// <summary>
    /// The default token audience used to acquire management-plane access tokens.
    /// </summary>
    public const string DefaultAudience = "https://management.azure.com";

    /// <summary>
    /// Gets or sets the management endpoint used for trigger-config GET requests.
    /// </summary>
    public Uri ManagementEndpoint { get; set; } = ConnectorNamespaceTriggerConfigManagementResolverOptions.DefaultManagementEndpoint;

    /// <summary>
    /// Gets or sets the token audience used to acquire management-plane access tokens.
    /// </summary>
    public string Audience { get; set; } = ConnectorNamespaceTriggerConfigManagementResolverOptions.DefaultAudience;

    /// <summary>
    /// Gets or sets the Connector Namespace management API version.
    /// </summary>
    public string ApiVersion { get; set; } = ConnectorNamespaceTriggerConfigManagementResolverOptions.DefaultApiVersion;
}
