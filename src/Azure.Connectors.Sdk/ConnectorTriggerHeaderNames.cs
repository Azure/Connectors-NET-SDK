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
/// <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, long, System.Threading.CancellationToken)"/>
/// to validate trigger identity before payload deserialization.
/// </para>
/// </remarks>
public static class ConnectorTriggerHeaderNames
{
    /// <summary>
    /// The header that carries the connector's API name (for example <c>office365</c>).
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// Compare against constants from <see cref="ConnectorNames"/>.
    /// </remarks>
    public const string ConnectorName = "x-ms-gateway-resource-name";

    /// <summary>
    /// The header that carries the trigger operation name (for example <c>OnNewEmailV3</c>).
    /// </summary>
    /// <remarks>
    /// Provisional — not yet finalized as a stable Connector Namespace service contract.
    /// Compare against constants from the connector's <c>{Connector}TriggerOperations</c> class.
    /// </remarks>
    public const string OperationName = "x-ms-trigger-name";

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
