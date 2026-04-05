using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class PreventionServiceTests
{
    [Fact]
    public async Task GetByRegionAsync_FiltersByDiseaseAndIncludesChildRegions()
    {
        await using var dbContext = CreateDbContext();
        var service = new PreventionService(dbContext, new RegionHierarchyService(dbContext));
        var county = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");

        var results = await service.GetByRegionAsync(
            county.Id,
            new PreventionFilters
            {
                Disease = "flu",
                Page = 1,
                PageSize = 25
            });

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnGuideFromDifferentRegion()
    {
        await using var dbContext = CreateDbContext();
        var service = new PreventionService(dbContext, new RegionHierarchyService(dbContext));
        var travis = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");
        var otherGuide = await dbContext.PreventionGuides.SingleAsync(guide => guide.Title == "Chicago flu guide");

        var result = await service.GetByIdAsync(travis.Id, otherGuide.Id);

        Assert.Null(result);
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
        var chicago = new Region { Name = "Chicago Metro", Type = RegionType.Metro, State = "IL" };

        var travisGuide = new PreventionGuide
        {
            Region = travis,
            Disease = "Flu",
            Title = "Travis flu guide",
            Content = "County guide",
            CostTiers =
            [
                new CostTier { Type = CostTierType.Free, Price = 0m, Provider = "County clinic" }
            ]
        };

        var zipGuide = new PreventionGuide
        {
            Region = zip,
            Disease = "Flu",
            Title = "Downtown flu guide",
            Content = "Zip guide",
            CostTiers =
            [
                new CostTier { Type = CostTierType.Insured, Price = 15m, Provider = "Local pharmacy" }
            ]
        };

        var chicagoGuide = new PreventionGuide
        {
            Region = chicago,
            Disease = "Flu",
            Title = "Chicago flu guide",
            Content = "Metro guide",
            CostTiers =
            [
                new CostTier { Type = CostTierType.OutOfPocket, Price = 45m, Provider = "Metro clinic" }
            ]
        };

        dbContext.Regions.AddRange(texas, travis, zip, chicago);
        dbContext.PreventionGuides.AddRange(travisGuide, zipGuide, chicagoGuide);
        dbContext.SaveChanges();

        return dbContext;
    }
}
