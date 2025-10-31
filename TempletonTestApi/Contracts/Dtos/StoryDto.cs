namespace TempletonTestApi.Contracts.Dtos;

public sealed record StoryDto(
    string Title,
    string Uri,
    string PostedBy,
    string Time,
    int Score,
    int CommentCount);
