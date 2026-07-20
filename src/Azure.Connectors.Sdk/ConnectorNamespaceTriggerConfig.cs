//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Azure.Connectors.Sdk;

/// <summary>
/// The authoritative connector and operation identity resolved from a Connector Namespace trigger config.
/// </summary>
/// <param name="ConnectorName">The connector API name (for example <c>office365</c>).</param>
/// <param name="OperationName">The trigger operation name (for example <c>OnNewEmailV3</c>).</param>
public sealed record ConnectorNamespaceTriggerConfig(string ConnectorName, string OperationName);
