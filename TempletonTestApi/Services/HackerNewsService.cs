using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TempletonTestApi.Clients;
using TempletonTestApi.Clients.Enums;
using TempletonTestApi.Clients.Models;
using TempletonTestApi.Contracts.Dtos;
using TempletonTestApi.Contracts.Services;
using TempletonTestApi.Extensions.Mappers;
using TempletonTestApi.Options;

namespace TempletonTestApi.Services;

public class HackerNewsService: IHackerNewsService
{
    private readonly IOptions<HackerNewsServiceOptions> _serviceOptions;
    private readonly ILogger<HackerNewsService> _logger;
    private readonly IHackerNewsClient _hackerNewsClient;
    private readonly IMemoryCache _cache;

    private const string CacheKeyPrefix = "hn:item";

    private readonly int MaxDegreeOfParallelism;
    private readonly TimeSpan ItemTTL;

    public HackerNewsService(
        IOptions<HackerNewsServiceOptions> serviceOptions,
        ILogger<HackerNewsService> logger,
        IHackerNewsClient hackerNewsClient,
        IMemoryCache cache)
    {
        _serviceOptions = serviceOptions;
        _logger = logger;
        _hackerNewsClient = hackerNewsClient;
        _cache = cache;

        ItemTTL = TimeSpan.FromMinutes(_serviceOptions.Value.ItemTTLInMinutes);
        MaxDegreeOfParallelism = _serviceOptions.Value.MaxDegreeOfParallelism;
    }

    public async Task<IEnumerable<StoryDto>> GetBestStoriesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0) 
            return Enumerable.Empty<StoryDto>();

        IEnumerable<long> ids = await _hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);

        var bag = new ConcurrentBag<HackerNewsItem>();

        await Parallel.ForEachAsync(
            ids,
            new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = cancellationToken },
            async (id, token) =>
            {
                var item = await GetStoryCachedAsync(id, token);
                if (item is not null && item.Type == ItemType.Story)
                    bag.Add(item);
            });

        return bag
            .OrderByDescending(s => s.Score)
            .Take(limit)
            .Select(HackerStoryMapper.MapToDto);
    }

    private Task<HackerNewsItem?> GetStoryCachedAsync(long id, CancellationToken cancellationToken)
    {
        var key = $"{CacheKeyPrefix}:{id}";
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ItemTTL;

            try
            {
                return await _hackerNewsClient.GetItemByIdAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching hacker news story item {Id}", id);
                return null;
            }
        })!;
    }
}
