using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/news")]
public sealed class AdminNewsController(
    NewsService newsService,
    IValidator<GetAdminNewsItemsQuery> getNewsValidator,
    IValidator<CreateNewsItemRequest> createNewsValidator,
    IValidator<UpdateNewsItemRequest> updateNewsValidator,
    IValidator<DeleteNewsItemRequest> deleteNewsValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminNewsItemListDto>>> GetNewsItems(
        [FromQuery] GetAdminNewsItemsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getNewsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var newsItems = await newsService.GetAdminNewsItemsAsync(query, cancellationToken);
        var totalCount = await newsService.CountAdminNewsItemsAsync(query, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(newsItems.Select(MapListDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminNewsItemDetailDto>> GetNewsItemById(Guid id, CancellationToken cancellationToken)
    {
        var newsItem = await newsService.GetAdminByIdAsync(id, cancellationToken);
        return newsItem is null ? NotFound() : Ok(MapDetailDto(newsItem));
    }

    [HttpPost]
    public async Task<ActionResult<AdminNewsItemDetailDto>> CreateNewsItem(
        [FromBody] CreateNewsItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createNewsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var newsItem = await newsService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetNewsItemById), new { id = newsItem.Id }, MapDetailDto(newsItem));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminNewsItemDetailDto>> UpdateNewsItem(
        Guid id,
        [FromBody] UpdateNewsItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateNewsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var newsItem = await newsService.UpdateAsync(id, request, cancellationToken);
        return newsItem is null ? NotFound() : Ok(MapDetailDto(newsItem));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNewsItem(
        Guid id,
        [FromBody] DeleteNewsItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await deleteNewsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var deleted = await newsService.SoftDeleteAsync(id, request.Justification!, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static AdminNewsItemListDto MapListDto(NewsItem newsItem)
    {
        return new AdminNewsItemListDto
        {
            Id = newsItem.Id,
            RegionId = newsItem.RegionId,
            Headline = newsItem.Headline,
            SourceUrl = newsItem.SourceUrl,
            PublishedAt = newsItem.PublishedAt,
            CreatedAt = newsItem.CreatedAt,
            IsDeleted = newsItem.IsDeleted,
            FactCheckStatus = newsItem.FactCheck?.Status
        };
    }

    private static AdminNewsItemDetailDto MapDetailDto(NewsItem newsItem)
    {
        return new AdminNewsItemDetailDto
        {
            Id = newsItem.Id,
            RegionId = newsItem.RegionId,
            Headline = newsItem.Headline,
            Content = newsItem.Content,
            SourceUrl = newsItem.SourceUrl,
            PublishedAt = newsItem.PublishedAt,
            CreatedAt = newsItem.CreatedAt,
            IsDeleted = newsItem.IsDeleted,
            DeletedAt = newsItem.DeletedAt,
            FactCheckStatus = newsItem.FactCheck?.Status
        };
    }
}
