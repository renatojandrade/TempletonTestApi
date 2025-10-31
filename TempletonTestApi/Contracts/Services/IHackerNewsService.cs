using TempletonTestApi.Contracts.Dtos;

namespace TempletonTestApi.Contracts.Services;

public interface IHackerNewsService
{
    Task<IEnumerable<StoryDto>> GetBestStoriesAsync(int limit, CancellationToken cancellationToken);
}
