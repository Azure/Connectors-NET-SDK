//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Connectors.Sdk;

/// <summary>
/// Envelope type for Connector Namespace trigger callback payloads.
/// The Connector Namespace delivers callbacks in two shapes depending on the trigger
/// configuration's splitOn setting:
/// <list type="bullet">
///   <item><description>Batch (splitOn disabled): <c>{"body":{"value":[...items...]}}</c></description></item>
///   <item><description>Single-item (splitOn enabled): <c>{"body":{...item...}}</c></description></item>
/// </list>
/// The <see cref="TriggerCallbackBodyConverterFactory"/> handles both shapes transparently,
/// always normalizing into <see cref="TriggerCallbackBody{T}.Value"/> as a list.
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
/// Inner body of the Connector Namespace trigger callback, containing the array of trigger items.
/// Deserialization is handled by <see cref="TriggerCallbackBodyConverterFactory"/> which accepts
/// both batch (<c>{"value":[...]}</c>) and single-item (<c>{...item...}</c>) JSON shapes.
/// </summary>
/// <typeparam name="T">The connector-specific trigger item type.</typeparam>
[JsonConverter(typeof(TriggerCallbackBodyConverterFactory))]
public class TriggerCallbackBody<T>
{
    /// <summary>
    /// The list of trigger items delivered by the connector trigger.
    /// Always a list regardless of whether the callback was batch or single-item.
    /// </summary>
    [JsonPropertyName("value")]
    public List<T>? Value { get; set; }
}

/// <summary>
/// Factory that creates <see cref="TriggerCallbackBodyConverter{T}"/> instances for any
/// <see cref="TriggerCallbackBody{T}"/> closed generic type.
/// </summary>
internal sealed class TriggerCallbackBodyConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(TriggerCallbackBody<>);
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type itemType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(TriggerCallbackBodyConverter<>).MakeGenericType(itemType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

/// <summary>
/// Deserializes <see cref="TriggerCallbackBody{T}"/> from either the batch shape
/// (<c>{"value":[...items...]}</c>) or the single-item shape (<c>{...item...}</c>)
/// that the Connector Namespace delivers depending on the trigger configuration's
/// splitOn setting.
/// </summary>
/// <typeparam name="T">The connector-specific trigger item type.</typeparam>
internal sealed class TriggerCallbackBodyConverter<T> : JsonConverter<TriggerCallbackBody<T>>
{
    /// <inheritdoc/>
    public override TriggerCallbackBody<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject or Null for TriggerCallbackBody<{typeof(T).Name}>, got {reader.TokenType}.");
        }

        // Parse into a JsonDocument to inspect the structure without consuming the reader position.
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("value", out JsonElement valueElement) && valueElement.ValueKind == JsonValueKind.Array)
        {
            // Batch shape: {"value":[...items...]}
            List<T>? items = valueElement.Deserialize<List<T>>(options);
            return new TriggerCallbackBody<T> { Value = items };
        }

        // Single-item shape: {...item properties...} — wrap in a one-element list.
        T? singleItem = root.Deserialize<T>(options);
        return new TriggerCallbackBody<T>
        {
            Value = singleItem is not null ? new List<T> { singleItem } : new List<T>()
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TriggerCallbackBody<T> value, JsonSerializerOptions options)
    {
        // Always serialize in batch shape for consistency.
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        JsonSerializer.Serialize(writer, value.Value, options);
        writer.WriteEndObject();
    }
}
