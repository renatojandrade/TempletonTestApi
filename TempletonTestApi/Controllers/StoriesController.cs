using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TempletonTestApi.Contracts.Dtos;
using TempletonTestApi.Contracts.Services;

namespace TempletonTestApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly IHackerNewsService _hackerNewsService;

    public StoriesController(IHackerNewsService hackerNewsService) => _hackerNewsService = hackerNewsService;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StoryDto>))]

    public async Task<IActionResult> GetStories(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var stories = await _hackerNewsService.GetBestStoriesAsync(limit, cancellationToken);

        if (stories?.Any() == false)
        {
            return NotFound();
        }

        return Ok(stories);
    }
}
