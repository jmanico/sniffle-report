namespace SniffleReport.Api.Models.DTOs;

public sealed class CreateNewsItemRequestValidator : NewsItemRequestValidatorBase<CreateNewsItemRequest>
{
    protected override Guid GetRegionId(CreateNewsItemRequest instance) => instance.RegionId;
    protected override string GetHeadline(CreateNewsItemRequest instance) => instance.Headline;
    protected override string GetContent(CreateNewsItemRequest instance) => instance.Content;
    protected override string GetSourceUrl(CreateNewsItemRequest instance) => instance.SourceUrl;
    protected override DateTime GetPublishedAt(CreateNewsItemRequest instance) => instance.PublishedAt;
}
