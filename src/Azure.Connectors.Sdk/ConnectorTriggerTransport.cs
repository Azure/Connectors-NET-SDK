//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace Azure.Connectors.Sdk;

/// <summary>
/// A framework-neutral representation of an incoming trigger callback request, carrying
/// the raw body stream and the HTTP headers needed for identity validation.
/// </summary>
/// <remarks>
/// <para>
/// This type uses only BCL abstractions (<see cref="Stream"/>,
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/>) so that the core SDK has no dependency on
/// Azure Functions, ASP.NET Core, or any other host-specific framework.
/// Host adapters are the caller's responsibility and stay within the application or optional
/// integration packages.
/// </para>
/// <para>
/// Example — adapting an Azure Functions isolated-worker <c>HttpRequestData</c>:
/// </para>
/// <code>
/// var transport = new ConnectorTriggerTransport
/// {
///     Body = request.Body,
///     Headers = request.Headers
///         .ToDictionary(
///             h => h.Key,
///             h => h.Value,
///             StringComparer.OrdinalIgnoreCase)
/// };
/// </code>
/// </remarks>
public sealed class ConnectorTriggerTransport
{
    /// <summary>
    /// Gets the callback body stream. The caller retains ownership; the SDK reads but does not close the stream.
    /// </summary>
    public required Stream Body { get; init; }

    /// <summary>
    /// Gets the request headers used for trigger identity validation.
    /// </summary>
    /// <remarks>
    /// The dictionary should use <see cref="System.StringComparer.OrdinalIgnoreCase"/> for
    /// predictable behavior when callers inspect it directly. The SDK performs its own
    /// <see cref="System.StringComparison.OrdinalIgnoreCase"/> header-name matching even when the
    /// provided dictionary uses a case-sensitive comparer. When not provided, defaults to an empty
    /// case-insensitive dictionary (all resource-context headers will be treated as absent).
    /// </remarks>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; init; }
        = new Dictionary<string, IEnumerable<string>>(System.StringComparer.OrdinalIgnoreCase);
}
