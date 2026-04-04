namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateNewsItemRequestValidator : NewsItemRequestValidatorBase<UpdateNewsItemRequest>
{
    protected override Guid GetRegionId(UpdateNewsItemRequest instance) => instance.RegionId;
    protected override string GetHeadline(UpdateNewsItemRequest instance) => instance.Headline;
    protected override string GetContent(UpdateNewsItemRequest instance) => instance.Content;
    protected override string GetSourceUrl(UpdateNewsItemRequest instance) => instance.SourceUrl;
    protected override DateTime GetPublishedAt(UpdateNewsItemRequest instance) => instance.PublishedAt;
}
