using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/regions")]
public sealed class RegionsController(
    RegionService regionService,
    IValidator<GetRegionsQuery> getRegionsValidator,
    IValidator<SearchRegionsQuery> searchRegionsValidator,
    IValidator<GetRegionByIdRoute> getRegionByIdValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegionListDto>>> GetRegions(
        [FromQuery] GetRegionsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getRegionsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var regions = await regionService.GetAllAsync(query.Type, cancellationToken);
        var pagedRegions = ApplyPagination(regions, query.Page, query.PageSize);

        Response.Headers.Append("X-Total-Count", regions.Count.ToString());

        return Ok(pagedRegions.Select(MapListDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RegionDetailDto>> GetRegionById(Guid id, CancellationToken cancellationToken)
    {
        var routeModel = new GetRegionByIdRoute { Id = id };
        var validationResult = await getRegionByIdValidator.ValidateAsync(routeModel, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var region = await regionService.GetByIdAsync(id, cancellationToken);
        if (region is null)
        {
            return NotFound();
        }

        return Ok(MapDetailDto(region));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<RegionListDto>>> SearchRegions(
        [FromQuery] SearchRegionsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await searchRegionsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var regions = await regionService.SearchAsync(query.Q, cancellationToken);
        var pagedRegions = ApplyPagination(regions, query.Page, query.PageSize);

        Response.Headers.Append("X-Total-Count", regions.Count.ToString());

        return Ok(pagedRegions.Select(MapListDto).ToList());
    }

    private static IReadOnlyList<Region> ApplyPagination(IReadOnlyList<Region> regions, int page, int pageSize)
    {
        return regions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private static RegionListDto MapListDto(Region region)
    {
        return new RegionListDto
        {
            Id = region.Id,
            Name = region.Name,
            Type = region.Type,
            State = region.State,
            ParentId = region.ParentId
        };
    }

    private static RegionDetailDto MapDetailDto(Region region)
    {
        return new RegionDetailDto
        {
            Id = region.Id,
            Name = region.Name,
            Type = region.Type,
            State = region.State,
            Latitude = region.Latitude,
            Longitude = region.Longitude,
            ChildCount = region.Children.Count,
            Parent = region.Parent is null
                ? null
                : new RegionParentDto
                {
                    Id = region.Parent.Id,
                    Name = region.Parent.Name,
                    Type = region.Parent.Type
                }
        };
    }
}
