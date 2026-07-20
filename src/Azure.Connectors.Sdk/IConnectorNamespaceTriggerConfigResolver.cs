//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Resolves authoritative Connector Namespace trigger configuration for a callback.
/// </summary>
public interface IConnectorNamespaceTriggerConfigResolver
{
    /// <summary>
    /// Retrieves the trigger configuration for the specified Connector Namespace trigger-config resource.
    /// </summary>
    /// <param name="resourceIdentity">The resource identity resolved from the callback headers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved connector and operation identity from the trigger configuration.</returns>
    ValueTask<ConnectorNamespaceTriggerConfig> GetTriggerConfigAsync(
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        CancellationToken cancellationToken = default);
}
