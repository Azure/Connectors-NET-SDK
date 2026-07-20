//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
namespace Azure.Connectors.Sdk;

/// <summary>
/// Thrown when the Connector Namespace trigger configuration resolved for a callback
/// does not match the expected connector and operation identity.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by
/// <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, IConnectorNamespaceTriggerConfigResolver, long, System.Threading.CancellationToken)"/>
/// when the resolved Connector Namespace trigger configuration does not match the
/// <see cref="ConnectorTriggerIdentity"/> supplied by the caller.
/// </para>
/// <para>
/// The exception message and all properties intentionally exclude authorization headers,
/// callback URLs, raw payload content, queue lock tokens, and other secrets. Only expected
/// and resolved identity values, the trigger-config resource identity, and the correlation
/// identifier are exposed for safe diagnostic use.
/// </para>
/// </remarks>
public sealed class ConnectorTriggerIdentityMismatchException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorTriggerIdentityMismatchException"/> class.
    /// </summary>
    /// <param name="message">A human-readable message describing the mismatch.</param>
    /// <param name="expectedConnectorName">The connector name the caller expected.</param>
    /// <param name="expectedOperationName">The operation name the caller expected.</param>
    /// <param name="resolvedConnectorName">
    /// The connector name resolved from the Connector Namespace trigger configuration.
    /// </param>
    /// <param name="resolvedOperationName">
    /// The operation name resolved from the Connector Namespace trigger configuration.
    /// </param>
    /// <param name="resourceIdentity">
    /// The Connector Namespace trigger-config resource identity resolved from the callback headers.
    /// </param>
    /// <param name="correlationId">
    /// The per-request correlation identifier from the callback headers, or <see langword="null"/>
    /// when not present.
    /// </param>
    internal ConnectorTriggerIdentityMismatchException(
        string message,
        string expectedConnectorName,
        string expectedOperationName,
        string resolvedConnectorName,
        string resolvedOperationName,
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        string? correlationId)
        : base(message)
    {
        this.ExpectedConnectorName = expectedConnectorName;
        this.ExpectedOperationName = expectedOperationName;
        this.ResolvedConnectorName = resolvedConnectorName;
        this.ResolvedOperationName = resolvedOperationName;
        this.ResourceIdentity = resourceIdentity;
        this.CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the connector name the caller expected.
    /// </summary>
    public string ExpectedConnectorName { get; }

    /// <summary>
    /// Gets the operation name the caller expected.
    /// </summary>
    public string ExpectedOperationName { get; }

    /// <summary>
    /// Gets the connector name resolved from the Connector Namespace trigger configuration.
    /// </summary>
    public string ResolvedConnectorName { get; }

    /// <summary>
    /// Gets the operation name resolved from the Connector Namespace trigger configuration.
    /// </summary>
    public string ResolvedOperationName { get; }

    /// <summary>
    /// Gets the Connector Namespace trigger-config resource identity resolved from the callback headers.
    /// </summary>
    public ConnectorNamespaceTriggerConfigResourceIdentity ResourceIdentity { get; }

    /// <summary>
    /// Gets the per-request correlation identifier from the callback headers, or <see langword="null"/>
    /// when not present.
    /// </summary>
    public string? CorrelationId { get; }
}
