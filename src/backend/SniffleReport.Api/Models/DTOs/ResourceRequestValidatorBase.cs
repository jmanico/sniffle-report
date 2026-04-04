using FluentValidation;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public abstract class ResourceRequestValidatorBase<T> : AbstractValidator<T>
{
    protected ResourceRequestValidatorBase()
    {
        RuleFor(x => GetRegionId(x))
            .NotEmpty();

        RuleFor(x => GetName(x))
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => GetType(x))
            .IsInEnum();

        RuleFor(x => GetAddress(x))
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => GetPhone(x))
            .MaximumLength(40);

        RuleFor(x => GetWebsite(x))
            .Must(BeValidHttpsUrl)
            .When(x => !string.IsNullOrWhiteSpace(GetWebsite(x)));

        RuleFor(x => GetLatitude(x))
            .InclusiveBetween(-90, 90)
            .When(x => GetLatitude(x).HasValue);

        RuleFor(x => GetLongitude(x))
            .InclusiveBetween(-180, 180)
            .When(x => GetLongitude(x).HasValue);

        RuleFor(x => x)
            .Must(instance => GetLatitude(instance).HasValue == GetLongitude(instance).HasValue)
            .WithMessage("Latitude and longitude must both be provided together.");

        RuleFor(x => x)
            .Custom((instance, context) =>
            {
                var services = GetServices(instance);
                for (var i = 0; i < services.Count; i++)
                {
                    var service = services[i];
                    if (string.IsNullOrWhiteSpace(service))
                    {
                        context.AddFailure($"Services[{i}]", "Service entries must not be empty.");
                    }
                    else if (service.Length > 100)
                    {
                        context.AddFailure($"Services[{i}]", "Service entries must be 100 characters or fewer.");
                    }
                }

                ValidateHours(context, GetHours(instance));
            });
    }

    protected abstract Guid GetRegionId(T instance);
    protected abstract string GetName(T instance);
    protected abstract ResourceType GetType(T instance);
    protected abstract string GetAddress(T instance);
    protected abstract string? GetPhone(T instance);
    protected abstract string? GetWebsite(T instance);
    protected abstract double? GetLatitude(T instance);
    protected abstract double? GetLongitude(T instance);
    protected abstract ResourceHoursDto GetHours(T instance);
    protected abstract IReadOnlyList<string> GetServices(T instance);

    private static void ValidateHours(ValidationContext<T> context, ResourceHoursDto hours)
    {
        ValidateHour(context, "Hours.Mon", hours.Mon);
        ValidateHour(context, "Hours.Tue", hours.Tue);
        ValidateHour(context, "Hours.Wed", hours.Wed);
        ValidateHour(context, "Hours.Thu", hours.Thu);
        ValidateHour(context, "Hours.Fri", hours.Fri);
        ValidateHour(context, "Hours.Sat", hours.Sat);
        ValidateHour(context, "Hours.Sun", hours.Sun);
    }

    private static void ValidateHour(ValidationContext<T> context, string propertyName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length > 100)
        {
            context.AddFailure(propertyName, "Hour entries must be 100 characters or fewer.");
        }
    }

    private static bool BeValidHttpsUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && value!.Length <= 500;
    }
}
