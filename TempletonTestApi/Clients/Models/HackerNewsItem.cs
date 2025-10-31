using System.Text.Json.Serialization;
using TempletonTestApi.Clients.Converters;
using TempletonTestApi.Clients.Enums;

namespace TempletonTestApi.Clients.Models;

public sealed class HackerNewsItem
{
    [JsonPropertyName("by")]
    public string CreatedBy { get; set; } = string.Empty;

    public int Descendants { get; set; }

    public long Id { get; set; }

    public List<long> Kids { get; set; } = [];

    public int Score { get; set; }

    [JsonPropertyName("time")]
    public long CreatedAt { get; set; }

    public string Title { get; set; } = string.Empty;

    [JsonConverter(typeof(ItemTypeConverter))]
    public ItemType Type { get; set; }

    public string Url { get; set; } = string.Empty;
}
