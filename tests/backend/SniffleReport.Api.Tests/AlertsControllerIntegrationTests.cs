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

public sealed class AlertsControllerIntegrationTests : IClassFixture<AlertsApiFactory>
{
    private readonly AlertsApiFactory _factory;

    public AlertsControllerIntegrationTests(AlertsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegionQuery_DoesNotReturnAnotherRegionsAlerts()
    {
        using var client = _factory.CreateClient();
        var travisId = _factory.TravisCountyId;

        var response = await client.GetAsync($"/api/v1/regions/{travisId}/alerts");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<AlertListDto>>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.DoesNotContain(payload, alert => alert.Title == "Cook County alert");
    }
}

public sealed class AlertsApiFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("alerts-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
            var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
            var zip = new Region { Name = "78701", Type = RegionType.Zip, State = "TX", Parent = travis };
            var cook = new Region { Name = "Cook County", Type = RegionType.County, State = "IL" };

            dbContext.Regions.AddRange(texas, travis, zip, cook);

            dbContext.HealthAlerts.AddRange(
                new HealthAlert
                {
                    Region = travis,
                    Title = "Travis County alert",
                    Disease = "Norovirus",
                    Summary = "Scoped alert",
                    Severity = AlertSeverity.Moderate,
                    CaseCount = 21,
                    SourceAttribution = "Sample Data — Not Real Health Information",
                    SourceDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Status = AlertStatus.Published,
                    CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new HealthAlert
                {
                    Region = zip,
                    Title = "Downtown zip alert",
                    Disease = "Norovirus",
                    Summary = "Child region alert",
                    Severity = AlertSeverity.Low,
                    CaseCount = 6,
                    SourceAttribution = "Sample Data — Not Real Health Information",
                    SourceDate = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    Status = AlertStatus.Published,
                    CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc)
                },
                new HealthAlert
                {
                    Region = cook,
                    Title = "Cook County alert",
                    Disease = "Norovirus",
                    Summary = "Wrong region alert",
                    Severity = AlertSeverity.High,
                    CaseCount = 44,
                    SourceAttribution = "Sample Data — Not Real Health Information",
                    SourceDate = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                    Status = AlertStatus.Published,
                    CreatedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc)
                });

            dbContext.SaveChanges();

            TravisCountyId = travis.Id;
        });
    }
}
