using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Models.DTOs;

public sealed class RegionParentDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public RegionType Type { get; init; }
}
