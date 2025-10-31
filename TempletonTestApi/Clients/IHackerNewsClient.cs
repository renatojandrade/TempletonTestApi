using Refit;
using TempletonTestApi.Clients.Models;

namespace TempletonTestApi.Clients;

public interface IHackerNewsClient
{
    [Get("/v0/item/{id}.json")]
    Task<HackerNewsItem?> GetItemByIdAsync(long id, CancellationToken cancellationToken = default);

    [Get("/v0/beststories.json")]
    Task<IEnumerable<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);
}
