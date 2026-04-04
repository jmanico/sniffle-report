using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/prevention")]
public sealed class AdminPreventionController(
    PreventionService preventionService,
    IValidator<GetAdminPreventionGuidesQuery> getGuidesValidator,
    IValidator<CreatePreventionGuideRequest> createGuideValidator,
    IValidator<UpdatePreventionGuideRequest> updateGuideValidator,
    IValidator<DeletePreventionGuideRequest> deleteGuideValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPreventionGuideListDto>>> GetGuides(
        [FromQuery] GetAdminPreventionGuidesQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getGuidesValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var guides = await preventionService.GetAdminGuidesAsync(query, cancellationToken);
        var totalCount = await preventionService.CountAdminGuidesAsync(query, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(guides.Select(MapListDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPreventionGuideDetailDto>> GetGuideById(Guid id, CancellationToken cancellationToken)
    {
        var guide = await preventionService.GetAdminByIdAsync(id, cancellationToken);
        return guide is null ? NotFound() : Ok(MapDetailDto(guide));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPreventionGuideDetailDto>> CreateGuide(
        [FromBody] CreatePreventionGuideRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createGuideValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var guide = await preventionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetGuideById), new { id = guide.Id }, MapDetailDto(guide));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminPreventionGuideDetailDto>> UpdateGuide(
        Guid id,
        [FromBody] UpdatePreventionGuideRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateGuideValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var guide = await preventionService.UpdateAsync(id, request, cancellationToken);
        return guide is null ? NotFound() : Ok(MapDetailDto(guide));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGuide(
        Guid id,
        [FromBody] DeletePreventionGuideRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await deleteGuideValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var deleted = await preventionService.SoftDeleteAsync(id, request.Justification!, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static AdminPreventionGuideListDto MapListDto(PreventionGuide guide)
    {
        return new AdminPreventionGuideListDto
        {
            Id = guide.Id,
            RegionId = guide.RegionId,
            Disease = guide.Disease,
            Title = guide.Title,
            CreatedAt = guide.CreatedAt,
            IsDeleted = guide.IsDeleted,
            CostTiers = guide.CostTiers.Select(MapCostTierDto).ToList()
        };
    }

    private static AdminPreventionGuideDetailDto MapDetailDto(PreventionGuide guide)
    {
        return new AdminPreventionGuideDetailDto
        {
            Id = guide.Id,
            RegionId = guide.RegionId,
            Disease = guide.Disease,
            Title = guide.Title,
            Content = guide.Content,
            CreatedAt = guide.CreatedAt,
            IsDeleted = guide.IsDeleted,
            DeletedAt = guide.DeletedAt,
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
