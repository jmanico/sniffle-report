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

public sealed class RegionsControllerIntegrationTests : IClassFixture<RegionsApiFactory>
{
    private readonly RegionsApiFactory _factory;

    public RegionsControllerIntegrationTests(RegionsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchEndpoint_ReturnsMatchingRegions()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/regions/search?q=travis");

        response.EnsureSuccessStatusCode();
        Assert.Equal("1", response.Headers.GetValues("X-Total-Count").Single());

        var payload = await response.Content.ReadFromJsonAsync<List<RegionListDto>>();

        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("Travis County", payload[0].Name);
    }
}

public sealed class RegionsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("regions-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
            var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };

            dbContext.Regions.AddRange(texas, travis);
            dbContext.SaveChanges();
        });
    }
}
