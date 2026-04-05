using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services.Ingestion;
using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class RegionMappingFuzzyTests
{
    [Fact]
    public async Task ResolveRegionIdAsync_MatchesCountyByStateName()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionMappingService(dbContext, NullLogger<RegionMappingService>.Instance);

        // "Travis County, Texas" should match even though DB has state code "TX"
        var result = await service.ResolveRegionIdAsync("Travis County, Texas", CancellationToken.None);

        Assert.NotNull(result);
        var travis = await dbContext.Regions.SingleAsync(r => r.Name == "Travis County");
        Assert.Equal(travis.Id, result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_MatchesCountyByStateCode()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionMappingService(dbContext, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("Travis County, TX", CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_FuzzyMatchesSpacingVariation()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionMappingService(dbContext, NullLogger<RegionMappingService>.Instance);

        // "Du Page County" vs "DuPage County" — spacing difference
        var result = await service.ResolveRegionIdAsync("Du Page County, IL", CancellationToken.None);

        Assert.NotNull(result);
        var dupage = await dbContext.Regions.SingleAsync(r => r.Name == "DuPage County");
        Assert.Equal(dupage.Id, result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_FuzzyMatchesAlaskaBoroughs()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionMappingService(dbContext, NullLogger<RegionMappingService>.Instance);

        // "Anchorage County" should fuzzy-match "Anchorage Municipality"
        var result = await service.ResolveRegionIdAsync("Anchorage Municipality, AK", CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_FallsBackToNationalForNullJurisdiction()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionMappingService(dbContext, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync(null, CancellationToken.None);

        Assert.NotNull(result);
        var us = await dbContext.Regions.SingleAsync(r => r.Name == "United States");
        Assert.Equal(us.Id, result);
    }

    [Fact]
    public async Task ResolveRegionIdAsync_ReturnsNullForCompletelyUnknown()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionMappingService(dbContext, NullLogger<RegionMappingService>.Instance);

        var result = await service.ResolveRegionIdAsync("Nonexistent Place", CancellationToken.None);

        Assert.Null(result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        var us = new Region { Name = "United States", Type = RegionType.State, State = "US" };
        var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
        var illinois = new Region { Name = "Illinois", Type = RegionType.State, State = "IL" };
        var alaska = new Region { Name = "Alaska", Type = RegionType.State, State = "AK" };
        var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
        var dupage = new Region { Name = "DuPage County", Type = RegionType.County, State = "IL", Parent = illinois };
        var anchorage = new Region { Name = "Anchorage Municipality", Type = RegionType.County, State = "AK", Parent = alaska };

        dbContext.Regions.AddRange(us, texas, illinois, alaska, travis, dupage, anchorage);
        dbContext.SaveChanges();

        return dbContext;
    }
}
