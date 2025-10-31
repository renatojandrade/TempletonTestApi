using System.Globalization;
using TempletonTestApi.Clients.Models;
using TempletonTestApi.Contracts.Dtos;

namespace TempletonTestApi.Extensions.Mappers;

public static class HackerStoryMapper
{
    public static StoryDto MapToDto(HackerNewsItem newsStory)
    {
        var time = DateTimeOffset.FromUnixTimeSeconds(newsStory.CreatedAt)
            .ToOffset(TimeSpan.Zero)
            .ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

        return new StoryDto(
            Title: newsStory.Title,
            Uri: newsStory.Url,
            PostedBy: newsStory.CreatedBy,
            Time: time,
            Score: newsStory.Score,
            CommentCount: newsStory.Descendants
        );
    }
}
