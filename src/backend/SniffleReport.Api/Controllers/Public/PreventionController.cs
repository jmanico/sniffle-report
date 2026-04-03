using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/regions/{regionId:guid}/prevention")]
public sealed class PreventionController(
    PreventionService preventionService,
    IValidator<GetPreventionQuery> getPreventionValidator,
    IValidator<GetPreventionByIdRoute> getPreventionByIdValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PreventionListDto>>> GetGuides(
        Guid regionId,
        [FromQuery] GetPreventionQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getPreventionValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = new PreventionFilters
        {
            Disease = query.Disease,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var guides = await preventionService.GetByRegionAsync(regionId, filters, cancellationToken);
        var totalCount = await preventionService.CountByRegionAsync(regionId, filters, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(guides.Select(MapListDto).ToList());
    }

    [HttpGet("{guideId:guid}")]
    public async Task<ActionResult<PreventionDetailDto>> GetGuideById(Guid regionId, Guid guideId, CancellationToken cancellationToken)
    {
        var routeModel = new GetPreventionByIdRoute
        {
            RegionId = regionId,
            GuideId = guideId
        };

        var validationResult = await getPreventionByIdValidator.ValidateAsync(routeModel, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var guide = await preventionService.GetByIdAsync(regionId, guideId, cancellationToken);
        if (guide is null)
        {
            return NotFound();
        }

        return Ok(MapDetailDto(guide));
    }

    private static PreventionListDto MapListDto(PreventionGuide guide)
    {
        return new PreventionListDto
        {
            Id = guide.Id,
            RegionId = guide.RegionId,
            Disease = guide.Disease,
            Title = guide.Title,
            CreatedAt = guide.CreatedAt,
            CostTiers = guide.CostTiers.Select(MapCostTierDto).ToList()
        };
    }

    private static PreventionDetailDto MapDetailDto(PreventionGuide guide)
    {
        return new PreventionDetailDto
        {
            Id = guide.Id,
            RegionId = guide.RegionId,
            Disease = guide.Disease,
            Title = guide.Title,
            Content = guide.Content,
            CreatedAt = guide.CreatedAt,
            CostTiers = guide.CostTiers.Select(MapCostTierDto).ToList()
        };
    }

    private static CostTierDto MapCostTierDto(CostTier tier)
    {
        return new CostTierDto
        {
            Type = tier.Type,
            Price = tier.Price,
            Provider = tier.Provider,
            Notes = tier.Notes
        };
    }
}
