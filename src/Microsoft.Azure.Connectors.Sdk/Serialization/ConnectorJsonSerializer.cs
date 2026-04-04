//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Connectors.Sdk.Serialization;

/// <summary>
/// JSON serializer for connector payloads with standard options.
/// </summary>
public static class ConnectorJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

    /// <summary>
    /// Gets the default serializer options for connector payloads.
    /// </summary>
    public static JsonSerializerOptions Options => DefaultOptions;

    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON string representation.</returns>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized object.</returns>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Deserializes a JSON stream to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The JSON stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized object.</returns>
    public static async ValueTask<T?> DeserializeAsync<T>(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        return await JsonSerializer
            .DeserializeAsync<T>(stream, DefaultOptions, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
