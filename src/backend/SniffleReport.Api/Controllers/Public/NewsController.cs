using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/regions/{regionId:guid}/news")]
public sealed class NewsController(
    NewsService newsService,
    IValidator<GetNewsQuery> validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NewsListDto>>> GetNews(
        Guid regionId,
        [FromQuery] GetNewsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = new NewsFilters
        {
            Headline = query.Headline,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var items = await newsService.GetByRegionAsync(regionId, filters, cancellationToken);
        var totalCount = await newsService.CountByRegionAsync(regionId, filters, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(items.Select(MapDto).ToList());
    }

    private static NewsListDto MapDto(NewsItem item)
    {
        return new NewsListDto
        {
            Id = item.Id,
            RegionId = item.RegionId,
            Headline = item.Headline,
            SourceUrl = item.SourceUrl,
            PublishedAt = item.PublishedAt,
            CreatedAt = item.CreatedAt,
            FactCheckStatus = item.FactCheck?.Status
        };
    }
}
