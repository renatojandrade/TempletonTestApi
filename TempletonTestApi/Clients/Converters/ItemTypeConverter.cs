using System.Text.Json;
using System.Text.Json.Serialization;
using TempletonTestApi.Clients.Enums;

namespace TempletonTestApi.Clients.Converters;

public sealed class ItemTypeConverter : JsonConverter<ItemType>
{
    public override ItemType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrEmpty(s)) return ItemType.Unknown;

        return s.ToLowerInvariant() switch
        {
            "story" => ItemType.Story,
            "comment" => ItemType.Comment,
            "job" => ItemType.Job,
            "poll" => ItemType.Poll,
            "pollopt" => ItemType.PollOption,
            _ => ItemType.Unknown
        };
    }

    public override void Write(Utf8JsonWriter writer, ItemType value, JsonSerializerOptions options)
    {
        var s = value switch
        {
            ItemType.Story => "story",
            ItemType.Comment => "comment",
            ItemType.Job => "job",
            ItemType.Poll => "poll",
            ItemType.PollOption => "pollopt",
            _ => "unknown"
        };
        writer.WriteStringValue(s);
    }
}
