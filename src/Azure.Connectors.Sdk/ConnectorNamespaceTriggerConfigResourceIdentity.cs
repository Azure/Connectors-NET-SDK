//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Azure.Connectors.Sdk;

/// <summary>
/// Identifies the Connector Namespace trigger-config resource that delivered a callback.
/// </summary>
/// <param name="SubscriptionId">The Azure subscription identifier.</param>
/// <param name="ResourceGroupName">The Azure resource group name.</param>
/// <param name="ConnectorNamespaceName">The Connector Namespace resource name.</param>
/// <param name="TriggerConfigName">The trigger-config resource name.</param>
public sealed record ConnectorNamespaceTriggerConfigResourceIdentity(
    string SubscriptionId,
    string ResourceGroupName,
    string ConnectorNamespaceName,
    string TriggerConfigName);
