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

public sealed class AdminContentControllersIntegrationTests : IClassFixture<AdminContentApiFactory>
{
    private readonly AdminContentApiFactory _factory;

    public AdminContentControllersIntegrationTests(AdminContentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPreventionGuides_ReturnsCountHeader()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/prevention");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<AdminPreventionGuideListDto>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("1", response.Headers.GetValues("X-Total-Count").Single());
    }

    [Fact]
    public async Task CreateResource_PersistsEntity()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/admin/resources", new CreateResourceRequest
        {
            RegionId = _factory.RegionId,
            Name = "New pharmacy",
            Type = ResourceType.Pharmacy,
            Address = "500 Congress Ave",
            Website = "https://example.org/pharmacy",
            Hours = new ResourceHoursDto { Mon = "10-6" },
            Services = ["flu-shots"]
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await dbContext.LocalResources.CountAsync());
        Assert.Equal(1, await dbContext.AuditLogEntries.CountAsync(entry => entry.Action == AuditLogAction.Create && entry.EntityType == nameof(LocalResource)));
    }

    [Fact]
    public async Task GetNewsItemById_ReturnsFactCheckStatus()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/admin/news/{_factory.NewsItemId}");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AdminNewsItemDetailDto>();
        Assert.NotNull(payload);
        Assert.Equal(FactCheckStatus.Verified, payload!.FactCheckStatus);
    }

    [Fact]
    public async Task DeleteNewsItem_SoftDeletesEntity()
    {
        using var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/news/{_factory.NewsItemId}")
        {
            Content = JsonContent.Create(new DeleteNewsItemRequest { Justification = "Retiring story" })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newsItem = await dbContext.NewsItems.IgnoreQueryFilters().SingleAsync(item => item.Id == _factory.NewsItemId);
        Assert.True(newsItem.IsDeleted);
    }
}

public sealed class AdminContentApiFactory : WebApplicationFactory<Program>
{
    public Guid RegionId { get; private set; }

    public Guid NewsItemId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("admin-content-controller-tests"));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var region = new Region { Name = "Travis County", Type = RegionType.County, State = "TX" };
            var guide = new PreventionGuide
            {
                Region = region,
                Disease = "Flu",
                Title = "Guide",
                Content = "Content",
                CostTiers =
                [
                    new CostTier
                    {
                        Type = CostTierType.Free,
                        Price = 0,
                        Provider = "County clinic"
                    }
                ]
            };
            var resource = new LocalResource
            {
                Region = region,
                Name = "Clinic",
                Type = ResourceType.Clinic,
                Address = "100 Main St",
                Website = "https://example.org/clinic",
                HoursJson = "{\"mon\":\"9-5\"}",
                ServicesJson = "[\"testing\"]"
            };
            var newsItem = new NewsItem
            {
                Region = region,
                Headline = "News",
                Content = "Content",
                SourceUrl = "https://example.org/news",
                PublishedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                FactCheck = new FactCheck
                {
                    Status = FactCheckStatus.Verified,
                    Verdict = "Verified",
                    SourcesJson = "[]",
                    CheckedAt = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc)
                }
            };

            dbContext.Regions.Add(region);
            dbContext.PreventionGuides.Add(guide);
            dbContext.LocalResources.Add(resource);
            dbContext.NewsItems.Add(newsItem);
            dbContext.SaveChanges();

            RegionId = region.Id;
            NewsItemId = newsItem.Id;
        });
    }
}
