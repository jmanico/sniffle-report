using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class DeleteResourceRequestValidator : AbstractValidator<DeleteResourceRequest>
{
    public DeleteResourceRequestValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty()
            .MaximumLength(2_000);
    }
}
