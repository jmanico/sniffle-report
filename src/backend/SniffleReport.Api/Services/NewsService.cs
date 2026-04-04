using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.DTOs;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Services;

public sealed class NewsService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<NewsItem>> GetAdminNewsItemsAsync(
        GetAdminNewsItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminFilteredQuery(query)
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminNewsItemsAsync(
        GetAdminNewsItemsQuery query,
        CancellationToken cancellationToken = default)
    {
        return BuildAdminFilteredQuery(query).CountAsync(cancellationToken);
    }

    public Task<NewsItem?> GetAdminByIdAsync(Guid newsItemId, CancellationToken cancellationToken = default)
    {
        return dbContext.NewsItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(item => item.FactCheck)
            .SingleOrDefaultAsync(item => item.Id == newsItemId, cancellationToken);
    }

    public async Task<NewsItem> CreateAsync(
        CreateNewsItemRequest request,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var newsItem = new NewsItem
        {
            RegionId = request.RegionId,
            Headline = request.Headline.Trim(),
            Content = request.Content.Trim(),
            SourceUrl = request.SourceUrl.Trim(),
            PublishedAt = request.PublishedAt
        };

        dbContext.NewsItems.Add(newsItem);
        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Create,
            nameof(NewsItem),
            newsItem.Id,
            before: null,
            after: CreateNewsSnapshot(newsItem),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return newsItem;
    }

    public async Task<NewsItem?> UpdateAsync(
        Guid newsItemId,
        UpdateNewsItemRequest request,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var newsItem = await dbContext.NewsItems
            .IgnoreQueryFilters()
            .Include(item => item.FactCheck)
            .SingleOrDefaultAsync(item => item.Id == newsItemId, cancellationToken);

        if (newsItem is null)
        {
            return null;
        }

        var before = CreateNewsSnapshot(newsItem);
        newsItem.RegionId = request.RegionId;
        newsItem.Headline = request.Headline.Trim();
        newsItem.Content = request.Content.Trim();
        newsItem.SourceUrl = request.SourceUrl.Trim();
        newsItem.PublishedAt = request.PublishedAt;

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Update,
            nameof(NewsItem),
            newsItem.Id,
            before,
            CreateNewsSnapshot(newsItem),
            justification: null));
        await dbContext.SaveChangesAsync(cancellationToken);

        return newsItem;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid newsItemId,
        string justification,
        CancellationToken cancellationToken = default,
        Guid adminId = default)
    {
        var newsItem = await dbContext.NewsItems
            .IgnoreQueryFilters()
            .Include(item => item.FactCheck)
            .SingleOrDefaultAsync(item => item.Id == newsItemId, cancellationToken);

        if (newsItem is null)
        {
            return false;
        }

        var before = CreateNewsSnapshot(newsItem);
        newsItem.IsDeleted = true;
        newsItem.DeletedAt = DateTime.UtcNow;
        newsItem.DeletedBy = adminId == Guid.Empty ? null : adminId;

        dbContext.AuditLogEntries.Add(AdminAuditLog.Create(
            adminId,
            AuditLogAction.Delete,
            nameof(NewsItem),
            newsItem.Id,
            before,
            CreateNewsSnapshot(newsItem),
            justification));
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private IQueryable<NewsItem> BuildAdminFilteredQuery(GetAdminNewsItemsQuery query)
    {
        IQueryable<NewsItem> newsItems = dbContext.NewsItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(item => item.FactCheck);

        if (query.RegionId.HasValue)
        {
            newsItems = newsItems.Where(item => item.RegionId == query.RegionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Headline))
        {
            var normalizedHeadline = query.Headline.Trim().ToLowerInvariant();
            newsItems = newsItems.Where(item => item.Headline.ToLower().Contains(normalizedHeadline));
        }

        return newsItems;
    }

    private static object CreateNewsSnapshot(NewsItem newsItem)
    {
        return new
        {
            newsItem.Id,
            newsItem.RegionId,
            newsItem.Headline,
            newsItem.Content,
            newsItem.SourceUrl,
            newsItem.PublishedAt,
            newsItem.CreatedAt,
            newsItem.IsDeleted,
            newsItem.DeletedAt,
            newsItem.DeletedBy,
            FactCheckStatus = newsItem.FactCheck?.Status
        };
    }
}
