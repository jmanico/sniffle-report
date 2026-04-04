using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/resources")]
public sealed class AdminResourcesController(
    ResourceService resourceService,
    IValidator<GetAdminResourcesQuery> getResourcesValidator,
    IValidator<CreateResourceRequest> createResourceValidator,
    IValidator<UpdateResourceRequest> updateResourceValidator,
    IValidator<DeleteResourceRequest> deleteResourceValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminResourceListDto>>> GetResources(
        [FromQuery] GetAdminResourcesQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getResourcesValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var resources = await resourceService.GetAdminResourcesAsync(query, cancellationToken);
        var totalCount = await resourceService.CountAdminResourcesAsync(query, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        return Ok(resources.Select(MapListDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminResourceDetailDto>> GetResourceById(Guid id, CancellationToken cancellationToken)
    {
        var resource = await resourceService.GetAdminByIdAsync(id, cancellationToken);
        return resource is null ? NotFound() : Ok(MapDetailDto(resource));
    }

    [HttpPost]
    public async Task<ActionResult<AdminResourceDetailDto>> CreateResource(
        [FromBody] CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createResourceValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var resource = await resourceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetResourceById), new { id = resource.Id }, MapDetailDto(resource));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminResourceDetailDto>> UpdateResource(
        Guid id,
        [FromBody] UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateResourceValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var resource = await resourceService.UpdateAsync(id, request, cancellationToken);
        return resource is null ? NotFound() : Ok(MapDetailDto(resource));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteResource(
        Guid id,
        [FromBody] DeleteResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await deleteResourceValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var deleted = await resourceService.DeleteAsync(id, request.Justification!, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static AdminResourceListDto MapListDto(LocalResource resource)
    {
        return new AdminResourceListDto
        {
            Id = resource.Id,
            RegionId = resource.RegionId,
            Name = resource.Name,
            Type = resource.Type,
            Address = resource.Address,
            Phone = resource.Phone,
            Website = resource.Website,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude
        };
    }

    private static AdminResourceDetailDto MapDetailDto(LocalResource resource)
    {
        return new AdminResourceDetailDto
        {
            Id = resource.Id,
            RegionId = resource.RegionId,
            Name = resource.Name,
            Type = resource.Type,
            Address = resource.Address,
            Phone = resource.Phone,
            Website = resource.Website,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude,
            Hours = ResourceService.ParseHours(resource.HoursJson),
            Services = ResourceService.ParseServices(resource.ServicesJson)
        };
    }
}
