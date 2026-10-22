using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamBoard.Api.Json;

// Every DateTime in and out of the API is UTC: "+07:00" input is converted, input without an offset is taken as UTC,
// and output always ends in "Z". Also applies to DateTime? (System.Text.Json wraps it for nullables).
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ToUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToUtc(value));

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
