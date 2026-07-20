//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Thrown when the Connector Namespace trigger identity headers delivered with a callback
/// do not match the expected connector and operation identity.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by
/// <see cref="ConnectorTriggerPayload.ReadAsync{TPayload}(ConnectorTriggerTransport, ConnectorTriggerIdentity, long, System.Threading.CancellationToken)"/>
/// when one or more identity headers are absent or their values do not match the
/// <see cref="ConnectorTriggerIdentity"/> supplied by the caller.
/// </para>
/// <para>
/// The exception message and all properties intentionally exclude authorization headers,
/// callback URLs, raw payload content, queue lock tokens, and other secrets. Only expected
/// and actual identity values, the names of present identity headers, and the correlation
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
    /// <param name="actualConnectorName">
    /// The connector name read from the callback headers, or <see langword="null"/> when the header
    /// was absent or carried an empty value.
    /// </param>
    /// <param name="actualOperationName">
    /// The operation name read from the callback headers, or <see langword="null"/> when the header
    /// was absent or carried an empty value.
    /// </param>
    /// <param name="presentIdentityHeaderNames">
    /// The names of the known identity headers that were present (and non-empty) in the callback.
    /// </param>
    /// <param name="correlationId">
    /// The per-request correlation identifier from the callback headers, or <see langword="null"/>
    /// when not present.
    /// </param>
    internal ConnectorTriggerIdentityMismatchException(
        string message,
        string expectedConnectorName,
        string expectedOperationName,
        string? actualConnectorName,
        string? actualOperationName,
        IReadOnlyCollection<string> presentIdentityHeaderNames,
        string? correlationId)
        : base(message)
    {
        this.ExpectedConnectorName = expectedConnectorName;
        this.ExpectedOperationName = expectedOperationName;
        this.ActualConnectorName = actualConnectorName;
        this.ActualOperationName = actualOperationName;
        this.PresentIdentityHeaderNames = presentIdentityHeaderNames;
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
    /// Gets the connector name read from the callback headers, or <see langword="null"/> when the
    /// header was absent or carried an empty value.
    /// </summary>
    public string? ActualConnectorName { get; }

    /// <summary>
    /// Gets the operation name read from the callback headers, or <see langword="null"/> when the
    /// header was absent or carried an empty value.
    /// </summary>
    public string? ActualOperationName { get; }

    /// <summary>
    /// Gets the names of the known identity headers that were present (and non-empty) in the callback.
    /// Use this to distinguish a fully-absent set of headers (possible routing mistake or non-Connector
    /// Namespace caller) from headers that were delivered but carried unexpected values.
    /// </summary>
    public IReadOnlyCollection<string> PresentIdentityHeaderNames { get; }

    /// <summary>
    /// Gets the per-request correlation identifier from the callback headers, or <see langword="null"/>
    /// when not present.
    /// </summary>
    public string? CorrelationId { get; }
}
