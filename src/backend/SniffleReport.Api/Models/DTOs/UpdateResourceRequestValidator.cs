using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateResourceRequestValidator : ResourceRequestValidatorBase<UpdateResourceRequest>
{
    protected override Guid GetRegionId(UpdateResourceRequest instance) => instance.RegionId;
    protected override string GetName(UpdateResourceRequest instance) => instance.Name;
    protected override ResourceType GetType(UpdateResourceRequest instance) => instance.Type;
    protected override string GetAddress(UpdateResourceRequest instance) => instance.Address;
    protected override string? GetPhone(UpdateResourceRequest instance) => instance.Phone;
    protected override string? GetWebsite(UpdateResourceRequest instance) => instance.Website;
    protected override double? GetLatitude(UpdateResourceRequest instance) => instance.Latitude;
    protected override double? GetLongitude(UpdateResourceRequest instance) => instance.Longitude;
    protected override ResourceHoursDto GetHours(UpdateResourceRequest instance) => instance.Hours;
    protected override IReadOnlyList<string> GetServices(UpdateResourceRequest instance) => instance.Services;
}
