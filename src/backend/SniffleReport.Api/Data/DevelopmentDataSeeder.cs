using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Models.Entities;
using SniffleReport.Api.Models.Enums;
using SniffleReport.Api.Services;
using SniffleReport.Api.Services.Snapshots;

namespace SniffleReport.Api.Data;

public static class DevelopmentDataSeeder
{
    private const string SampleSourceAttribution = "Sample Data — Not Real Health Information";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync(cancellationToken);

        await SeedRegionsAsync(context, logger, cancellationToken);
        await SeedHealthAlertsAsync(context, cancellationToken);
        await SeedDiseaseTrendsAsync(context, cancellationToken);
        await SeedPreventionGuidesAsync(context, cancellationToken);
        await SeedLocalResourcesAsync(context, cancellationToken);
        await SeedFeedSourcesAsync(context, cancellationToken);

        // Build initial region snapshots so the dashboard is ready immediately
        await BuildInitialSnapshotsAsync(scope.ServiceProvider, context, logger, cancellationToken);

        logger.LogInformation("Development seed completed for Sniffle Report.");
    }

    private static async Task SeedRegionsAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        // National-level region for feeds with no jurisdiction (RSS, national data)
        await UpsertRegionAsync(
            context,
            name: "United States",
            type: RegionType.State,
            state: "US",
            parentId: null,
            latitude: 39.8283,
            longitude: -98.5795,
            cancellationToken);

        foreach (var state in GetStates())
        {
            await UpsertRegionAsync(
                context,
                name: state.Name,
                type: RegionType.State,
                state: state.Code,
                parentId: null,
                latitude: state.Latitude,
                longitude: state.Longitude,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        var stateLookup = await context.Regions
            .Where(region => region.Type == RegionType.State)
            .ToDictionaryAsync(region => region.State, cancellationToken);

        var countyData = LoadCountyDataFromEmbeddedResource();
        logger.LogInformation("Loading {CountyCount} US counties from embedded seed data...", countyData.Count);

        // Bulk-load existing counties to avoid 3,000+ individual queries
        var existingCounties = await context.Regions
            .Where(r => r.Type == RegionType.County)
            .ToDictionaryAsync(r => (r.Name, r.State), cancellationToken);

        var added = 0;
        foreach (var county in countyData)
        {
            if (!stateLookup.TryGetValue(county.State, out var parentState))
                continue;

            var key = (county.Name, county.State);
            if (existingCounties.TryGetValue(key, out var existing))
            {
                existing.ParentId = parentState.Id;
                existing.Latitude = county.Latitude;
                existing.Longitude = county.Longitude;
            }
            else
            {
                context.Regions.Add(new Region
                {
                    Name = county.Name,
                    Type = RegionType.County,
                    State = county.State,
                    ParentId = parentState.Id,
                    Latitude = county.Latitude,
                    Longitude = county.Longitude
                });
                added++;
            }
        }

        logger.LogInformation("Added {AddedCount} new counties, updated {UpdatedCount} existing.", added, existingCounties.Count);

        foreach (var metro in GetMetros())
        {
            await UpsertRegionAsync(
                context,
                name: metro.Name,
                type: RegionType.Metro,
                state: metro.StateCode,
                parentId: null,
                latitude: metro.Latitude,
                longitude: metro.Longitude,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedHealthAlertsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var regions = await GetRegionLookupAsync(context, cancellationToken);

        foreach (var seed in GetAlertSeeds(regions))
        {
            var existing = await context.HealthAlerts
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(alert => alert.Title == seed.Title, cancellationToken);

            if (existing is null)
            {
                context.HealthAlerts.Add(seed);
                continue;
            }

            seed.Id = existing.Id;
            context.Entry(existing).CurrentValues.SetValues(seed);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDiseaseTrendsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var alertLookup = await context.HealthAlerts
            .IgnoreQueryFilters()
            .Where(alert => alert.SourceAttribution == SampleSourceAttribution)
            .ToDictionaryAsync(alert => alert.Title, cancellationToken);

        foreach (var seed in GetTrendSeeds(alertLookup))
        {
            var existing = await context.DiseaseTrends
                .SingleOrDefaultAsync(trend => trend.AlertId == seed.AlertId && trend.Date == seed.Date, cancellationToken);

            if (existing is null)
            {
                context.DiseaseTrends.Add(seed);
                continue;
            }

            seed.Id = existing.Id;
            context.Entry(existing).CurrentValues.SetValues(seed);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPreventionGuidesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var regions = await GetRegionLookupAsync(context, cancellationToken);

        foreach (var seed in GetGuideSeeds(regions))
        {
            var existing = await context.PreventionGuides
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(guide => guide.Title == seed.Title, cancellationToken);

            if (existing is null)
            {
                context.PreventionGuides.Add(seed);
                continue;
            }

            seed.Id = existing.Id;
            context.Entry(existing).CurrentValues.SetValues(seed);
        }

        await context.SaveChangesAsync(cancellationToken);

        var guideLookup = await context.PreventionGuides
            .IgnoreQueryFilters()
            .ToDictionaryAsync(guide => guide.Title, cancellationToken);

        foreach (var seed in GetCostTierSeeds(guideLookup))
        {
            var existing = await context.CostTiers
                .SingleOrDefaultAsync(
                    tier => tier.GuideId == seed.GuideId && tier.Type == seed.Type && tier.Provider == seed.Provider,
                    cancellationToken);

            if (existing is null)
            {
                context.CostTiers.Add(seed);
                continue;
            }

            seed.Id = existing.Id;
            context.Entry(existing).CurrentValues.SetValues(seed);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedLocalResourcesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var regions = await GetRegionLookupAsync(context, cancellationToken);

        foreach (var seed in GetResourceSeeds(regions))
        {
            var existing = await context.LocalResources
                .SingleOrDefaultAsync(resource => resource.Name == seed.Name, cancellationToken);

            if (existing is null)
            {
                context.LocalResources.Add(seed);
                continue;
            }

            seed.Id = existing.Id;
            context.Entry(existing).CurrentValues.SetValues(seed);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task BuildInitialSnapshotsAsync(
        IServiceProvider serviceProvider,
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var regionCount = await context.Regions.CountAsync(cancellationToken);
        var snapshotCount = await context.RegionSnapshots.CountAsync(cancellationToken);

        if (snapshotCount >= regionCount && snapshotCount > 0)
        {
            logger.LogInformation("Region snapshots are up to date ({SnapshotCount} snapshots for {RegionCount} regions).", snapshotCount, regionCount);
            return;
        }

        var hierarchyService = serviceProvider.GetRequiredService<RegionHierarchyService>();
        var snapshotOptions = serviceProvider.GetRequiredService<IOptions<SnapshotOptions>>();
        var builderLogger = serviceProvider.GetRequiredService<ILogger<RegionSnapshotBuilder>>();
        var builder = new RegionSnapshotBuilder(context, hierarchyService, snapshotOptions, builderLogger);

        logger.LogInformation("Building initial region snapshots...");
        await builder.RebuildAllAsync(cancellationToken);
        logger.LogInformation("Initial region snapshots built successfully.");
    }

    private static async Task<Dictionary<string, Region>> GetRegionLookupAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var regions = await context.Regions.ToListAsync(cancellationToken);
        var lookup = new Dictionary<string, Region>();

        foreach (var region in regions)
        {
            lookup.TryAdd(region.Name, region);
        }

        return lookup;
    }

    private static async Task UpsertRegionAsync(
        AppDbContext context,
        string name,
        RegionType type,
        string state,
        Guid? parentId,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken)
    {
        var existing = await context.Regions
            .SingleOrDefaultAsync(region => region.Name == name && region.Type == type, cancellationToken);

        if (existing is null)
        {
            context.Regions.Add(new Region
            {
                Name = name,
                Type = type,
                State = state,
                ParentId = parentId,
                Latitude = latitude,
                Longitude = longitude
            });

            return;
        }

        existing.State = state;
        existing.ParentId = parentId;
        existing.Latitude = latitude;
        existing.Longitude = longitude;
    }

    private static async Task SeedFeedSourcesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        foreach (var seed in GetFeedSourceSeeds())
        {
            var existing = await context.FeedSources
                .SingleOrDefaultAsync(f => f.Name == seed.Name, cancellationToken);

            if (existing is null)
            {
                context.FeedSources.Add(seed);
                continue;
            }

            seed.Id = existing.Id;
            context.Entry(existing).CurrentValues.SetValues(seed);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<FeedSource> GetFeedSourceSeeds()
    {
        return
        [
            new FeedSource
            {
                Name = "CDC Wastewater Surveillance",
                Type = FeedSourceType.CdcSocrata,
                Url = "2ew6-ywp6",
                SoqlQuery = "SELECT reporting_jurisdiction, date_end, ptc_15d, percentile, county_names, county_fips, population_served WHERE date_end > '2025-01-01' ORDER BY date_end DESC LIMIT 5000",
                PollingInterval = TimeSpan.FromHours(6),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "CDC NNDSS Weekly Tables",
                Type = FeedSourceType.CdcSocrata,
                Url = "x9gk-5huc",
                SoqlQuery = "SELECT states, year, week, label, m1, m2 WHERE year = '2026' ORDER BY sort_order DESC LIMIT 5000",
                PollingInterval = TimeSpan.FromHours(24),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "CDC Food Safety Alerts",
                Type = FeedSourceType.CdcRss,
                Url = "https://tools.cdc.gov/api/v2/resources/media/316422.rss",
                PollingInterval = TimeSpan.FromHours(12),
                IsEnabled = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "CDC Outbreak Alerts",
                Type = FeedSourceType.CdcRss,
                Url = "https://tools.cdc.gov/api/v2/resources/media/285676.rss",
                PollingInterval = TimeSpan.FromHours(12),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            // --- New Socrata datasets ---
            new FeedSource
            {
                Name = "CDC COVID-19 Vaccination Distribution",
                Type = FeedSourceType.CdcSocrata,
                Url = "unsk-b7fc",
                SoqlQuery = "SELECT date, location, administered, admin_per_100k WHERE date > '2025-01-01' ORDER BY date DESC LIMIT 5000",
                PollingInterval = TimeSpan.FromHours(24),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "CDC PLACES County Health",
                Type = FeedSourceType.CdcSocrata,
                Url = "swc5-untb",
                SoqlQuery = "SELECT stateabbr, locationname, category, measure, data_value, data_value_type WHERE data_value_type = 'Age-adjusted prevalence' LIMIT 5000",
                PollingInterval = TimeSpan.FromDays(7),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "CDC Provisional Drug Overdose Deaths",
                Type = FeedSourceType.CdcSocrata,
                Url = "xkb8-kh2a",
                SoqlQuery = "SELECT state, state_name, year, month, indicator, data_value, predicted_value WHERE year >= '2025' AND data_value IS NOT NULL ORDER BY year DESC, month DESC LIMIT 5000",
                PollingInterval = TimeSpan.FromDays(7),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            // --- New RSS feeds ---
            new FeedSource
            {
                Name = "FDA Drug Recalls",
                Type = FeedSourceType.CdcRss,
                Url = "https://www.fda.gov/about-fda/contact-fda/stay-informed/rss-feeds/drug-recalls/rss.xml",
                PollingInterval = TimeSpan.FromHours(12),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "FDA Food and Safety Recalls",
                Type = FeedSourceType.CdcRss,
                Url = "https://www.fda.gov/about-fda/contact-fda/stay-informed/rss-feeds/recalls/rss.xml",
                PollingInterval = TimeSpan.FromHours(12),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            // --- New REST API connectors ---
            new FeedSource
            {
                Name = "NPI Registry — Pharmacies",
                Type = FeedSourceType.NpiRegistry,
                Url = "pharmacy",
                PollingInterval = TimeSpan.FromDays(30),
                IsEnabled = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "NPI Registry — Clinics",
                Type = FeedSourceType.NpiRegistry,
                Url = "urgent care",
                PollingInterval = TimeSpan.FromDays(30),
                IsEnabled = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "NPI Registry — Hospitals",
                Type = FeedSourceType.NpiRegistry,
                Url = "hospital",
                PollingInterval = TimeSpan.FromDays(30),
                IsEnabled = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            },
            new FeedSource
            {
                Name = "openFDA Drug Enforcement",
                Type = FeedSourceType.OpenFda,
                Url = "drug/enforcement.json?limit=100&sort=report_date:desc",
                PollingInterval = TimeSpan.FromHours(24),
                IsEnabled = true,
                AutoPublish = true,
                LastSyncStatus = FeedSyncStatus.NeverRun
            }
        ];
    }

    private static IEnumerable<HealthAlert> GetAlertSeeds(IReadOnlyDictionary<string, Region> regions)
    {
        return
        [
            CreateAlert(regions["Travis County"], "Chickenpox watch in Travis County schools", "Chickenpox", AlertSeverity.Moderate, 47, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            CreateAlert(regions["Cook County"], "Cook County seasonal flu activity is elevated", "Influenza A", AlertSeverity.High, 123, new DateTime(2026, 1, 22, 0, 0, 0, DateTimeKind.Utc)),
            CreateAlert(regions["Los Angeles County"], "Norovirus cluster linked to several public events", "Norovirus", AlertSeverity.High, 61, new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)),
            CreateAlert(regions["King County"], "Localized measles exposure notification", "Measles", AlertSeverity.Low, 9, new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc)),
            CreateAlert(regions["Miami-Dade County"], "RSV cases rising across pediatric clinics", "RSV", AlertSeverity.Moderate, 58, new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc)),
            CreateAlert(regions["Atlanta Metro"], "Pertussis spike under active community monitoring", "Pertussis", AlertSeverity.Critical, 34, new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc))
        ];
    }

    private static IEnumerable<DiseaseTrend> GetTrendSeeds(IReadOnlyDictionary<string, HealthAlert> alerts)
    {
        return
        [
            CreateTrend(alerts["Chickenpox watch in Travis County schools"], new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc), 19),
            CreateTrend(alerts["Chickenpox watch in Travis County schools"], new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 31),
            CreateTrend(alerts["Chickenpox watch in Travis County schools"], new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), 47),
            CreateTrend(alerts["Cook County seasonal flu activity is elevated"], new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc), 77),
            CreateTrend(alerts["Cook County seasonal flu activity is elevated"], new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc), 103),
            CreateTrend(alerts["Cook County seasonal flu activity is elevated"], new DateTime(2026, 1, 22, 0, 0, 0, DateTimeKind.Utc), 123),
            CreateTrend(alerts["Norovirus cluster linked to several public events"], new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), 18),
            CreateTrend(alerts["Norovirus cluster linked to several public events"], new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc), 39),
            CreateTrend(alerts["Norovirus cluster linked to several public events"], new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc), 61),
            CreateTrend(alerts["Localized measles exposure notification"], new DateTime(2026, 1, 28, 0, 0, 0, DateTimeKind.Utc), 2),
            CreateTrend(alerts["Localized measles exposure notification"], new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), 5),
            CreateTrend(alerts["Localized measles exposure notification"], new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc), 9),
            CreateTrend(alerts["RSV cases rising across pediatric clinics"], new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), 22),
            CreateTrend(alerts["RSV cases rising across pediatric clinics"], new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc), 43),
            CreateTrend(alerts["RSV cases rising across pediatric clinics"], new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), 58),
            CreateTrend(alerts["Pertussis spike under active community monitoring"], new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc), 11),
            CreateTrend(alerts["Pertussis spike under active community monitoring"], new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc), 21),
            CreateTrend(alerts["Pertussis spike under active community monitoring"], new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), 34)
        ];
    }

    private static IEnumerable<PreventionGuide> GetGuideSeeds(IReadOnlyDictionary<string, Region> regions)
    {
        return
        [
            CreateGuide(regions["Texas"], "Measles prevention for families and schools", "Measles", "Review vaccination records, isolate symptomatic contacts, and use school exclusion guidance for clearly fake sample scenarios."),
            CreateGuide(regions["Illinois"], "Seasonal flu prevention and low-cost care options", "Influenza A", "Use layered prevention: vaccination, home isolation during symptoms, and early clinic contact for high-risk household members."),
            CreateGuide(regions["California"], "Norovirus cleanup and food safety reminders", "Norovirus", "Disinfect high-touch surfaces, exclude food handlers while symptomatic, and reinforce strict handwashing protocols."),
            CreateGuide(regions["Florida"], "RSV support guidance for caregivers", "RSV", "Prioritize hydration, monitor breathing changes, and use pediatric follow-up pathways for vulnerable children.")
        ];
    }

    private static IEnumerable<CostTier> GetCostTierSeeds(IReadOnlyDictionary<string, PreventionGuide> guides)
    {
        return
        [
            CreateCostTier(guides["Measles prevention for families and schools"], CostTierType.Free, 0m, "County public health clinic", "Community vaccine event"),
            CreateCostTier(guides["Measles prevention for families and schools"], CostTierType.Insured, 25m, "Network pediatric office", "Typical insured administration fee"),
            CreateCostTier(guides["Measles prevention for families and schools"], CostTierType.OutOfPocket, 135m, "Retail urgent care", "Sample self-pay estimate"),
            CreateCostTier(guides["Seasonal flu prevention and low-cost care options"], CostTierType.Free, 0m, "Workplace wellness pop-up", "Limited seasonal campaign"),
            CreateCostTier(guides["Seasonal flu prevention and low-cost care options"], CostTierType.Insured, 15m, "Primary care office", "Sample co-pay"),
            CreateCostTier(guides["Seasonal flu prevention and low-cost care options"], CostTierType.OutOfPocket, 49m, "Grocery pharmacy", "Sample cash-pay vaccine rate"),
            CreateCostTier(guides["Norovirus cleanup and food safety reminders"], CostTierType.Free, 0m, "Public health handout", "Digital guidance only"),
            CreateCostTier(guides["Norovirus cleanup and food safety reminders"], CostTierType.Promotional, 12m, "Bulk cleaning supply coupon", "Illustrative promotion"),
            CreateCostTier(guides["RSV support guidance for caregivers"], CostTierType.Insured, 30m, "Pediatric telehealth", "Sample telehealth co-pay"),
            CreateCostTier(guides["RSV support guidance for caregivers"], CostTierType.OutOfPocket, 95m, "After-hours clinic", "Sample self-pay respiratory consult")
        ];
    }

    private static IEnumerable<LocalResource> GetResourceSeeds(IReadOnlyDictionary<string, Region> regions)
    {
        return
        [
            CreateResource(regions["Travis County"], "Austin Community Immunization Clinic", ResourceType.Clinic, "1200 Springdale Rd, Austin, TX", "512-555-0101", "https://example.org/austin-clinic", 30.2747, -97.7002, Hours(("mon", "8:00-17:00"), ("tue", "8:00-17:00"), ("wed", "8:00-17:00")), Services("flu-vaccine", "mmr-vaccine", "rapid-test")),
            CreateResource(regions["Cook County"], "South Loop Family Pharmacy", ResourceType.Pharmacy, "840 W Harrison St, Chicago, IL", "312-555-0110", "https://example.org/south-loop-pharmacy", 41.8746, -87.6481, Hours(("mon", "9:00-19:00"), ("sat", "10:00-16:00")), Services("flu-vaccine", "covid-vaccine")),
            CreateResource(regions["Los Angeles County"], "LA Metro Vaccination Hub", ResourceType.VaccinationSite, "410 S Main St, Los Angeles, CA", "213-555-0133", "https://example.org/la-vax", 34.0487, -118.2475, Hours(("mon", "8:00-18:00"), ("thu", "8:00-18:00"), ("sun", "9:00-13:00")), Services("covid-vaccine", "mmr-vaccine", "flu-vaccine")),
            CreateResource(regions["King County"], "Puget Sound Neighborhood Clinic", ResourceType.Clinic, "1550 3rd Ave, Seattle, WA", "206-555-0142", "https://example.org/puget-clinic", 47.6094, -122.3370, Hours(("mon", "8:30-17:30"), ("fri", "8:30-17:30")), Services("telehealth", "rapid-test", "pediatric-consult")),
            CreateResource(regions["Miami-Dade County"], "Little Havana Care Point", ResourceType.Clinic, "900 SW 8th St, Miami, FL", "305-555-0158", "https://example.org/little-havana-care", 25.7652, -80.2105, Hours(("mon", "8:00-18:00"), ("sat", "9:00-14:00")), Services("rsv-screening", "flu-vaccine", "family-care")),
            CreateResource(regions["Atlanta Metro"], "Peachtree Respiratory Pharmacy", ResourceType.Pharmacy, "550 Peachtree St NE, Atlanta, GA", "404-555-0164", "https://example.org/peachtree-pharmacy", 33.7687, -84.3867, Hours(("mon", "9:00-20:00"), ("sun", "11:00-15:00")), Services("rapid-test", "flu-vaccine", "pertussis-information"))
        ];
    }

    private static HealthAlert CreateAlert(Region region, string title, string disease, AlertSeverity severity, int caseCount, DateTime sourceDate)
    {
        return new HealthAlert
        {
            RegionId = region.Id,
            Title = title,
            Disease = disease,
            Summary = $"Sample alert for {region.Name}. This record is fake and for development use only.",
            Severity = severity,
            CaseCount = caseCount,
            SourceAttribution = SampleSourceAttribution,
            SourceDate = sourceDate,
            Status = AlertStatus.Published,
            CreatedAt = sourceDate,
            UpdatedAt = sourceDate
        };
    }

    private static DiseaseTrend CreateTrend(HealthAlert alert, DateTime date, int caseCount)
    {
        return new DiseaseTrend
        {
            AlertId = alert.Id,
            Date = date,
            CaseCount = caseCount,
            Source = SampleSourceAttribution,
            SourceDate = date,
            Notes = "Synthetic trend line for local development."
        };
    }

    private static PreventionGuide CreateGuide(Region region, string title, string disease, string content)
    {
        return new PreventionGuide
        {
            RegionId = region.Id,
            Title = title,
            Disease = disease,
            Content = content,
            CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static CostTier CreateCostTier(PreventionGuide guide, CostTierType type, decimal price, string provider, string? notes)
    {
        return new CostTier
        {
            GuideId = guide.Id,
            Type = type,
            Price = price,
            Provider = provider,
            Notes = notes
        };
    }

    private static LocalResource CreateResource(
        Region region,
        string name,
        ResourceType type,
        string address,
        string phone,
        string website,
        double latitude,
        double longitude,
        string hoursJson,
        string servicesJson)
    {
        return new LocalResource
        {
            RegionId = region.Id,
            Name = name,
            Type = type,
            Address = address,
            Phone = phone,
            Website = website,
            Latitude = latitude,
            Longitude = longitude,
            HoursJson = hoursJson,
            ServicesJson = servicesJson
        };
    }

    private static string Hours(params (string Day, string Range)[] values)
    {
        return "{" + string.Join(",", values.Select(value => $"\"{value.Day}\":\"{value.Range}\"")) + "}";
    }

    private static string Services(params string[] values)
    {
        return "[" + string.Join(",", values.Select(value => $"\"{value}\"")) + "]";
    }

    private static IEnumerable<(string Code, string Name, double Latitude, double Longitude)> GetStates()
    {
        return
        [
            ("AL", "Alabama", 32.806671, -86.79113),
            ("AK", "Alaska", 61.370716, -152.404419),
            ("AZ", "Arizona", 33.729759, -111.431221),
            ("AR", "Arkansas", 34.969704, -92.373123),
            ("CA", "California", 36.116203, -119.681564),
            ("CO", "Colorado", 39.059811, -105.311104),
            ("CT", "Connecticut", 41.597782, -72.755371),
            ("DE", "Delaware", 39.318523, -75.507141),
            ("FL", "Florida", 27.766279, -81.686783),
            ("GA", "Georgia", 33.040619, -83.643074),
            ("HI", "Hawaii", 21.094318, -157.498337),
            ("ID", "Idaho", 44.240459, -114.478828),
            ("IL", "Illinois", 40.349457, -88.986137),
            ("IN", "Indiana", 39.849426, -86.258278),
            ("IA", "Iowa", 42.011539, -93.210526),
            ("KS", "Kansas", 38.5266, -96.726486),
            ("KY", "Kentucky", 37.66814, -84.670067),
            ("LA", "Louisiana", 31.169546, -91.867805),
            ("ME", "Maine", 44.693947, -69.381927),
            ("MD", "Maryland", 39.063946, -76.802101),
            ("MA", "Massachusetts", 42.230171, -71.530106),
            ("MI", "Michigan", 43.326618, -84.536095),
            ("MN", "Minnesota", 45.694454, -93.900192),
            ("MS", "Mississippi", 32.741646, -89.678696),
            ("MO", "Missouri", 38.456085, -92.288368),
            ("MT", "Montana", 46.921925, -110.454353),
            ("NE", "Nebraska", 41.12537, -98.268082),
            ("NV", "Nevada", 38.313515, -117.055374),
            ("NH", "New Hampshire", 43.452492, -71.563896),
            ("NJ", "New Jersey", 40.298904, -74.521011),
            ("NM", "New Mexico", 34.840515, -106.248482),
            ("NY", "New York", 42.165726, -74.948051),
            ("NC", "North Carolina", 35.630066, -79.806419),
            ("ND", "North Dakota", 47.528912, -99.784012),
            ("OH", "Ohio", 40.388783, -82.764915),
            ("OK", "Oklahoma", 35.565342, -96.928917),
            ("OR", "Oregon", 44.572021, -122.070938),
            ("PA", "Pennsylvania", 40.590752, -77.209755),
            ("RI", "Rhode Island", 41.680893, -71.51178),
            ("SC", "South Carolina", 33.856892, -80.945007),
            ("SD", "South Dakota", 44.299782, -99.438828),
            ("TN", "Tennessee", 35.747845, -86.692345),
            ("TX", "Texas", 31.054487, -97.563461),
            ("UT", "Utah", 40.150032, -111.862434),
            ("VT", "Vermont", 44.045876, -72.710686),
            ("VA", "Virginia", 37.769337, -78.169968),
            ("WA", "Washington", 47.400902, -121.490494),
            ("WV", "West Virginia", 38.491226, -80.954453),
            ("WI", "Wisconsin", 44.268543, -89.616508),
            ("WY", "Wyoming", 42.755966, -107.30249),
            ("DC", "District of Columbia", 38.904149, -77.017094),
            ("PR", "Puerto Rico", 18.220833, -66.590149),
            ("GU", "Guam", 13.444304, 144.793731),
            ("VI", "U.S. Virgin Islands", 18.335765, -64.896335),
            ("AS", "American Samoa", -14.270972, -170.132217),
            ("MP", "Northern Mariana Islands", 15.0979, 145.6739)
        ];
    }

    private static IReadOnlyList<CountySeedEntry> LoadCountyDataFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("us-counties.json", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Deserialize<List<CountySeedEntry>>(stream, options) ?? [];
    }

    private sealed class CountySeedEntry
    {
        public string Fips { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    private static IEnumerable<(string Name, string StateCode, double Latitude, double Longitude)> GetMetros()
    {
        return
        [
            ("Austin Metro", "TX", 30.2672, -97.7431),
            ("Chicago Metro", "IL", 41.8781, -87.6298),
            ("Los Angeles Metro", "CA", 34.0522, -118.2437),
            ("Seattle Metro", "WA", 47.6062, -122.3321),
            ("Atlanta Metro", "GA", 33.7490, -84.3880)
        ];
    }
}
