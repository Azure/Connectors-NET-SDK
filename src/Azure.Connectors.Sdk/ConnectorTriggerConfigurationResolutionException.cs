//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Thrown when the SDK cannot resolve the Connector Namespace trigger configuration for a callback.
/// </summary>
/// <remarks>
/// The exception message and properties intentionally exclude authorization headers, callback URLs,
/// response bodies, and other secrets. Only resource identity, status, and correlation diagnostics
/// are exposed.
/// </remarks>
public sealed class ConnectorTriggerConfigurationResolutionException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorTriggerConfigurationResolutionException"/> class.
    /// </summary>
    /// <param name="message">A human-readable message describing the resolution failure.</param>
    /// <param name="resourceIdentity">The Connector Namespace trigger-config resource being resolved.</param>
    /// <param name="status">The HTTP status code returned by the management API, when available.</param>
    /// <param name="correlationId">The callback correlation identifier, when present.</param>
    /// <param name="innerException">The underlying failure, when available.</param>
    internal ConnectorTriggerConfigurationResolutionException(
        string message,
        ConnectorNamespaceTriggerConfigResourceIdentity resourceIdentity,
        int? status,
        string? correlationId,
        Exception? innerException = null)
        : base(message, innerException)
    {
        this.ResourceIdentity = resourceIdentity;
        this.Status = status;
        this.CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the Connector Namespace trigger-config resource identity being resolved.
    /// </summary>
    public ConnectorNamespaceTriggerConfigResourceIdentity ResourceIdentity { get; }

    /// <summary>
    /// Gets the HTTP status code returned by the management API, when available.
    /// </summary>
    public int? Status { get; }

    /// <summary>
    /// Gets the per-request correlation identifier from the callback headers, or <see langword="null"/>
    /// when not present.
    /// </summary>
    public string? CorrelationId { get; }
}
