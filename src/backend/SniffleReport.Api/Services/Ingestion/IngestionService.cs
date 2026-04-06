using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services.Ingestion;

public sealed class IngestionService(
    AppDbContext dbContext,
    IEnumerable<IFeedConnector> connectors,
    RegionMappingService regionMapping,
    AlertThresholdService thresholdService,
    IOptions<FeedIngestionOptions> options,
    ILogger<IngestionService> logger)
{
    private readonly FeedIngestionOptions _options = options.Value;

    public async Task<FeedSyncLog> ExecuteSyncAsync(FeedSource source, CancellationToken ct)
    {
        var syncLog = new FeedSyncLog
        {
            FeedSourceId = source.Id,
            StartedAt = DateTime.UtcNow,
            Status = FeedSyncStatus.Running
        };
        dbContext.FeedSyncLogs.Add(syncLog);

        source.LastSyncStartedAt = DateTime.UtcNow;
        source.UpdatedAt = DateTime.UtcNow;

        try
        {
            var connector = connectors.FirstOrDefault(c => c.SourceType == source.Type)
                ?? throw new InvalidOperationException($"No connector registered for feed type {source.Type}");

            var result = await connector.FetchAsync(source, ct);

            if (!result.IsSuccess)
            {
                MarkFailed(source, syncLog, result.ErrorMessage ?? "Fetch failed with no error message");
                await dbContext.SaveChangesAsync(ct);
                return syncLog;
            }

            syncLog.RecordsFetched = result.Records.Count;
            logger.LogInformation(
                "Feed {FeedName} fetched {Count} records",
                source.Name, result.Records.Count);

            regionMapping.InvalidateCache();

            // Deduplicate records within the batch by external source ID
            var deduplicatedRecords = result.Records
                .GroupBy(r => r.ExternalSourceId)
                .Select(g => g.First())
                .ToList();

            if (deduplicatedRecords.Count < result.Records.Count)
            {
                logger.LogInformation(
                    "Deduplicated {Original} records to {Unique} unique external IDs for {FeedName}",
                    result.Records.Count, deduplicatedRecords.Count, source.Name);
            }

            var affectedRegionDiseases = new HashSet<(Guid RegionId, string Disease)>();

            foreach (var record in deduplicatedRecords)
            {
                await ProcessRecordAsync(source, record, syncLog, affectedRegionDiseases, ct);
            }

            // Evaluate thresholds for affected region+disease combinations
            foreach (var (regionId, disease) in affectedRegionDiseases)
            {
                var promoted = await thresholdService.EvaluateAndPromoteAsync(
                    regionId, disease, source.Name, ct);

                syncLog.AlertsPromoted += promoted.Count;
            }

            MarkSuccess(source, syncLog);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Feed sync failed for {FeedName}", source.Name);
            MarkFailed(source, syncLog, ex.Message, ex.StackTrace);

            // Detach tracked entities to avoid saving partial state, then re-attach log
            dbContext.ChangeTracker.Clear();
            dbContext.Attach(source);
            dbContext.Entry(source).State = EntityState.Modified;
            dbContext.FeedSyncLogs.Add(syncLog);

            await dbContext.SaveChangesAsync(ct);
        }

        return syncLog;
    }

    private async Task ProcessRecordAsync(
        FeedSource source,
        NormalizedFeedRecord record,
        FeedSyncLog syncLog,
        HashSet<(Guid RegionId, string Disease)> affectedRegionDiseases,
        CancellationToken ct)
    {
        var payloadHash = ComputeHash(record.RawPayloadJson);

        var existing = await dbContext.IngestedRecords
            .FirstOrDefaultAsync(
                r => r.FeedSourceId == source.Id && r.ExternalSourceId == record.ExternalSourceId,
                ct);

        if (existing is not null)
        {
            if (existing.PayloadHash == payloadHash)
            {
                existing.LastIngestedAt = DateTime.UtcNow;
                existing.IngestCount++;
                syncLog.RecordsSkippedDuplicate++;
                return;
            }

            // Data changed — update the target entity
            await UpdateExistingEntityAsync(existing, record, source, syncLog, ct);
            existing.PayloadHash = payloadHash;
            existing.LastIngestedAt = DateTime.UtcNow;
            existing.IngestCount++;
            syncLog.RecordsUpdated++;
            return;
        }

        // New record — resolve region and create entity
        var regionId = await regionMapping.ResolveRegionIdAsync(record.JurisdictionName, ct);
        if (regionId is null)
        {
            // Store the ingested record for potential future mapping
            dbContext.IngestedRecords.Add(new IngestedRecord
            {
                FeedSourceId = source.Id,
                ExternalSourceId = record.ExternalSourceId,
                PayloadHash = payloadHash,
                TargetEntityType = GetTargetEntityTypeName(record.RecordType),
                TargetEntityId = Guid.Empty
            });
            syncLog.RecordsSkippedUnmappable++;
            return;
        }

        var (entityType, entityId) = await CreateEntityFromRecordAsync(
            record, regionId.Value, source, ct);

        var ingestedRecord = new IngestedRecord
        {
            FeedSourceId = source.Id,
            ExternalSourceId = record.ExternalSourceId,
            PayloadHash = payloadHash,
            TargetEntityType = entityType,
            TargetEntityId = entityId
        };
        dbContext.IngestedRecords.Add(ingestedRecord);

        if (record.RecordType == NormalizedRecordType.TrendDataPoint && record.Disease is not null)
        {
            affectedRegionDiseases.Add((regionId.Value, record.Disease));
        }

        syncLog.RecordsCreated++;
    }

    private async Task<(string EntityType, Guid EntityId)> CreateEntityFromRecordAsync(
        NormalizedFeedRecord record,
        Guid regionId,
        FeedSource source,
        CancellationToken ct)
    {
        switch (record.RecordType)
        {
            case NormalizedRecordType.TrendDataPoint:
                return await CreateTrendFromRecordAsync(record, regionId, source, ct);

            case NormalizedRecordType.NewsArticle:
                return CreateNewsFromRecord(record, regionId, source);

            case NormalizedRecordType.LocalResourceEntry:
                return await CreateLocalResourceFromRecordAsync(record, regionId, source, ct);

            case NormalizedRecordType.ShortageAreaDesignation:
                return CreateShortageAreaDesignationFromRecord(record, regionId, source);

            case NormalizedRecordType.DrinkingWaterViolation:
                return await CreateDrinkingWaterViolationFromRecordAsync(record, regionId, source, ct);

            default:
                throw new ArgumentOutOfRangeException(nameof(record), $"Unknown record type: {record.RecordType}");
        }
    }

    private async Task<(string, Guid)> CreateTrendFromRecordAsync(
        NormalizedFeedRecord record,
        Guid regionId,
        FeedSource source,
        CancellationToken ct)
    {
        // Find or create a parent HealthAlert for this region + disease
        var disease = record.Disease?.Trim() ?? "Unknown";
        var alert = await FindOrCreateAlertAsync(regionId, disease, source, record.Title, record.Summary, ct);

        var trend = new DiseaseTrend
        {
            AlertId = alert.Id,
            Date = record.DataDate ?? DateTime.UtcNow,
            CaseCount = record.CaseCount ?? 0,
            Source = record.SourceAttribution?.Trim() ?? source.Name,
            SourceDate = record.SourceDate ?? DateTime.UtcNow
        };
        dbContext.DiseaseTrends.Add(trend);

        // Update the alert's aggregate case count if the new trend is more recent
        if (trend.Date >= alert.SourceDate)
        {
            alert.CaseCount = trend.CaseCount;
            alert.SourceDate = trend.Date;
            alert.UpdatedAt = DateTime.UtcNow;
        }

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(DiseaseTrend),
            trend.Id,
            null,
            new { trend.AlertId, trend.Date, trend.CaseCount, trend.Source, FeedSource = source.Name },
            $"Ingested from {source.Name}"));

        return (nameof(DiseaseTrend), trend.Id);
    }

    private (string, Guid) CreateNewsFromRecord(
        NormalizedFeedRecord record,
        Guid regionId,
        FeedSource source)
    {
        var newsItem = new NewsItem
        {
            RegionId = regionId,
            Headline = (record.Title?.Trim() ?? "Untitled")[..Math.Min(record.Title?.Trim().Length ?? 9, 300)],
            Content = record.Summary?.Trim() ?? string.Empty,
            SourceUrl = (record.SourceUrl?.Trim() ?? string.Empty)[..Math.Min(record.SourceUrl?.Trim().Length ?? 0, 500)],
            PublishedAt = record.SourceDate ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.NewsItems.Add(newsItem);

        // Auto-create a verified FactCheck for CDC sources
        var factCheck = new FactCheck
        {
            NewsItemId = newsItem.Id,
            Status = FactCheckStatus.Verified,
            Verdict = $"Source: {source.Name}. Auto-verified from official CDC feed.",
            SourcesJson = System.Text.Json.JsonSerializer.Serialize(
                new[] { record.SourceUrl ?? source.Url }),
            CheckedAt = DateTime.UtcNow
        };
        dbContext.FactChecks.Add(factCheck);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(NewsItem),
            newsItem.Id,
            null,
            new { newsItem.Headline, newsItem.SourceUrl, FeedSource = source.Name },
            $"Ingested from {source.Name}"));

        return (nameof(NewsItem), newsItem.Id);
    }

    private async Task<(string, Guid)> CreateLocalResourceFromRecordAsync(
        NormalizedFeedRecord record,
        Guid regionId,
        FeedSource source,
        CancellationToken ct)
    {
        // Check if resource already exists by name + region to avoid duplicates
        var name = record.ResourceName?.Trim() ?? "Unknown Provider";
        var existing = await dbContext.LocalResources
            .FirstOrDefaultAsync(r => r.RegionId == regionId && r.Name == name, ct);

        if (existing is not null)
        {
            // Update existing resource
            existing.Address = record.Address?.Trim() ?? existing.Address;
            existing.Phone = record.Phone;
            existing.Website = record.Website;
            existing.Latitude = record.Latitude ?? existing.Latitude;
            existing.Longitude = record.Longitude ?? existing.Longitude;
            if (record.ResourceType.HasValue) existing.Type = record.ResourceType.Value;
            return (nameof(LocalResource), existing.Id);
        }

        var resource = new LocalResource
        {
            RegionId = regionId,
            Name = name,
            Type = record.ResourceType ?? Models.Enums.ResourceType.Clinic,
            Address = record.Address?.Trim() ?? string.Empty,
            Phone = record.Phone,
            Website = record.Website,
            Latitude = record.Latitude,
            Longitude = record.Longitude
        };
        dbContext.LocalResources.Add(resource);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(LocalResource),
            resource.Id,
            null,
            new { resource.RegionId, resource.Name, resource.Type, FeedSource = source.Name },
            $"Ingested from {source.Name}"));

        return (nameof(LocalResource), resource.Id);
    }

    private (string, Guid) CreateShortageAreaDesignationFromRecord(
        NormalizedFeedRecord record,
        Guid regionId,
        FeedSource source)
    {
        var designation = new ShortageAreaDesignation
        {
            RegionId = regionId,
            ExternalSourceId = record.ExternalSourceId,
            AreaName = record.AreaName?.Trim() ?? record.JurisdictionName?.Trim() ?? "Unknown shortage area",
            Discipline = record.Discipline?.Trim() ?? "Primary care",
            DesignationType = record.DesignationType?.Trim() ?? "Geographic area",
            Status = record.DesignationStatus?.Trim() ?? "Designated",
            PopulationGroup = record.PopulationGroup?.Trim(),
            HpsaScore = record.HpsaScore,
            PopulationToProviderRatio = record.PopulationToProviderRatio,
            SourceUpdatedAt = record.SourceDate
        };
        dbContext.ShortageAreaDesignations.Add(designation);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(ShortageAreaDesignation),
            designation.Id,
            null,
            new { designation.RegionId, designation.AreaName, designation.Discipline, FeedSource = source.Name },
            $"Ingested from {source.Name}"));

        return (nameof(ShortageAreaDesignation), designation.Id);
    }

    private async Task<(string, Guid)> CreateDrinkingWaterViolationFromRecordAsync(
        NormalizedFeedRecord record,
        Guid regionId,
        FeedSource source,
        CancellationToken ct)
    {
        var waterSystemExternalId = record.ParentExternalSourceId?.Trim();
        var waterSystem = waterSystemExternalId is null
            ? null
            : await dbContext.WaterSystems.FirstOrDefaultAsync(s => s.ExternalSourceId == waterSystemExternalId, ct);

        if (waterSystem is null)
        {
            waterSystem = new WaterSystem
            {
                ExternalSourceId = waterSystemExternalId ?? record.ExternalSourceId,
                Name = record.WaterSystemName?.Trim() ?? "Unknown water system",
                SystemType = record.WaterSystemType?.Trim(),
                Address = record.WaterSystemAddress?.Trim(),
                City = record.WaterSystemCity?.Trim(),
                State = record.WaterSystemState?.Trim(),
                PostalCode = record.WaterSystemPostalCode?.Trim(),
                CountyServed = record.CountyServed?.Trim(),
                PopulationServed = record.PopulationServed
            };
            dbContext.WaterSystems.Add(waterSystem);
        }
        else
        {
            waterSystem.Name = record.WaterSystemName?.Trim() ?? waterSystem.Name;
            waterSystem.SystemType = record.WaterSystemType?.Trim() ?? waterSystem.SystemType;
            waterSystem.Address = record.WaterSystemAddress?.Trim() ?? waterSystem.Address;
            waterSystem.City = record.WaterSystemCity?.Trim() ?? waterSystem.City;
            waterSystem.State = record.WaterSystemState?.Trim() ?? waterSystem.State;
            waterSystem.PostalCode = record.WaterSystemPostalCode?.Trim() ?? waterSystem.PostalCode;
            waterSystem.CountyServed = record.CountyServed?.Trim() ?? waterSystem.CountyServed;
            waterSystem.PopulationServed = record.PopulationServed ?? waterSystem.PopulationServed;
        }

        var violation = new WaterSystemViolation
        {
            WaterSystem = waterSystem,
            RegionId = regionId,
            ExternalSourceId = record.ExternalSourceId,
            ViolationCategory = record.ViolationCategory?.Trim() ?? "Drinking water violation",
            RuleName = record.RuleName?.Trim() ?? "EPA SDWIS rule",
            ContaminantName = record.ContaminantName?.Trim(),
            Summary = record.Summary?.Trim() ?? "EPA drinking water violation.",
            IsOpen = record.IsOpenViolation ?? true,
            IdentifiedAt = record.IdentifiedAt,
            ResolvedAt = record.ResolvedAt,
            SourceUpdatedAt = record.SourceDate
        };
        dbContext.WaterSystemViolations.Add(violation);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(WaterSystemViolation),
            violation.Id,
            null,
            new { violation.RegionId, violation.ViolationCategory, violation.RuleName, FeedSource = source.Name },
            $"Ingested from {source.Name}"));

        return (nameof(WaterSystemViolation), violation.Id);
    }

    private async Task<HealthAlert> FindOrCreateAlertAsync(
        Guid regionId,
        string disease,
        FeedSource source,
        string? recordTitle,
        string? recordSummary,
        CancellationToken ct)
    {
        // Look for an existing non-archived alert for this region + disease
        var existing = await dbContext.HealthAlerts
            .FirstOrDefaultAsync(
                a => a.RegionId == regionId
                    && a.Disease == disease
                    && a.Status != AlertStatus.Archived,
                ct);

        if (existing is not null)
        {
            // Update title/summary if better data is available
            if (recordTitle is not null && existing.Title.Contains("data from"))
            {
                existing.Title = recordTitle.Length > 200 ? recordTitle[..200] : recordTitle;
            }
            if (recordSummary is not null && existing.Summary.StartsWith("Surveillance data"))
            {
                existing.Summary = recordSummary.Length > 2000 ? recordSummary[..2000] : recordSummary;
            }
            return existing;
        }

        var title = recordTitle ?? $"{disease} — data from {source.Name}";
        var summary = recordSummary ?? $"Surveillance data ingested from {source.Name}.";

        var alert = new HealthAlert
        {
            RegionId = regionId,
            Disease = disease,
            Title = title.Length > 200 ? title[..200] : title,
            Summary = summary.Length > 2000 ? summary[..2000] : summary,
            Severity = AlertSeverity.Low,
            CaseCount = 0,
            SourceAttribution = source.Name,
            SourceDate = DateTime.UnixEpoch, // Set to epoch so the first trend data point always updates the alert
            Status = source.AutoPublish ? AlertStatus.Published : AlertStatus.Draft
        };
        dbContext.HealthAlerts.Add(alert);

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            _options.SystemUserId,
            AuditLogAction.FeedIngest,
            nameof(HealthAlert),
            alert.Id,
            null,
            new { alert.RegionId, alert.Disease, alert.Title, FeedSource = source.Name },
            $"Auto-created from {source.Name} feed ingestion"));

        return alert;
    }

    private async Task UpdateExistingEntityAsync(
        IngestedRecord ingestedRecord,
        NormalizedFeedRecord record,
        FeedSource source,
        FeedSyncLog syncLog,
        CancellationToken ct)
    {
        switch (ingestedRecord.TargetEntityType)
        {
            case nameof(DiseaseTrend):
                var trend = await dbContext.DiseaseTrends
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == ingestedRecord.TargetEntityId, ct);
                if (trend is not null)
                {
                    trend.CaseCount = record.CaseCount ?? trend.CaseCount;
                    trend.SourceDate = record.SourceDate ?? trend.SourceDate;
                    trend.Source = record.SourceAttribution?.Trim() ?? trend.Source;
                }
                break;

            case nameof(NewsItem):
                var news = await dbContext.NewsItems
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(n => n.Id == ingestedRecord.TargetEntityId, ct);
                if (news is not null)
                {
                    news.Headline = (record.Title?.Trim() ?? news.Headline)[..Math.Min(record.Title?.Trim().Length ?? news.Headline.Length, 300)];
                    news.Content = record.Summary?.Trim() ?? news.Content;
                }
                break;

            case nameof(LocalResource):
                var resource = await dbContext.LocalResources
                    .FirstOrDefaultAsync(r => r.Id == ingestedRecord.TargetEntityId, ct);
                if (resource is not null)
                {
                    resource.Address = record.Address?.Trim() ?? resource.Address;
                    resource.Phone = record.Phone ?? resource.Phone;
                    resource.Website = record.Website ?? resource.Website;
                    resource.Latitude = record.Latitude ?? resource.Latitude;
                    resource.Longitude = record.Longitude ?? resource.Longitude;
                    if (record.ResourceType.HasValue)
                    {
                        resource.Type = record.ResourceType.Value;
                    }
                }
                break;

            case nameof(ShortageAreaDesignation):
                var designation = await dbContext.ShortageAreaDesignations
                    .FirstOrDefaultAsync(d => d.Id == ingestedRecord.TargetEntityId, ct);
                if (designation is not null)
                {
                    designation.AreaName = record.AreaName?.Trim() ?? designation.AreaName;
                    designation.Discipline = record.Discipline?.Trim() ?? designation.Discipline;
                    designation.DesignationType = record.DesignationType?.Trim() ?? designation.DesignationType;
                    designation.Status = record.DesignationStatus?.Trim() ?? designation.Status;
                    designation.PopulationGroup = record.PopulationGroup?.Trim() ?? designation.PopulationGroup;
                    designation.HpsaScore = record.HpsaScore ?? designation.HpsaScore;
                    designation.PopulationToProviderRatio = record.PopulationToProviderRatio ?? designation.PopulationToProviderRatio;
                    designation.SourceUpdatedAt = record.SourceDate ?? designation.SourceUpdatedAt;
                }
                break;

            case nameof(WaterSystemViolation):
                var violation = await dbContext.WaterSystemViolations
                    .Include(v => v.WaterSystem)
                    .FirstOrDefaultAsync(v => v.Id == ingestedRecord.TargetEntityId, ct);
                if (violation is not null)
                {
                    violation.ViolationCategory = record.ViolationCategory?.Trim() ?? violation.ViolationCategory;
                    violation.RuleName = record.RuleName?.Trim() ?? violation.RuleName;
                    violation.ContaminantName = record.ContaminantName?.Trim() ?? violation.ContaminantName;
                    violation.Summary = record.Summary?.Trim() ?? violation.Summary;
                    violation.IsOpen = record.IsOpenViolation ?? violation.IsOpen;
                    violation.IdentifiedAt = record.IdentifiedAt ?? violation.IdentifiedAt;
                    violation.ResolvedAt = record.ResolvedAt ?? violation.ResolvedAt;
                    violation.SourceUpdatedAt = record.SourceDate ?? violation.SourceUpdatedAt;

                    violation.WaterSystem.Name = record.WaterSystemName?.Trim() ?? violation.WaterSystem.Name;
                    violation.WaterSystem.SystemType = record.WaterSystemType?.Trim() ?? violation.WaterSystem.SystemType;
                    violation.WaterSystem.Address = record.WaterSystemAddress?.Trim() ?? violation.WaterSystem.Address;
                    violation.WaterSystem.City = record.WaterSystemCity?.Trim() ?? violation.WaterSystem.City;
                    violation.WaterSystem.State = record.WaterSystemState?.Trim() ?? violation.WaterSystem.State;
                    violation.WaterSystem.PostalCode = record.WaterSystemPostalCode?.Trim() ?? violation.WaterSystem.PostalCode;
                    violation.WaterSystem.CountyServed = record.CountyServed?.Trim() ?? violation.WaterSystem.CountyServed;
                    violation.WaterSystem.PopulationServed = record.PopulationServed ?? violation.WaterSystem.PopulationServed;
                }
                break;
        }
    }

    private static string GetTargetEntityTypeName(NormalizedRecordType recordType)
    {
        return recordType switch
        {
            NormalizedRecordType.TrendDataPoint => nameof(DiseaseTrend),
            NormalizedRecordType.NewsArticle => nameof(NewsItem),
            NormalizedRecordType.LocalResourceEntry => nameof(LocalResource),
            NormalizedRecordType.ShortageAreaDesignation => nameof(ShortageAreaDesignation),
            NormalizedRecordType.DrinkingWaterViolation => nameof(WaterSystemViolation),
            _ => nameof(NewsItem)
        };
    }

    private static string ComputeHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void MarkSuccess(FeedSource source, FeedSyncLog syncLog)
    {
        var hasSkipped = syncLog.RecordsSkippedUnmappable > 0;
        var status = hasSkipped ? FeedSyncStatus.PartialSuccess : FeedSyncStatus.Success;

        syncLog.Status = status;
        syncLog.CompletedAt = DateTime.UtcNow;

        source.LastSyncCompletedAt = DateTime.UtcNow;
        source.LastSyncStatus = status;
        source.LastSyncError = null;
        source.ConsecutiveFailureCount = 0;
        source.UpdatedAt = DateTime.UtcNow;
    }

    private static void MarkFailed(
        FeedSource source, FeedSyncLog syncLog, string error, string? stackTrace = null)
    {
        syncLog.Status = FeedSyncStatus.Failed;
        syncLog.CompletedAt = DateTime.UtcNow;
        syncLog.ErrorMessage = error.Length > 4000 ? error[..4000] : error;
        syncLog.ErrorStackTrace = stackTrace;

        source.LastSyncCompletedAt = DateTime.UtcNow;
        source.LastSyncStatus = FeedSyncStatus.Failed;
        source.LastSyncError = error.Length > 2000 ? error[..2000] : error;
        source.ConsecutiveFailureCount++;
        source.UpdatedAt = DateTime.UtcNow;
    }
}
