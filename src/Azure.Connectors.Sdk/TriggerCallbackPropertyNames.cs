//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Azure.Connectors.Sdk;

/// <summary>
/// Wire property names shared by the Connector Namespace trigger callback envelope
/// (<see cref="TriggerCallbackPayload{T}"/> / <see cref="TriggerCallbackBody{T}"/>) and the
/// <see cref="ConnectorTriggerPayload"/> readers.
/// </summary>
internal static class TriggerCallbackPropertyNames
{
    /// <summary>
    /// The outer envelope property carrying the trigger body (<c>{"body": ...}</c>).
    /// </summary>
    internal const string Body = "body";

    /// <summary>
    /// The inner body property carrying the batch item array (<c>{"value": [...]}</c>).
    /// </summary>
    internal const string Value = "value";
}
