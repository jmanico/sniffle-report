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

public sealed class AdminAlertsControllerIntegrationTests : IClassFixture<AdminAlertsApiFactory>
{
    private readonly AdminAlertsApiFactory _factory;

    public AdminAlertsControllerIntegrationTests(AdminAlertsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAlerts_ReturnsDraftAndPublishedAlerts()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/alerts");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<AdminAlertListDto>>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.Contains(payload, alert => alert.Status == AlertStatus.Draft);
        Assert.Equal("2", response.Headers.GetValues("X-Total-Count").Single());
    }

    [Fact]
    public async Task CreateAlert_PersistsEntity()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/admin/alerts", new CreateAlertRequest
        {
            RegionId = _factory.RegionId,
            Disease = "Norovirus",
            Title = "Created from controller test",
            Summary = "Controller create test",
            Severity = AlertSeverity.Moderate,
            CaseCount = 19,
            SourceAttribution = "Sample",
            SourceDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = AlertStatus.Draft
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await dbContext.AuditLogEntries.CountAsync(entry => entry.Action == AuditLogAction.Create));
    }

    [Fact]
    public async Task DeleteAlert_SoftDeletesEntity()
    {
        using var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/alerts/{_factory.AlertId}")
        {
            Content = JsonContent.Create(new DeleteAlertRequest { Justification = "No longer needed" })
        };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alert = await dbContext.HealthAlerts.IgnoreQueryFilters().SingleAsync(item => item.Id == _factory.AlertId);
        Assert.True(alert.IsDeleted);
        Assert.Equal(1, await dbContext.AuditLogEntries.CountAsync(entry => entry.Action == AuditLogAction.Delete));
    }
}

public sealed class AdminAlertsApiFactory : WebApplicationFactory<Program>
{
    public Guid RegionId { get; private set; }

    public Guid AlertId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("admin-alerts-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var region = new Region { Name = "Travis County", Type = RegionType.County, State = "TX" };
            dbContext.Regions.Add(region);

            var draft = new HealthAlert
            {
                Region = region,
                Title = "Draft alert",
                Disease = "Influenza",
                Summary = "Draft alert",
                Severity = AlertSeverity.Low,
                CaseCount = 6,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Draft
            };
            var published = new HealthAlert
            {
                Region = region,
                Title = "Published alert",
                Disease = "RSV",
                Summary = "Published alert",
                Severity = AlertSeverity.High,
                CaseCount = 18,
                SourceAttribution = "Sample",
                SourceDate = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                Status = AlertStatus.Published
            };

            dbContext.HealthAlerts.AddRange(draft, published);
            dbContext.SaveChanges();

            RegionId = region.Id;
            AlertId = draft.Id;
        });
    }
}
