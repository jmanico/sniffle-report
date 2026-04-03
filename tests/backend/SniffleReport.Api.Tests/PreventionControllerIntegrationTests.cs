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

public sealed class PreventionControllerIntegrationTests : IClassFixture<PreventionApiFactory>
{
    private readonly PreventionApiFactory _factory;

    public PreventionControllerIntegrationTests(PreventionApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DetailEndpoint_ReturnsAssociatedCostTiers()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/regions/{_factory.RegionId}/prevention/{_factory.GuideId}");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PreventionDetailDto>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload!.CostTiers.Count);
        Assert.Contains(payload.CostTiers, tier => tier.Type == CostTierType.Free);
        Assert.Contains(payload.CostTiers, tier => tier.Type == CostTierType.Promotional);
    }
}

public sealed class PreventionApiFactory : WebApplicationFactory<Program>
{
    public Guid RegionId { get; private set; }

    public Guid GuideId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("prevention-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
            var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };

            var guide = new PreventionGuide
            {
                Region = travis,
                Disease = "Flu",
                Title = "County prevention guide",
                Content = "Sample prevention content",
                CostTiers =
                [
                    new CostTier { Type = CostTierType.Free, Price = 0m, Provider = "County event" },
                    new CostTier { Type = CostTierType.Promotional, Price = 10m, Provider = "Retail partner", Notes = "Limited-time sample offer" }
                ]
            };

            dbContext.Regions.AddRange(texas, travis);
            dbContext.PreventionGuides.Add(guide);
            dbContext.SaveChanges();

            RegionId = travis.Id;
            GuideId = guide.Id;
        });
    }
}
