using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Services;

namespace SniffleReport.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/v1/regions/{regionId:guid}/resources")]
public sealed class ResourcesController(
    ResourceService resourceService,
    IValidator<GetResourcesQuery> getResourcesValidator,
    IValidator<GetResourceByIdRoute> getResourceByIdValidator,
    IValidator<GetNearbyResourcesQuery> getNearbyResourcesValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResourceListDto>>> GetResources(
        Guid regionId,
        [FromQuery] GetResourcesQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getResourcesValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = new ResourceFilters
        {
            Type = query.Type,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var resources = await resourceService.GetByRegionAsync(regionId, filters, cancellationToken);
        var totalCount = await resourceService.CountByRegionAsync(regionId, filters, cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(resources);
    }

    [HttpGet("{resourceId:guid}")]
    public async Task<ActionResult<ResourceDetailDto>> GetResourceById(
        Guid regionId,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var routeModel = new GetResourceByIdRoute
        {
            RegionId = regionId,
            ResourceId = resourceId
        };

        var validationResult = await getResourceByIdValidator.ValidateAsync(routeModel, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var resource = await resourceService.GetByIdAsync(regionId, resourceId, cancellationToken);
        if (resource is null)
        {
            return NotFound();
        }

        return Ok(resource);
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IReadOnlyList<ResourceListDto>>> SearchNearby(
        Guid regionId,
        [FromQuery] GetNearbyResourcesQuery query,
        CancellationToken cancellationToken)
    {
        var validationResult = await getNearbyResourcesValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var filters = new ResourceFilters
        {
            Type = query.Type,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var resources = await resourceService.SearchNearbyAsync(
            regionId,
            query.Lat!.Value,
            query.Lng!.Value,
            query.Radius,
            filters,
            cancellationToken);
        var totalCount = await resourceService.CountNearbyAsync(
            regionId,
            query.Lat.Value,
            query.Lng.Value,
            query.Radius,
            filters,
            cancellationToken);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        return Ok(resources);
    }
}
