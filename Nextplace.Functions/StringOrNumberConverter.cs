using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nextplace.Functions
{
    public class StringOrNumberConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            }
            throw new JsonException($"Unexpected token parsing string. Expected String or Number, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

}
