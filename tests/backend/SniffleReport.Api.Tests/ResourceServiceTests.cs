using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class ResourceServiceTests
{
    [Fact]
    public async Task GetByIdAsync_DoesNotReturnResourceFromDifferentRegion()
    {
        await using var dbContext = CreateDbContext();
        var service = new ResourceService(dbContext, new RegionHierarchyService(dbContext));
        var travis = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");
        var chicagoResource = await dbContext.LocalResources.SingleAsync(resource => resource.Name == "Chicago Pharmacy");

        var result = await service.GetByIdAsync(travis.Id, chicagoResource.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchNearbyAsync_ReturnsOnlyResourcesWithinRadiusOrderedByDistance()
    {
        await using var dbContext = CreateDbContext();
        var service = new ResourceService(dbContext, new RegionHierarchyService(dbContext));
        var travis = await dbContext.Regions.SingleAsync(region => region.Name == "Travis County");

        var results = await service.SearchNearbyAsync(
            travis.Id,
            30.2672,
            -97.7431,
            8,
            new ResourceFilters
            {
                Page = 1,
                PageSize = 25
            });

        Assert.Equal(2, results.Count);
        Assert.Equal("Downtown Pharmacy", results[0].Name);
        Assert.Equal("County Clinic", results[1].Name);
        Assert.All(results, resource => Assert.True(resource.DistanceMiles <= 8));
    }

    [Fact]
    public void CalculateDistanceMiles_ReturnsZeroForSameCoordinates()
    {
        var distance = ResourceService.CalculateDistanceMiles(30.2672, -97.7431, 30.2672, -97.7431);

        Assert.Equal(0, distance, 6);
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

        dbContext.Regions.AddRange(texas, travis, zip, chicago);
        dbContext.LocalResources.AddRange(
            new LocalResource
            {
                Region = travis,
                Name = "County Clinic",
                Type = ResourceType.Clinic,
                Address = "111 County Rd",
                Latitude = 30.2747,
                Longitude = -97.7002,
                HoursJson = "{\"mon\":\"8:00-17:00\"}",
                ServicesJson = "[\"flu-vaccine\"]"
            },
            new LocalResource
            {
                Region = zip,
                Name = "Downtown Pharmacy",
                Type = ResourceType.Pharmacy,
                Address = "222 Downtown St",
                Latitude = 30.2685,
                Longitude = -97.7420,
                HoursJson = "{\"sat\":\"9:00-14:00\"}",
                ServicesJson = "[\"rapid-test\"]"
            },
            new LocalResource
            {
                Region = chicago,
                Name = "Chicago Pharmacy",
                Type = ResourceType.Pharmacy,
                Address = "333 Lake Shore Dr",
                Latitude = 41.8781,
                Longitude = -87.6298,
                HoursJson = "{\"mon\":\"9:00-18:00\"}",
                ServicesJson = "[\"covid-vaccine\"]"
            });

        dbContext.SaveChanges();

        return dbContext;
    }
}
