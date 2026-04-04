using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/alerts")]
public sealed class AdminAlertsController(
    AlertService alertService,
    IValidator<GetAdminAlertsQuery> getAdminAlertsValidator,
    IValidator<CreateAlertRequest> createAlertValidator,
    IValidator<UpdateAlertRequest> updateAlertValidator,
    IValidator<UpdateAlertStatusRequest> updateAlertStatusValidator,
    IValidator<DeleteAlertRequest> deleteAlertValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAlertListDto>>> GetAlerts(
        [FromQuery] GetAdminAlertsQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getAdminAlertsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = new AdminAlertFilters
        {
            RegionId = query.RegionId,
            Disease = query.Disease,
            Status = query.Status,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var alerts = await alertService.GetAdminAlertsAsync(filters, cancellationToken);
        var totalCount = await alertService.CountAdminAlertsAsync(filters, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(alerts.Select(MapListDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminAlertDetailDto>> GetAlertById(Guid id, CancellationToken cancellationToken)
    {
        var alert = await alertService.GetAdminByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            return NotFound();
        }

        return Ok(MapDetailDto(alert));
    }

    [HttpPost]
    public async Task<ActionResult<AdminAlertDetailDto>> CreateAlert(
        [FromBody] CreateAlertRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createAlertValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var alert = await alertService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetAlertById), new { id = alert.Id }, MapDetailDto(alert));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAlertDetailDto>> UpdateAlert(
        Guid id,
        [FromBody] UpdateAlertRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateAlertValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var alert = await alertService.UpdateAsync(id, request, cancellationToken);
        if (alert is null)
        {
            return NotFound();
        }

        return Ok(MapDetailDto(alert));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminAlertDetailDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateAlertStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateAlertStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var alert = await alertService.UpdateStatusAsync(id, request, cancellationToken);
        if (alert is null)
        {
            return NotFound();
        }

        return Ok(MapDetailDto(alert));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAlert(
        Guid id,
        [FromBody] DeleteAlertRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await deleteAlertValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var deleted = await alertService.SoftDeleteAsync(id, request.Justification!, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static AdminAlertListDto MapListDto(HealthAlert alert)
    {
        return new AdminAlertListDto
        {
            Id = alert.Id,
            RegionId = alert.RegionId,
            Disease = alert.Disease,
            Title = alert.Title,
            Severity = alert.Severity,
            CaseCount = alert.CaseCount,
            SourceAttribution = alert.SourceAttribution,
            SourceDate = alert.SourceDate,
            Status = alert.Status,
            CreatedAt = alert.CreatedAt
        };
    }

    private static AdminAlertDetailDto MapDetailDto(HealthAlert alert)
    {
        return new AdminAlertDetailDto
        {
            Id = alert.Id,
            RegionId = alert.RegionId,
            Disease = alert.Disease,
            Title = alert.Title,
            Summary = alert.Summary,
            Severity = alert.Severity,
            CaseCount = alert.CaseCount,
            SourceAttribution = alert.SourceAttribution,
            SourceDate = alert.SourceDate,
            Status = alert.Status,
            CreatedAt = alert.CreatedAt,
            UpdatedAt = alert.UpdatedAt
        };
    }
}
