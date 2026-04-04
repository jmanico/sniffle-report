using System.Text.Json;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

internal static class AdminAuditLog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AuditLogEntry Create(
        Guid adminId,
        AuditLogAction action,
        string entityType,
        Guid entityId,
        object? before,
        object? after,
        string? justification)
    {
        return new AuditLogEntry
        {
            AdminId = adminId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = Serialize(before),
            AfterJson = Serialize(after),
            Justification = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim()
        };
    }

    private static string? Serialize(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
    }
}
