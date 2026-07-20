//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Thrown when a callback does not contain the resource-context headers required to resolve its trigger config.
/// </summary>
/// <remarks>
/// The exception message and properties expose only safe diagnostics: which known resource-identity
/// headers were present and the callback correlation identifier, when available.
/// </remarks>
public sealed class ConnectorTriggerResourceIdentityException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorTriggerResourceIdentityException"/> class.
    /// </summary>
    /// <param name="message">A human-readable message describing the missing or malformed headers.</param>
    /// <param name="presentResourceIdentityHeaderNames">The known resource-identity header names that were present.</param>
    /// <param name="correlationId">The callback correlation identifier, when present.</param>
    internal ConnectorTriggerResourceIdentityException(
        string message,
        IReadOnlyList<string> presentResourceIdentityHeaderNames,
        string? correlationId)
        : base(message)
    {
        this.PresentResourceIdentityHeaderNames = presentResourceIdentityHeaderNames;
        this.CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the names of the known resource-identity headers that were present and non-empty in the callback.
    /// </summary>
    public IReadOnlyList<string> PresentResourceIdentityHeaderNames { get; }

    /// <summary>
    /// Gets the per-request correlation identifier from the callback headers, or <see langword="null"/>
    /// when not present.
    /// </summary>
    public string? CorrelationId { get; }
}
