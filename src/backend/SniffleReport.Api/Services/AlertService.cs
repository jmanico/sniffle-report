using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

public sealed class AlertService(AppDbContext dbContext, RegionHierarchyService regionHierarchy)
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<HealthAlert>> GetAdminAlertsAsync(AdminAlertFilters filters, CancellationToken cancellationToken = default)
    {
        return await BuildAdminFilteredQuery(filters)
            .OrderByDescending(alert => alert.CreatedAt)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminAlertsAsync(AdminAlertFilters filters, CancellationToken cancellationToken = default)
    {
        return BuildAdminFilteredQuery(filters).CountAsync(cancellationToken);
    }

    public Task<HealthAlert?> GetAdminByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        return dbContext.HealthAlerts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(alert => alert.Id == alertId, cancellationToken);
    }

    public async Task<HealthAlert> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken = default, Guid adminId = default)
    {
        var alert = new HealthAlert
        {
            RegionId = request.RegionId,
            Disease = request.Disease.Trim(),
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            Severity = request.Severity,
            CaseCount = request.CaseCount,
            SourceAttribution = request.SourceAttribution.Trim(),
            SourceDate = request.SourceDate,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.HealthAlerts.Add(alert);
        dbContext.AuditLogEntries.Add(CreateAuditLogEntry(
            adminId,
            AuditLogAction.Create,
            alert,
            before: null,
            after: CreateAlertSnapshot(alert),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return alert;
    }

    public async Task<HealthAlert?> UpdateAsync(Guid alertId, UpdateAlertRequest request, CancellationToken cancellationToken = default, Guid adminId = default)
    {
        var alert = await dbContext.HealthAlerts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(existing => existing.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var before = CreateAlertSnapshot(alert);

        alert.RegionId = request.RegionId;
        alert.Disease = request.Disease.Trim();
        alert.Title = request.Title.Trim();
        alert.Summary = request.Summary.Trim();
        alert.Severity = request.Severity;
        alert.CaseCount = request.CaseCount;
        alert.SourceAttribution = request.SourceAttribution.Trim();
        alert.SourceDate = request.SourceDate;
        alert.UpdatedAt = DateTime.UtcNow;

        dbContext.AuditLogEntries.Add(CreateAuditLogEntry(
            adminId,
            AuditLogAction.Update,
            alert,
            before,
            CreateAlertSnapshot(alert),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return alert;
    }

    public async Task<HealthAlert?> UpdateStatusAsync(Guid alertId, UpdateAlertStatusRequest request, CancellationToken cancellationToken = default, Guid adminId = default)
    {
        var alert = await dbContext.HealthAlerts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(existing => existing.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var before = CreateAlertSnapshot(alert);
        alert.Status = request.Status;
        alert.UpdatedAt = DateTime.UtcNow;

        dbContext.AuditLogEntries.Add(CreateAuditLogEntry(
            adminId,
            AuditLogAction.StatusChange,
            alert,
            before,
            CreateAlertSnapshot(alert),
            request.Justification));
        await dbContext.SaveChangesAsync(cancellationToken);

        return alert;
    }

    public async Task<bool> SoftDeleteAsync(Guid alertId, string justification, CancellationToken cancellationToken = default, Guid adminId = default)
    {
        var alert = await dbContext.HealthAlerts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(existing => existing.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        var before = CreateAlertSnapshot(alert);
        alert.IsDeleted = true;
        alert.DeletedAt = DateTime.UtcNow;
        alert.DeletedBy = adminId == Guid.Empty ? null : adminId;
        alert.UpdatedAt = DateTime.UtcNow;

        dbContext.AuditLogEntries.Add(CreateAuditLogEntry(
            adminId,
            AuditLogAction.Delete,
            alert,
            before,
            CreateAlertSnapshot(alert),
            justification));
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<HealthAlert>> GetByRegionAsync(Guid regionId, AlertFilters filters, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        var query = BuildFilteredQuery(scopedRegionIds, filters);

        query = ApplySorting(query, filters.SortBy, filters.SortDirection);

        return await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByRegionAsync(Guid regionId, AlertFilters filters, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);
        return await BuildFilteredQuery(scopedRegionIds, filters).CountAsync(cancellationToken);
    }

    public async Task<HealthAlert?> GetByIdAsync(Guid regionId, Guid alertId, CancellationToken cancellationToken = default)
    {
        var scopedRegionIds = await GetScopedRegionIdsAsync(regionId, cancellationToken);

        return await dbContext.HealthAlerts
            .AsNoTracking()
            .Include(alert => alert.DiseaseTrends.OrderByDescending(trend => trend.Date))
            .SingleOrDefaultAsync(
                alert => alert.Id == alertId
                    && scopedRegionIds.Contains(alert.RegionId)
                    && alert.Status == AlertStatus.Published,
                cancellationToken);
    }

    public Task<IReadOnlyList<HealthAlert>> GetActiveByRegionAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        return GetByRegionAsync(
            regionId,
            new AlertFilters
            {
                Status = AlertStatus.Published,
                Page = 1,
                PageSize = 100
            },
            cancellationToken);
    }

    private IQueryable<HealthAlert> BuildFilteredQuery(IReadOnlyCollection<Guid> scopedRegionIds, AlertFilters filters)
    {
        IQueryable<HealthAlert> query = dbContext.HealthAlerts
            .AsNoTracking()
            .Where(alert => scopedRegionIds.Contains(alert.RegionId))
            .Where(alert => alert.Status == AlertStatus.Published)
            .Include(alert => alert.DiseaseTrends);

        if (filters.Severity is not null)
        {
            query = query.Where(alert => alert.Severity == filters.Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Disease))
        {
            var normalizedDisease = filters.Disease.Trim().ToLowerInvariant();
            query = query.Where(alert => alert.Disease.ToLower().Contains(normalizedDisease));
        }

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(alert => alert.SourceDate >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(alert => alert.SourceDate <= filters.DateTo.Value);
        }

        return query;
    }

    private IQueryable<HealthAlert> BuildAdminFilteredQuery(AdminAlertFilters filters)
    {
        IQueryable<HealthAlert> query = dbContext.HealthAlerts
            .IgnoreQueryFilters()
            .AsNoTracking();

        if (filters.RegionId.HasValue)
        {
            query = query.Where(alert => alert.RegionId == filters.RegionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Disease))
        {
            var normalizedDisease = filters.Disease.Trim().ToLowerInvariant();
            query = query.Where(alert => alert.Disease.ToLower().Contains(normalizedDisease));
        }

        if (filters.Status.HasValue)
        {
            query = query.Where(alert => alert.Status == filters.Status.Value);
        }

        return query;
    }

    private static IQueryable<HealthAlert> ApplySorting(IQueryable<HealthAlert> query, string? sortBy, string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "sourceDate" => descending
                ? query.OrderByDescending(alert => alert.SourceDate).ThenByDescending(alert => alert.CreatedAt)
                : query.OrderBy(alert => alert.SourceDate).ThenBy(alert => alert.CreatedAt),
            "caseCount" => descending
                ? query.OrderByDescending(alert => alert.CaseCount).ThenByDescending(alert => alert.CreatedAt)
                : query.OrderBy(alert => alert.CaseCount).ThenBy(alert => alert.CreatedAt),
            _ => descending
                ? query.OrderByDescending(alert => alert.CreatedAt)
                : query.OrderBy(alert => alert.CreatedAt)
        };
    }

    private Task<IReadOnlyCollection<Guid>> GetScopedRegionIdsAsync(Guid rootRegionId, CancellationToken cancellationToken)
    {
        return regionHierarchy.GetScopedRegionIdsAsync(rootRegionId, cancellationToken);
    }

    private static AuditLogEntry CreateAuditLogEntry(
        Guid adminId,
        AuditLogAction action,
        HealthAlert alert,
        object? before,
        object? after,
        string? justification)
    {
        return new AuditLogEntry
        {
            AdminId = adminId,
            Action = action,
            EntityType = nameof(HealthAlert),
            EntityId = alert.Id,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, AuditJsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, AuditJsonOptions),
            Justification = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim()
        };
    }

    private static object CreateAlertSnapshot(HealthAlert alert)
    {
        return new
        {
            alert.Id,
            alert.RegionId,
            alert.Disease,
            alert.Title,
            alert.Summary,
            alert.Severity,
            alert.CaseCount,
            alert.SourceAttribution,
            alert.SourceDate,
            alert.Status,
            alert.IsDeleted,
            alert.DeletedAt,
            alert.DeletedBy,
            alert.CreatedAt,
            alert.UpdatedAt
        };
    }
}
