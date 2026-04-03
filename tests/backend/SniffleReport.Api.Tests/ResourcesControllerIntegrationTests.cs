using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class ResourcesControllerIntegrationTests : IClassFixture<ResourcesApiFactory>
{
    private readonly ResourcesApiFactory _factory;

    public ResourcesControllerIntegrationTests(ResourcesApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResourceQuery_FiltersByTypeWithinScopedRegion()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/regions/{_factory.TravisCountyId}/resources?type=Pharmacy");

        response.EnsureSuccessStatusCode();
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Total-Count")));

        var payload = await response.Content.ReadFromJsonAsync<List<ResourceListDto>>();

        Assert.NotNull(payload);
        var resource = Assert.Single(payload!);
        Assert.Equal("Downtown Pharmacy", resource.Name);
        Assert.Equal(ResourceType.Pharmacy, resource.Type);
    }
}

public sealed class ResourcesApiFactory : WebApplicationFactory<Program>
{
    public Guid TravisCountyId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("resources-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
            var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
            var zip = new Region { Name = "78701", Type = RegionType.Zip, State = "TX", Parent = travis };
            var cook = new Region { Name = "Cook County", Type = RegionType.County, State = "IL" };

            dbContext.Regions.AddRange(texas, travis, zip, cook);
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
                    Region = cook,
                    Name = "Cook County Pharmacy",
                    Type = ResourceType.Pharmacy,
                    Address = "333 Lake Shore Dr",
                    Latitude = 41.8781,
                    Longitude = -87.6298,
                    HoursJson = "{\"mon\":\"9:00-18:00\"}",
                    ServicesJson = "[\"covid-vaccine\"]"
                });

            dbContext.SaveChanges();

            TravisCountyId = travis.Id;
        });
    }
}
