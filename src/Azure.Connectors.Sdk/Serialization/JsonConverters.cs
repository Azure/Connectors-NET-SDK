//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Connectors.Sdk.Serialization;

/// <summary>
/// JSON converter for DateTime values using ISO 8601 format.
/// </summary>
public class Iso8601DateTimeConverter : JsonConverter<DateTime>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrEmpty(value))
        {
            return DateTime.MinValue;
        }

        return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(value.ToUniversalTime().ToString(DateTimeFormat));
    }
}

/// <summary>
/// JSON converter for TimeSpan values using ISO 8601 duration format.
/// </summary>
public class Iso8601TimeSpanConverter : JsonConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrEmpty(value))
        {
            return TimeSpan.Zero;
        }

        return System.Xml.XmlConvert.ToTimeSpan(value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue(System.Xml.XmlConvert.ToString(value));
    }
}

/// <summary>
/// JSON converter for nullable TimeSpan values.
/// </summary>
public class NullableTimeSpanConverter : JsonConverter<TimeSpan?>
{
    private readonly Iso8601TimeSpanConverter _innerConverter = new();

    /// <inheritdoc />
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return this._innerConverter.Read(ref reader, typeof(TimeSpan), options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value.HasValue)
        {
            this._innerConverter.Write(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
