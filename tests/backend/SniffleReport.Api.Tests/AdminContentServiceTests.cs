using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;

using Xunit;

namespace SniffleReport.Api.Tests;

public sealed class AdminContentServiceTests
{
    [Fact]
    public async Task CreatePreventionGuideAsync_PersistsCostTiersAndAudit()
    {
        await using var dbContext = CreateDbContext();
        var service = new PreventionService(dbContext, new RegionHierarchyService(dbContext));
        var regionId = await dbContext.Regions.Select(region => region.Id).FirstAsync();

        var created = await service.CreateAsync(new CreatePreventionGuideRequest
        {
            RegionId = regionId,
            Disease = "Measles",
            Title = "New guide",
            Content = "Long-form prevention guidance",
            CostTiers =
            [
                new AdminCostTierInput
                {
                    Type = CostTierType.Free,
                    Price = 0,
                    Provider = "County clinic"
                }
            ]
        });

        Assert.Equal("New guide", created.Title);
        Assert.Single(created.CostTiers);
        Assert.Equal(2, await dbContext.PreventionGuides.IgnoreQueryFilters().CountAsync());
        Assert.Equal(AuditLogAction.Create, (await dbContext.AuditLogEntries.SingleAsync()).Action);
    }

    [Fact]
    public async Task UpdateResourceAsync_UpdatesStructuredFieldsAndAudit()
    {
        await using var dbContext = CreateDbContext();
        var service = new ResourceService(dbContext, new RegionHierarchyService(dbContext));
        var resourceId = await dbContext.LocalResources.Select(resource => resource.Id).FirstAsync();
        var regionId = await dbContext.Regions.Select(region => region.Id).FirstAsync();

        var updated = await service.UpdateAsync(resourceId, new UpdateResourceRequest
        {
            RegionId = regionId,
            Name = "Updated clinic",
            Type = ResourceType.Hospital,
            Address = "200 Main St",
            Phone = "512-555-1212",
            Website = "https://example.org/updated",
            Latitude = 30.1,
            Longitude = -97.7,
            Hours = new ResourceHoursDto { Mon = "8-5" },
            Services = ["vaccines", "testing"]
        });

        Assert.NotNull(updated);
        Assert.Equal("Updated clinic", updated!.Name);
        Assert.Contains("vaccines", updated.ServicesJson);
        Assert.Equal(AuditLogAction.Update, (await dbContext.AuditLogEntries.SingleAsync()).Action);
    }

    [Fact]
    public async Task SoftDeleteNewsItemAsync_SetsSoftDeleteFieldsAndAudit()
    {
        await using var dbContext = CreateDbContext();
        var service = new NewsService(dbContext, new RegionHierarchyService(dbContext));
        var newsItemId = await dbContext.NewsItems.Select(item => item.Id).FirstAsync();

        var deleted = await service.SoftDeleteAsync(newsItemId, "Retiring outdated article");

        Assert.True(deleted);

        var newsItem = await dbContext.NewsItems.IgnoreQueryFilters().SingleAsync(item => item.Id == newsItemId);
        Assert.True(newsItem.IsDeleted);
        Assert.NotNull(newsItem.DeletedAt);

        var auditEntry = await dbContext.AuditLogEntries.SingleAsync();
        Assert.Equal(AuditLogAction.Delete, auditEntry.Action);
        Assert.Equal("Retiring outdated article", auditEntry.Justification);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        var region = new Region { Name = "Travis County", Type = RegionType.County, State = "TX" };

        var guide = new PreventionGuide
        {
            Region = region,
            Disease = "Flu",
            Title = "Existing guide",
            Content = "Existing prevention guidance",
            CostTiers =
            [
                new CostTier
                {
                    Type = CostTierType.Insured,
                    Price = 20,
                    Provider = "Primary care"
                }
            ]
        };

        var resource = new LocalResource
        {
            Region = region,
            Name = "Existing clinic",
            Type = ResourceType.Clinic,
            Address = "100 Main St",
            Website = "https://example.org/clinic",
            HoursJson = "{\"mon\":\"9-5\"}",
            ServicesJson = "[\"testing\"]"
        };

        var newsItem = new NewsItem
        {
            Region = region,
            Headline = "Existing article",
            Content = "Content",
            SourceUrl = "https://example.org/news",
            PublishedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        dbContext.Regions.Add(region);
        dbContext.PreventionGuides.Add(guide);
        dbContext.LocalResources.Add(resource);
        dbContext.NewsItems.Add(newsItem);
        dbContext.SaveChanges();

        return dbContext;
    }
}
