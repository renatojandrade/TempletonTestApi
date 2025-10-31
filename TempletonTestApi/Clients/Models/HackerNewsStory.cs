using System.Text.Json.Serialization;

namespace TempletonTestApi.Clients.Models;

public sealed class HackerNewsStory
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

    public string Type { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}
