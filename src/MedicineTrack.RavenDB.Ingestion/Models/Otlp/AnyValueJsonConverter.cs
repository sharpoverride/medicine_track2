using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicineTrack.RavenDB.Ingestion.Models.Otlp;

/// <summary>
/// Custom JSON converter for AnyValue that handles numeric values sent as strings
/// </summary>
public class AnyValueJsonConverter : JsonConverter<AnyValue>
{
    public override AnyValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        string? stringValue = null;
        long? intValue = null;
        double? doubleValue = null;
        bool? boolValue = null;

        if (root.TryGetProperty("stringValue", out var strProp))
            stringValue = strProp.GetString();

        if (root.TryGetProperty("intValue", out var intProp))
        {
            // Handle both numeric and string representations
            if (intProp.ValueKind == JsonValueKind.String)
            {
                if (long.TryParse(intProp.GetString(), out var parsedInt))
                    intValue = parsedInt;
            }
            else if (intProp.ValueKind == JsonValueKind.Number)
            {
                intValue = intProp.GetInt64();
            }
        }

        if (root.TryGetProperty("doubleValue", out var doubleProp))
        {
            // Handle both numeric and string representations
            if (doubleProp.ValueKind == JsonValueKind.String)
            {
                if (double.TryParse(doubleProp.GetString(), out var parsedDouble))
                    doubleValue = parsedDouble;
            }
            else if (doubleProp.ValueKind == JsonValueKind.Number)
            {
                doubleValue = doubleProp.GetDouble();
            }
        }

        if (root.TryGetProperty("boolValue", out var boolProp))
        {
            // Handle both boolean and string representations
            if (boolProp.ValueKind == JsonValueKind.String)
            {
                if (bool.TryParse(boolProp.GetString(), out var parsedBool))
                    boolValue = parsedBool;
            }
            else if (boolProp.ValueKind == JsonValueKind.True || boolProp.ValueKind == JsonValueKind.False)
            {
                boolValue = boolProp.GetBoolean();
            }
        }

        return new AnyValue
        {
            StringValue = stringValue,
            IntValue = intValue,
            DoubleValue = doubleValue,
            BoolValue = boolValue
        };
    }

    public override void Write(Utf8JsonWriter writer, AnyValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.StringValue != null)
            writer.WriteString("stringValue", value.StringValue);

        if (value.IntValue.HasValue)
            writer.WriteNumber("intValue", value.IntValue.Value);

        if (value.DoubleValue.HasValue)
            writer.WriteNumber("doubleValue", value.DoubleValue.Value);

        if (value.BoolValue.HasValue)
            writer.WriteBoolean("boolValue", value.BoolValue.Value);

        writer.WriteEndObject();
    }
}
