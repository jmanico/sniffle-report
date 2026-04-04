using FluentValidation;

namespace SniffleReport.Api.Models.DTOs;

public sealed class DeleteNewsItemRequestValidator : AbstractValidator<DeleteNewsItemRequest>
{
    public DeleteNewsItemRequestValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty()
            .MaximumLength(2_000);
    }
}
