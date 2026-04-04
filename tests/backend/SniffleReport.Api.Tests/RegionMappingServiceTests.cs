using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class RegionMappingServiceTests
{
    [Fact]
    public async Task ResolveRegionIdAsync_MatchesStateByName()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("Texas", CancellationToken.None);

        Assert.NotNull(result);
        var texas = await db.Regions.SingleAsync(r => r.Name == "Texas");
        Assert.Equal(texas.Id, result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_MatchesStateByCode()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("TX", CancellationToken.None);

        Assert.NotNull(result);
        var texas = await db.Regions.SingleAsync(r => r.Name == "Texas");
        Assert.Equal(texas.Id, result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_MatchesCountyByName()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("Travis County", CancellationToken.None);

        Assert.NotNull(result);
        var travis = await db.Regions.SingleAsync(r => r.Name == "Travis County");
        Assert.Equal(travis.Id, result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_IsCaseInsensitive()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("texas", CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_ReturnsNullForUnknownJurisdiction()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("Narnia", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_ReturnsNullForNullInput()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync(null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateCache_ForcesReload()
    {
        await using var db = CreateDbContext();
        var service = new RegionMappingService(db, NullLogger<RegionMappingService>.Instance);

        // Load cache
        await service.ResolveRegionIdAsync("Texas", CancellationToken.None);

        // Add a new region
        db.Regions.Add(new Region { Name = "New State", Type = RegionType.State, State = "NS" });
        await db.SaveChangesAsync();

        // Without invalidation, cache is stale
        service.InvalidateCache();

        var result = await service.ResolveRegionIdAsync("New State", CancellationToken.None);
        Assert.NotNull(result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
        var illinois = new Region { Name = "Illinois", Type = RegionType.State, State = "IL" };
        var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
        var cook = new Region { Name = "Cook County", Type = RegionType.County, State = "IL", Parent = illinois };

        db.Regions.AddRange(texas, illinois, travis, cook);
        db.SaveChanges();

        return db;
    }
}
