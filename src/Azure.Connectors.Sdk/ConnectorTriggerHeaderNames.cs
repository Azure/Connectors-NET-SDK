//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Azure.Connectors.Sdk;

/// <summary>
/// HTTP header name constants used by the Connector Namespace service when delivering
/// trigger callbacks to application endpoints.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Important — provisional service contract:</strong> The header names defined here
/// reflect the current Connector Namespace webhook implementation. They have not yet been
/// formally agreed as a stable, versioned API surface with the Connector Namespace service team.
/// Do not treat these constants as immutable across SDK versions. The service team will document
/// a finalized, versioned header contract before identity validation is enabled by default.
/// </para>
/// <para>
/// Use these constants with
/// <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, IConnectorNamespaceTriggerConfigResolver, long, System.Threading.CancellationToken)"/>
/// to build the Connector Namespace trigger-config resource identity before payload deserialization.
/// </para>
/// </remarks>
public static class ConnectorTriggerHeaderNames
{
    /// <summary>
    /// The header that carries the Azure subscription identifier that owns the Connector Namespace resource.
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// </remarks>
    public const string SubscriptionId = "x-ms-subscription-id";

    /// <summary>
    /// The header that carries the Azure resource group name that owns the Connector Namespace resource.
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// </remarks>
    public const string ResourceGroupName = "x-ms-resource-group";

    /// <summary>
    /// The header that carries the Connector Namespace resource name.
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// This is the Connector Namespace resource name, not the connector API name.
    /// </remarks>
    public const string ConnectorNamespaceName = "x-ms-gateway-resource-name";

    /// <summary>
    /// The header that carries the trigger-config resource name.
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// This is the trigger-config resource name, not the Swagger trigger operation name.
    /// </remarks>
    public const string TriggerConfigName = "x-ms-trigger-name";

    /// <summary>
    /// The header that carries the per-request correlation identifier, when present.
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// When present, this value is included in <see cref="ConnectorTriggerIdentityMismatchException.CorrelationId"/>
    /// to assist with call tracing.
    /// </remarks>
    public const string CorrelationId = "x-ms-client-request-id";
}
