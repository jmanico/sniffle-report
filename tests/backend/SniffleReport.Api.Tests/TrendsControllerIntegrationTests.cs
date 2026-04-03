using System.Net;
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

public sealed class TrendsControllerIntegrationTests : IClassFixture<TrendsApiFactory>
{
    private readonly TrendsApiFactory _factory;

    public TrendsControllerIntegrationTests(TrendsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegionTrendQuery_ReturnsScopedAggregatedSeries()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/regions/{_factory.TravisCountyId}/trends?disease=flu");

        response.EnsureSuccessStatusCode();
        Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Total-Count")));

        var payload = await response.Content.ReadFromJsonAsync<List<TrendSeriesDto>>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.DoesNotContain(payload, series => series.AlertTitle == "Cook County measles alert");
        Assert.All(payload, series => Assert.NotEmpty(series.DataPoints));
    }

    [Fact]
    public async Task AlertTrendQuery_RejectsDateRangeLongerThanOneYear()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/regions/{_factory.TravisCountyId}/alerts/{_factory.TravisAlertId}/trends?dateFrom=2024-01-01&dateTo=2025-02-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class TrendsApiFactory : WebApplicationFactory<Program>
{
    public Guid TravisCountyId { get; private set; }

    public Guid TravisAlertId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("trends-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var texas = new Region { Name = "Texas", Type = RegionType.State, State = "TX" };
            var travis = new Region { Name = "Travis County", Type = RegionType.County, State = "TX", Parent = texas };
            var zip = new Region { Name = "78701", Type = RegionType.Zip, State = "TX", Parent = travis };
            var cook = new Region { Name = "Cook County", Type = RegionType.County, State = "IL" };

            var travisAlert = new HealthAlert
            {
                Region = travis,
                Title = "Travis flu alert",
                Disease = "Flu",
                Summary = "Scoped alert",
                Severity = AlertSeverity.Moderate,
                CaseCount = 21,
                SourceAttribution = "Sample Data",
                SourceDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published,
                CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                DiseaseTrends =
                [
                    new DiseaseTrend { Date = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc), CaseCount = 10, Source = "County report", SourceDate = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc) }
                ]
            };

            var zipAlert = new HealthAlert
            {
                Region = zip,
                Title = "Downtown flu alert",
                Disease = "Flu",
                Summary = "Child region alert",
                Severity = AlertSeverity.Low,
                CaseCount = 6,
                SourceAttribution = "Sample Data",
                SourceDate = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published,
                CreatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                DiseaseTrends =
                [
                    new DiseaseTrend { Date = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc), CaseCount = 6, Source = "Zip report", SourceDate = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc) }
                ]
            };

            var cookAlert = new HealthAlert
            {
                Region = cook,
                Title = "Cook County measles alert",
                Disease = "Measles",
                Summary = "Wrong region alert",
                Severity = AlertSeverity.High,
                CaseCount = 44,
                SourceAttribution = "Sample Data",
                SourceDate = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published,
                CreatedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc),
                DiseaseTrends =
                [
                    new DiseaseTrend { Date = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), CaseCount = 20, Source = "Cook report", SourceDate = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc) }
                ]
            };

            dbContext.Regions.AddRange(texas, travis, zip, cook);
            dbContext.HealthAlerts.AddRange(travisAlert, zipAlert, cookAlert);
            dbContext.SaveChanges();

            TravisCountyId = travis.Id;
            TravisAlertId = travisAlert.Id;
        });
    }
}
