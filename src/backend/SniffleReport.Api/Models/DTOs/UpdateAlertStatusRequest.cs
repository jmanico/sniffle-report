using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class UpdateAlertStatusRequest
{
    public AlertStatus Status { get; init; }

    public string? Justification { get; init; }
}
