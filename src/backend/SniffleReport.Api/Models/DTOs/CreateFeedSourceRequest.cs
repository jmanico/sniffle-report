using FluentValidation;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class CreateFeedSourceRequest
{
    public string Name { get; init; } = string.Empty;

    public FeedSourceType Type { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? SoqlQuery { get; init; }

    public int PollingIntervalMinutes { get; init; } = 360;

    public bool IsEnabled { get; init; } = true;

    public bool AutoPublish { get; init; }
}

public sealed class CreateFeedSourceRequestValidator : AbstractValidator<CreateFeedSourceRequest>
{
    public CreateFeedSourceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(1_000);
        RuleFor(x => x.SoqlQuery).MaximumLength(2_000);
        RuleFor(x => x.PollingIntervalMinutes).InclusiveBetween(5, 1440);
    }
}
