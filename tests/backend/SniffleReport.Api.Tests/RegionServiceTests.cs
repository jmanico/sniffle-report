using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class RegionServiceTests
{
    [Fact]
    public async Task GetAllAsync_FiltersByType()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionService(dbContext);

        var results = await service.GetAllAsync(RegionType.County);

        Assert.Single(results);
        Assert.Equal("Travis County", results[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_LoadsParentAndChildren()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionService(dbContext);

        var county = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");
        var result = await service.GetByIdAsync(county.Id);

        Assert.NotNull(result);
        Assert.Equal("Texas", result!.Parent!.Name);
        Assert.Single(result.Children);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingRegions()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionService(dbContext);

        var results = await service.SearchAsync("travis");

        Assert.Single(results);
        Assert.Equal("Travis County", results[0].Name);
    }

    [Fact]
    public async Task GetByZipAsync_ReturnsZipRegion()
    {
        await using var dbContext = CreateDbContext();
        var service = new RegionService(dbContext);

        var result = await service.GetByZipAsync("78701");

        Assert.NotNull(result);
        Assert.Equal(RegionType.Zip, result!.Type);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);

        var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
        var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
        var zip = new Region { Name = "78701", Type = RegionType.Zip, State = "TX", Parent = travis };

        dbContext.Regions.AddRange(texas, travis, zip);
        dbContext.SaveChanges();

        return dbContext;
    }
}
