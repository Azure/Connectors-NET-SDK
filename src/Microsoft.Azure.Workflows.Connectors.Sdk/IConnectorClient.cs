//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Workflows.Connectors.Sdk
{
    /// <summary>
    /// Marker interface for connector clients.
    /// </summary>
    public interface IConnectorClient : IDisposable
    {
        /// <summary>
        /// Gets the connector name.
        /// </summary>
        string ConnectorName { get; }
    }
}
