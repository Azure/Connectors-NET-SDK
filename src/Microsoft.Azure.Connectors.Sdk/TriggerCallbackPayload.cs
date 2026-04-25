//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Connectors.Sdk;

/// <summary>
/// Envelope type for Connector Gateway trigger callback payloads.
/// The Connector Gateway wraps triggerBody() in a <c>{"body":{"value":[...]}}</c> structure.
/// </summary>
/// <typeparam name="T">The connector-specific trigger item type (e.g., <c>GraphClientReceiveMessage</c> for Office 365 email triggers).</typeparam>
public class TriggerCallbackPayload<T>
{
    /// <summary>
    /// The body envelope containing the trigger items.
    /// </summary>
    [JsonPropertyName("body")]
    public TriggerCallbackBody<T>? Body { get; set; }
}

/// <summary>
/// Inner body of the Connector Gateway trigger callback, containing the array of trigger items.
/// </summary>
/// <typeparam name="T">The connector-specific trigger item type.</typeparam>
public class TriggerCallbackBody<T>
{
    /// <summary>
    /// The list of trigger items delivered by the connector trigger.
    /// Split-on is not supported — consumers must iterate this array.
    /// </summary>
    [JsonPropertyName("value")]
    public List<T>? Value { get; set; }
}
