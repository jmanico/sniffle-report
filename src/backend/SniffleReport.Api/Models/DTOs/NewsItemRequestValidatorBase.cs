using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public abstract class NewsItemRequestValidatorBase<T> : AbstractValidator<T>
{
    protected NewsItemRequestValidatorBase()
    {
        RuleFor(x => GetRegionId(x))
            .NotEmpty();

        RuleFor(x => GetHeadline(x))
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => GetContent(x))
            .NotEmpty()
            .MaximumLength(10_000);

        RuleFor(x => GetSourceUrl(x))
            .Must(BeValidHttpsUrl)
            .WithMessage("SourceUrl must be a valid https URL.");

        RuleFor(x => GetPublishedAt(x))
            .NotEqual(default(DateTime));
    }

    protected abstract Guid GetRegionId(T instance);
    protected abstract string GetHeadline(T instance);
    protected abstract string GetContent(T instance);
    protected abstract string GetSourceUrl(T instance);
    protected abstract DateTime GetPublishedAt(T instance);

    private static bool BeValidHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && value.Length <= 500;
    }
}
