using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;

namespace SniffleReport.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public DbSet<CostTier> CostTiers => Set<CostTier>();

    public DbSet<DiseaseTrend> DiseaseTrends => Set<DiseaseTrend>();

    public DbSet<FactCheck> FactChecks => Set<FactCheck>();

    public DbSet<FactCheckHistory> FactCheckHistories => Set<FactCheckHistory>();

    public DbSet<HealthAlert> HealthAlerts => Set<HealthAlert>();

    public DbSet<LocalResource> LocalResources => Set<LocalResource>();

    public DbSet<NewsItem> NewsItems => Set<NewsItem>();

    public DbSet<PreventionGuide> PreventionGuides => Set<PreventionGuide>();

    public DbSet<Region> Regions => Set<Region>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRegion(modelBuilder);
        ConfigureHealthAlert(modelBuilder);
        ConfigureDiseaseTrend(modelBuilder);
        ConfigurePreventionGuide(modelBuilder);
        ConfigureCostTier(modelBuilder);
        ConfigureLocalResource(modelBuilder);
        ConfigureNewsItem(modelBuilder);
        ConfigureFactCheck(modelBuilder);
        ConfigureFactCheckHistory(modelBuilder);
        ConfigureAuditLogEntry(modelBuilder);
    }

    private static void ConfigureRegion(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Region>();

        entity.ToTable("Regions");
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.State).HasMaxLength(100);
        entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        entity.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureHealthAlert(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<HealthAlert>();

        entity.ToTable("HealthAlerts");
        entity.Property(x => x.Disease).HasMaxLength(120);
        entity.Property(x => x.Title).HasMaxLength(200);
        entity.Property(x => x.Summary).HasMaxLength(2_000);
        entity.Property(x => x.SourceAttribution).HasMaxLength(300);
        entity.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.HasQueryFilter(x => !x.IsDeleted);
        entity.HasIndex(x => new { x.RegionId, x.CreatedAt })
            .HasDatabaseName("IX_HealthAlert_RegionId_CreatedAt");
    }

    private static void ConfigureDiseaseTrend(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DiseaseTrend>();

        entity.ToTable("DiseaseTrends");
        entity.Property(x => x.Source).HasMaxLength(300);
        entity.Property(x => x.Notes).HasMaxLength(2_000);
        entity.HasQueryFilter(x => !x.Alert.IsDeleted);
        entity.HasIndex(x => new { x.AlertId, x.Date })
            .HasDatabaseName("IX_DiseaseTrend_AlertId_Date");
    }

    private static void ConfigurePreventionGuide(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PreventionGuide>();

        entity.ToTable("PreventionGuides");
        entity.Property(x => x.Disease).HasMaxLength(120);
        entity.Property(x => x.Title).HasMaxLength(200);
        entity.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureCostTier(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CostTier>();

        entity.ToTable("CostTiers");
        entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Price).HasPrecision(10, 2);
        entity.Property(x => x.Provider).HasMaxLength(200);
        entity.Property(x => x.Notes).HasMaxLength(1_000);
        entity.HasQueryFilter(x => !x.Guide.IsDeleted);
    }

    private static void ConfigureLocalResource(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<LocalResource>();

        entity.ToTable("LocalResources");
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Address).HasMaxLength(300);
        entity.Property(x => x.Phone).HasMaxLength(40);
        entity.Property(x => x.Website).HasMaxLength(500);
        entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.HoursJson).HasColumnType("jsonb");
        entity.Property(x => x.ServicesJson).HasColumnType("jsonb");
        entity.HasIndex(x => new { x.RegionId, x.Type })
            .HasDatabaseName("IX_LocalResource_RegionId_Type");
    }

    private static void ConfigureNewsItem(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<NewsItem>();

        entity.ToTable("NewsItems");
        entity.Property(x => x.Headline).HasMaxLength(300);
        entity.Property(x => x.SourceUrl).HasMaxLength(500);
        entity.HasQueryFilter(x => !x.IsDeleted);
        entity.HasIndex(x => new { x.RegionId, x.PublishedAt })
            .HasDatabaseName("IX_NewsItem_RegionId_PublishedAt");
    }

    private static void ConfigureFactCheck(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FactCheck>();

        entity.ToTable("FactChecks");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Verdict).HasMaxLength(2_000);
        entity.Property(x => x.SourcesJson).HasColumnType("jsonb");
        entity.HasQueryFilter(x => !x.IsDeleted);
        entity.HasOne(x => x.NewsItem)
            .WithOne(x => x.FactCheck)
            .HasForeignKey<FactCheck>(x => x.NewsItemId);
        entity.HasIndex(x => x.NewsItemId).IsUnique();
    }

    private static void ConfigureFactCheckHistory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FactCheckHistory>();

        entity.ToTable("FactCheckHistories");
        entity.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Justification).HasMaxLength(2_000);
        entity.Property(x => x.SourcesJson).HasColumnType("jsonb");
        entity.HasQueryFilter(x => !x.FactCheck.IsDeleted);
    }

    private static void ConfigureAuditLogEntry(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLogEntry>();

        entity.ToTable("AuditLogEntries");
        entity.Property(x => x.Action).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.EntityType).HasMaxLength(128);
        entity.Property(x => x.BeforeJson).HasColumnType("jsonb");
        entity.Property(x => x.AfterJson).HasColumnType("jsonb");
        entity.Property(x => x.IpAddress).HasMaxLength(64);
        entity.Property(x => x.UserAgent).HasMaxLength(512);
        entity.Property(x => x.Justification).HasMaxLength(2_000);
    }
}
