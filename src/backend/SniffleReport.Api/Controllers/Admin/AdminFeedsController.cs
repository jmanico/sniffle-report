using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;

namespace SniffleReport.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/feeds")]
public sealed class AdminFeedsController(
    AppDbContext dbContext,
    IngestionService ingestionService,
    IValidator<CreateFeedSourceRequest> createValidator,
    IValidator<UpdateFeedSourceRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeedSourceListDto>>> GetFeeds(
        CancellationToken ct)
    {
        var sources = await dbContext.FeedSources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new FeedSourceListDto
            {
                Id = s.Id,
                Name = s.Name,
                Type = s.Type,
                Url = s.Url,
                IsEnabled = s.IsEnabled,
                LastSyncStatus = s.LastSyncStatus,
                LastSyncCompletedAt = s.LastSyncCompletedAt,
                ConsecutiveFailureCount = s.ConsecutiveFailureCount
            })
            .ToListAsync(ct);

        return Ok(sources);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FeedSourceDetailDto>> GetFeedById(Guid id, CancellationToken ct)
    {
        var source = await dbContext.FeedSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (source is null)
            return NotFound();

        var recentLogs = await dbContext.FeedSyncLogs
            .Where(l => l.FeedSourceId == id)
            .OrderByDescending(l => l.StartedAt)
            .Take(20)
            .AsNoTracking()
            .Select(l => new FeedSyncLogDto
            {
                Id = l.Id,
                StartedAt = l.StartedAt,
                CompletedAt = l.CompletedAt,
                Status = l.Status,
                RecordsFetched = l.RecordsFetched,
                RecordsCreated = l.RecordsCreated,
                RecordsUpdated = l.RecordsUpdated,
                RecordsSkippedDuplicate = l.RecordsSkippedDuplicate,
                RecordsSkippedUnmappable = l.RecordsSkippedUnmappable,
                AlertsPromoted = l.AlertsPromoted,
                ErrorMessage = l.ErrorMessage
            })
            .ToListAsync(ct);

        return Ok(new FeedSourceDetailDto
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            Url = source.Url,
            SoqlQuery = source.SoqlQuery,
            PollingInterval = source.PollingInterval,
            IsEnabled = source.IsEnabled,
            LastSyncStartedAt = source.LastSyncStartedAt,
            LastSyncCompletedAt = source.LastSyncCompletedAt,
            LastSyncStatus = source.LastSyncStatus,
            LastSyncError = source.LastSyncError,
            ConsecutiveFailureCount = source.ConsecutiveFailureCount,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            RecentSyncLogs = recentLogs
        });
    }

    [HttpPost]
    public async Task<ActionResult<FeedSourceDetailDto>> CreateFeed(
        [FromBody] CreateFeedSourceRequest request,
        CancellationToken ct)
    {
        var validationResult = await createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));

        var source = new FeedSource
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Url = request.Url.Trim(),
            SoqlQuery = request.SoqlQuery?.Trim(),
            PollingInterval = TimeSpan.FromMinutes(request.PollingIntervalMinutes),
            IsEnabled = request.IsEnabled,
            LastSyncStatus = FeedSyncStatus.NeverRun
        };
        dbContext.FeedSources.Add(source);
        await dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetFeedById), new { id = source.Id }, new FeedSourceDetailDto
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            Url = source.Url,
            SoqlQuery = source.SoqlQuery,
            PollingInterval = source.PollingInterval,
            IsEnabled = source.IsEnabled,
            LastSyncStatus = source.LastSyncStatus,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            RecentSyncLogs = []
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FeedSourceDetailDto>> UpdateFeed(
        Guid id,
        [FromBody] UpdateFeedSourceRequest request,
        CancellationToken ct)
    {
        var validationResult = await updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));

        var source = await dbContext.FeedSources.FindAsync([id], ct);
        if (source is null)
            return NotFound();

        source.Name = request.Name.Trim();
        source.Url = request.Url.Trim();
        source.SoqlQuery = request.SoqlQuery?.Trim();
        source.PollingInterval = TimeSpan.FromMinutes(request.PollingIntervalMinutes);
        source.IsEnabled = request.IsEnabled;
        source.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Ok(new FeedSourceDetailDto
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            Url = source.Url,
            SoqlQuery = source.SoqlQuery,
            PollingInterval = source.PollingInterval,
            IsEnabled = source.IsEnabled,
            LastSyncStartedAt = source.LastSyncStartedAt,
            LastSyncCompletedAt = source.LastSyncCompletedAt,
            LastSyncStatus = source.LastSyncStatus,
            LastSyncError = source.LastSyncError,
            ConsecutiveFailureCount = source.ConsecutiveFailureCount,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            RecentSyncLogs = []
        });
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<ActionResult<FeedSyncLogDto>> TriggerSync(Guid id, CancellationToken ct)
    {
        var source = await dbContext.FeedSources.FindAsync([id], ct);
        if (source is null)
            return NotFound();

        if (!source.IsEnabled)
            return BadRequest(new ProblemDetails
            {
                Title = "Feed source is disabled",
                Detail = "Enable the feed source before triggering a sync."
            });

        var syncLog = await ingestionService.ExecuteSyncAsync(source, ct);

        return Ok(new FeedSyncLogDto
        {
            Id = syncLog.Id,
            StartedAt = syncLog.StartedAt,
            CompletedAt = syncLog.CompletedAt,
            Status = syncLog.Status,
            RecordsFetched = syncLog.RecordsFetched,
            RecordsCreated = syncLog.RecordsCreated,
            RecordsUpdated = syncLog.RecordsUpdated,
            RecordsSkippedDuplicate = syncLog.RecordsSkippedDuplicate,
            RecordsSkippedUnmappable = syncLog.RecordsSkippedUnmappable,
            AlertsPromoted = syncLog.AlertsPromoted,
            ErrorMessage = syncLog.ErrorMessage
        });
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IReadOnlyList<FeedSyncLogDto>>> GetSyncLogs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (!await dbContext.FeedSources.AnyAsync(s => s.Id == id, ct))
            return NotFound();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await dbContext.FeedSyncLogs
            .CountAsync(l => l.FeedSourceId == id, ct);

        Response.Headers.Append("X-Total-Count", totalCount.ToString());

        var logs = await dbContext.FeedSyncLogs
            .Where(l => l.FeedSourceId == id)
            .OrderByDescending(l => l.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(l => new FeedSyncLogDto
            {
                Id = l.Id,
                StartedAt = l.StartedAt,
                CompletedAt = l.CompletedAt,
                Status = l.Status,
                RecordsFetched = l.RecordsFetched,
                RecordsCreated = l.RecordsCreated,
                RecordsUpdated = l.RecordsUpdated,
                RecordsSkippedDuplicate = l.RecordsSkippedDuplicate,
                RecordsSkippedUnmappable = l.RecordsSkippedUnmappable,
                AlertsPromoted = l.AlertsPromoted,
                ErrorMessage = l.ErrorMessage
            })
            .ToListAsync(ct);

        return Ok(logs);
    }
}
