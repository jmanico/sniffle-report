using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class CreateResourceRequestValidator : ResourceRequestValidatorBase<CreateResourceRequest>
{
    protected override Guid GetRegionId(CreateResourceRequest instance) => instance.RegionId;
    protected override string GetName(CreateResourceRequest instance) => instance.Name;
    protected override ResourceType GetType(CreateResourceRequest instance) => instance.Type;
    protected override string GetAddress(CreateResourceRequest instance) => instance.Address;
    protected override string? GetPhone(CreateResourceRequest instance) => instance.Phone;
    protected override string? GetWebsite(CreateResourceRequest instance) => instance.Website;
    protected override double? GetLatitude(CreateResourceRequest instance) => instance.Latitude;
    protected override double? GetLongitude(CreateResourceRequest instance) => instance.Longitude;
    protected override ResourceHoursDto GetHours(CreateResourceRequest instance) => instance.Hours;
    protected override IReadOnlyList<string> GetServices(CreateResourceRequest instance) => instance.Services;
}
