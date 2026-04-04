using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class IngestionServiceTests
{
    [Fact]
    public async Task ExecuteSyncAsync_CreatesEntitiesFromNewRecords()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);
        var connector = new FakeConnector(
        [
            new NormalizedFeedRecord
            {
                ExternalSourceId = "test:1",
                RawPayloadJson = "{\"id\":1,\"value\":42}",
                RecordType = NormalizedRecordType.TrendDataPoint,
                Disease = "Flu",
                JurisdictionName = "Texas",
                CaseCount = 42,
                DataDate = DateTime.UtcNow.AddDays(-1),
                SourceDate = DateTime.UtcNow,
                SourceAttribution = "TestSource"
            }
        ]);

        var service = CreateIngestionService(db, connector);
        var syncLog = await service.ExecuteSyncAsync(feedSource, CancellationToken.None);

        Assert.Equal(FeedSyncStatus.Success, syncLog.Status);
        Assert.Equal(1, syncLog.RecordsFetched);
        Assert.Equal(1, syncLog.RecordsCreated);
        Assert.Equal(0, syncLog.RecordsSkippedDuplicate);

        // Verify ingested record was created
        var ingestedRecord = await db.IngestedRecords.SingleAsync();
        Assert.Equal("test:1", ingestedRecord.ExternalSourceId);
        Assert.Equal(feedSource.Id, ingestedRecord.FeedSourceId);
    }

    [Fact]
    public async Task ExecuteSyncAsync_SkipsDuplicateRecords()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);
        var record = new NormalizedFeedRecord
        {
            ExternalSourceId = "test:dup",
            RawPayloadJson = "{\"id\":1}",
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = "Flu",
            JurisdictionName = "Texas",
            CaseCount = 10,
            DataDate = DateTime.UtcNow,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "TestSource"
        };

        // First sync
        var connector1 = new FakeConnector([record]);
        var service1 = CreateIngestionService(db, connector1);
        await service1.ExecuteSyncAsync(feedSource, CancellationToken.None);

        // Second sync with same record
        var connector2 = new FakeConnector([record]);
        var service2 = CreateIngestionService(db, connector2);
        var syncLog = await service2.ExecuteSyncAsync(feedSource, CancellationToken.None);

        Assert.Equal(1, syncLog.RecordsSkippedDuplicate);
        Assert.Equal(0, syncLog.RecordsCreated);
    }

    [Fact]
    public async Task ExecuteSyncAsync_UpdatesRecordWhenPayloadChanges()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);

        // First sync
        var record1 = new NormalizedFeedRecord
        {
            ExternalSourceId = "test:change",
            RawPayloadJson = "{\"id\":1,\"count\":10}",
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = "Flu",
            JurisdictionName = "Texas",
            CaseCount = 10,
            DataDate = DateTime.UtcNow,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "TestSource"
        };
        var connector1 = new FakeConnector([record1]);
        var service1 = CreateIngestionService(db, connector1);
        await service1.ExecuteSyncAsync(feedSource, CancellationToken.None);

        // Second sync with changed payload
        var record2 = new NormalizedFeedRecord
        {
            ExternalSourceId = "test:change",
            RawPayloadJson = "{\"id\":1,\"count\":25}",
            RecordType = NormalizedRecordType.TrendDataPoint,
            Disease = "Flu",
            JurisdictionName = "Texas",
            CaseCount = 25,
            DataDate = DateTime.UtcNow,
            SourceDate = DateTime.UtcNow,
            SourceAttribution = "TestSource"
        };
        var connector2 = new FakeConnector([record2]);
        var service2 = CreateIngestionService(db, connector2);
        var syncLog = await service2.ExecuteSyncAsync(feedSource, CancellationToken.None);

        Assert.Equal(1, syncLog.RecordsUpdated);
        Assert.Equal(0, syncLog.RecordsCreated);
    }

    [Fact]
    public async Task ExecuteSyncAsync_SkipsUnmappableJurisdictions()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);
        var connector = new FakeConnector(
        [
            new NormalizedFeedRecord
            {
                ExternalSourceId = "test:unknown",
                RawPayloadJson = "{\"id\":1}",
                RecordType = NormalizedRecordType.TrendDataPoint,
                Disease = "Flu",
                JurisdictionName = "Atlantis",
                CaseCount = 5,
                DataDate = DateTime.UtcNow,
                SourceDate = DateTime.UtcNow,
                SourceAttribution = "TestSource"
            }
        ]);

        var service = CreateIngestionService(db, connector);
        var syncLog = await service.ExecuteSyncAsync(feedSource, CancellationToken.None);

        Assert.Equal(FeedSyncStatus.PartialSuccess, syncLog.Status);
        Assert.Equal(1, syncLog.RecordsSkippedUnmappable);
        Assert.Equal(0, syncLog.RecordsCreated);
    }

    [Fact]
    public async Task ExecuteSyncAsync_MarksFailedOnConnectorError()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);
        var connector = new FakeConnector(errorMessage: "Connection refused");

        var service = CreateIngestionService(db, connector);
        var syncLog = await service.ExecuteSyncAsync(feedSource, CancellationToken.None);

        Assert.Equal(FeedSyncStatus.Failed, syncLog.Status);
        Assert.Equal("Connection refused", syncLog.ErrorMessage);
        Assert.Equal(1, feedSource.ConsecutiveFailureCount);
    }

    [Fact]
    public async Task ExecuteSyncAsync_CreatesNewsItemWithFactCheckForRssRecords()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);

        // RSS records need a region or they'll be skipped as unmappable
        // Use null jurisdiction — they'll be skipped. Instead use a known one.
        var connector = new FakeConnector(
        [
            new NormalizedFeedRecord
            {
                ExternalSourceId = "rss:article-1",
                RawPayloadJson = "{\"title\":\"MMWR Report\"}",
                RecordType = NormalizedRecordType.NewsArticle,
                Title = "Weekly Disease Summary",
                Summary = "This week's disease report.",
                SourceUrl = "https://cdc.gov/mmwr/123",
                SourceDate = DateTime.UtcNow,
                SourceAttribution = "CDC MMWR",
                JurisdictionName = "Texas"
            }
        ]);

        var service = CreateIngestionService(db, connector);
        var syncLog = await service.ExecuteSyncAsync(feedSource, CancellationToken.None);

        Assert.Equal(1, syncLog.RecordsCreated);

        var newsItem = await db.NewsItems.Include(n => n.FactCheck).SingleAsync();
        Assert.Equal("Weekly Disease Summary", newsItem.Headline);
        Assert.NotNull(newsItem.FactCheck);
        Assert.Equal(FactCheckStatus.Verified, newsItem.FactCheck.Status);
    }

    [Fact]
    public async Task ExecuteSyncAsync_CreatesAuditLogEntries()
    {
        await using var db = CreateDbContext();
        var feedSource = await CreateFeedSourceAsync(db);
        var connector = new FakeConnector(
        [
            new NormalizedFeedRecord
            {
                ExternalSourceId = "test:audit",
                RawPayloadJson = "{\"id\":1}",
                RecordType = NormalizedRecordType.TrendDataPoint,
                Disease = "Flu",
                JurisdictionName = "Texas",
                CaseCount = 5,
                DataDate = DateTime.UtcNow,
                SourceDate = DateTime.UtcNow,
                SourceAttribution = "TestSource"
            }
        ]);

        var service = CreateIngestionService(db, connector);
        await service.ExecuteSyncAsync(feedSource, CancellationToken.None);

        var auditEntries = await db.AuditLogEntries
            .Where(a => a.Action == AuditLogAction.FeedIngest)
            .ToListAsync();

        Assert.True(auditEntries.Count >= 1);
    }

    private static IngestionService CreateIngestionService(AppDbContext db, FakeConnector connector)
    {
        var options = Options.Create(new FeedIngestionOptions());
        var regionMapping = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);
        var thresholdService = new AlertThresholdService(db, options, NullLogger<AlertThresholdService>.Instance);

        return new IngestionService(
            db,
            [connector],
            regionMapping,
            thresholdService,
            options,
            NullLogger<IngestionService>.Instance);
    }

    private static async Task<FeedSource> CreateFeedSourceAsync(AppDbContext db)
    {
        var source = new FeedSource
        {
            Name = "Test Feed",
            Type = FeedSourceType.CdcSocrata,
            Url = "test-dataset",
            PollingInterval = TimeSpan.FromHours(6),
            IsEnabled = true,
            LastSyncStatus = FeedSyncStatus.NeverRun
        };
        db.FeedSources.Add(source);
        await db.SaveChangesAsync();
        return source;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Regions.Add(new Region { Name = "Texas", Type = RegionType.State, State = "TX" });
        db.SaveChanges();

        return db;
    }

    private sealed class FakeConnector : IFeedConnector
    {
        private readonly FeedFetchResult _result;

        public FakeConnector(IReadOnlyList<NormalizedFeedRecord>? records = null, string? errorMessage = null)
        {
            _result = errorMessage is not null
                ? FeedFetchResult.Failure(errorMessage)
                : FeedFetchResult.Success(records ?? []);
        }

        public FeedSourceType SourceType => FeedSourceType.CdcSocrata;

        public Task<FeedFetchResult> FetchAsync(FeedSource source, CancellationToken ct) =>
            Task.FromResult(_result);
    }
}
