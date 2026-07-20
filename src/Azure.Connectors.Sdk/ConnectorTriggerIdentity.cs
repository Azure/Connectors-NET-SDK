//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Azure.Connectors.Sdk;

/// <summary>
/// Identifies the expected connector and operation for a trigger callback.
/// </summary>
/// <param name="ConnectorName">
/// The connector API name (for example <c>office365</c>).
/// Use constants from <see cref="ConnectorNames"/> for IntelliSense and compile-time validation.
/// </param>
/// <param name="OperationName">
/// The trigger operation name (for example <c>OnNewEmailV3</c>).
/// Use constants from the connector's <c>{Connector}TriggerOperations</c> class.
/// </param>
/// <remarks>
/// Pass this to
/// <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, IConnectorNamespaceTriggerConfigResolver, long, System.Threading.CancellationToken)"/>
/// to validate that the resolved trigger configuration matches the expected connector trigger before
/// payload deserialization.
/// </remarks>
public sealed record ConnectorTriggerIdentity(string ConnectorName, string OperationName);
