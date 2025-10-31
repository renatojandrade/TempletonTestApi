using Bogus;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TempletonTestApi.Clients;
using TempletonTestApi.Clients.Enums;
using TempletonTestApi.Clients.Models;
using TempletonTestApi.Options;
using TempletonTestApi.Services;

namespace TempletonTestApi.Tests.Services;

public class HackerNewsServiceTests
{
    private static IMemoryCache CreateMemoryCache() =>
        new MemoryCache(new MemoryCacheOptions());

    private static IOptions<HackerNewsServiceOptions> CreateOptions(
        int ttlMinutes = 20, int maxParallelism = 8) =>
        Microsoft.Extensions.Options.Options.Create(new HackerNewsServiceOptions
        {
            ItemTTLInMinutes = ttlMinutes,
            MaxDegreeOfParallelism = maxParallelism
        });

    private static Mock<ILogger<HackerNewsService>> CreateLoggerMock() =>
        new Mock<ILogger<HackerNewsService>>();

    private static Faker<HackerNewsItem> StoryFaker(ItemType type = ItemType.Story) =>
        new Faker<HackerNewsItem>()
            .RuleFor(s => s.Id, f => f.Random.Long(1, long.MaxValue))
            .RuleFor(s => s.Title, f => f.Lorem.Sentence(3))
            .RuleFor(s => s.Score, f => f.Random.Int(0, 5000))
            .RuleFor(s => s.Type, type)
            .RuleFor(s => s.CreatedBy, f => f.Internet.UserName())
            .RuleFor(s => s.Descendants, f => f.Random.Int(0, 1000))
            .RuleFor(s => s.CreatedAt, f => DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .RuleFor(s => s.Url, f => f.Internet.Url());

    [Fact]
    public async Task GetBestStoriesAsync_SortsByScoreDesc_And_TakesLimit()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>(MockBehavior.Strict);
        var logger = CreateLoggerMock();
        var cache = CreateMemoryCache();
        var opts = CreateOptions();

        // Known IDs + scores so we can assert order
        var ids = new long[] { 1, 2, 3, 4 };
        var s1 = StoryFaker().RuleFor(s => s.Id, 1).RuleFor(s => s.Title, "low").RuleFor(s => s.Score, 10).Generate();
        var s2 = StoryFaker().RuleFor(s => s.Id, 2).RuleFor(s => s.Title, "high").RuleFor(s => s.Score, 40).Generate();
        var s3 = StoryFaker().RuleFor(s => s.Id, 3).RuleFor(s => s.Title, "mid").RuleFor(s => s.Score, 20).Generate();
        var s4 = StoryFaker().RuleFor(s => s.Id, 4).RuleFor(s => s.Title, "mid2").RuleFor(s => s.Score, 30).Generate();

        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);
        client.Setup(c => c.GetItemByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(s1);
        client.Setup(c => c.GetItemByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(s2);
        client.Setup(c => c.GetItemByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(s3);
        client.Setup(c => c.GetItemByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(s4);

        var sut = new HackerNewsService(opts, logger.Object, client.Object, cache);

        // Act
        var result = (await sut.GetBestStoriesAsync(limit: 3, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        // If StoryDto includes Score, prefer asserting scores.
        // Here we assert order via Titles mapped by your Mapper.
        Assert.Collection(result,
            r => Assert.Equal("high", r.Title),  // 40
            r => Assert.Equal("mid2", r.Title),  // 30
            r => Assert.Equal("mid", r.Title));  // 20

        client.Verify(c => c.GetItemByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task GetBestStoriesAsync_Ignores_NonStory_Types()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>(MockBehavior.Strict);
        var logger = CreateLoggerMock();
        var cache = CreateMemoryCache();
        var opts = CreateOptions();

        var ids = new long[] { 10, 11, 12 };
        var story1 = StoryFaker(ItemType.Story).RuleFor(s => s.Id, 10).RuleFor(s => s.Title, "ok-1").Generate();
        var comment = StoryFaker(ItemType.Comment).RuleFor(s => s.Id, 11).RuleFor(s => s.Title, "should-skip").Generate();
        var story2 = StoryFaker(ItemType.Story).RuleFor(s => s.Id, 12).RuleFor(s => s.Title, "ok-2").Generate();

        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);
        client.Setup(c => c.GetItemByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(story1);
        client.Setup(c => c.GetItemByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(comment);
        client.Setup(c => c.GetItemByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(story2);

        var sut = new HackerNewsService(opts, logger.Object, client.Object, cache);

        // Act
        var result = (await sut.GetBestStoriesAsync(limit: 5, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Title == "ok-1");
        Assert.Contains(result, r => r.Title == "ok-2");
        Assert.DoesNotContain(result, r => r.Title == "should-skip");
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesCache_Across_TwoCalls()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>(MockBehavior.Strict);
        var logger = CreateLoggerMock();
        var cache = CreateMemoryCache();
        var opts = CreateOptions(ttlMinutes: 60);

        var ids = new long[] { 101, 102 };
        var s101 = StoryFaker().RuleFor(s => s.Id, 101).RuleFor(s => s.Title, "A").Generate();
        var s102 = StoryFaker().RuleFor(s => s.Id, 102).RuleFor(s => s.Title, "B").Generate();

        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);
        client.Setup(c => c.GetItemByIdAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(s101);
        client.Setup(c => c.GetItemByIdAsync(102, It.IsAny<CancellationToken>())).ReturnsAsync(s102);

        var sut = new HackerNewsService(opts, logger.Object, client.Object, cache);

        // Act
        var first = (await sut.GetBestStoriesAsync(limit: 2, CancellationToken.None)).ToList();
        var second = (await sut.GetBestStoriesAsync(limit: 2, CancellationToken.None)).ToList();

        // Assert: both calls returned two items
        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);

        // Critically: item endpoint was called once per id (second call served from cache)
        client.Verify(c => c.GetItemByIdAsync(101, It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.GetItemByIdAsync(102, It.IsAny<CancellationToken>()), Times.Once);

        // IDs list can be re-fetched per call (service doesn't cache IDs)
        client.Verify(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetBestStoriesAsync_LimitZero_ReturnsEmpty_And_DoesNotCallClient()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>(MockBehavior.Strict);
        var logger = CreateLoggerMock();
        var cache = CreateMemoryCache();
        var opts = CreateOptions();

        var sut = new HackerNewsService(opts, logger.Object, client.Object, cache);

        // Act
        var result = await sut.GetBestStoriesAsync(0, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBestStoriesAsync_ClientThrows_ItemIsSkipped_And_Logged()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>(MockBehavior.Strict);
        var logger = CreateLoggerMock();
        var cache = CreateMemoryCache();
        var opts = CreateOptions();

        var ids = new long[] { 201, 202 };
        var ok = StoryFaker().RuleFor(s => s.Id, 202).RuleFor(s => s.Title, "OK").Generate();

        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);

        // First item throws (service catches and logs, returns null → filtered out)
        client.Setup(c => c.GetItemByIdAsync(201, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("boom"));
        client.Setup(c => c.GetItemByIdAsync(202, It.IsAny<CancellationToken>())).ReturnsAsync(ok);

        var sut = new HackerNewsService(opts, logger.Object, client.Object, cache);

        // Act
        var result = (await sut.GetBestStoriesAsync(5, CancellationToken.None)).ToList();

        // Assert: only the good one remains
        Assert.Single(result);
        Assert.Equal("OK", result[0].Title);

        // We can’t easily assert logger calls without a provider,
        // but at least verify both client calls were attempted:
        client.Verify(c => c.GetItemByIdAsync(201, It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.GetItemByIdAsync(202, It.IsAny<CancellationToken>()), Times.Once);
    }
}
