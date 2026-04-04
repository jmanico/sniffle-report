using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Models.Configuration;
using SniffleReport.Api.Services;
using SniffleReport.Api.Services.Ingestion;
using SniffleReport.Api.Services.Ingestion.Connectors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["ConnectionStrings__Default"]
        ?? "Host=localhost;Database=snifflereport;Username=sniffle;Password=localdev";

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
    });
});
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<NewsService>();
builder.Services.AddScoped<PreventionService>();
builder.Services.AddScoped<RegionService>();
builder.Services.AddScoped<ResourceService>();
builder.Services.AddScoped<TrendService>();

// Feed ingestion configuration
builder.Services.Configure<FeedIngestionOptions>(
    builder.Configuration.GetSection(FeedIngestionOptions.SectionName));

// Named HttpClients for external feeds
builder.Services.AddHttpClient("CdcSocrata", client =>
{
    client.BaseAddress = new Uri("https://data.cdc.gov/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SniffleReport/1.0");
});
builder.Services.AddHttpClient("CdcRss", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SniffleReport/1.0");
});

// Feed connectors and ingestion services
builder.Services.AddScoped<IFeedConnector, CdcSocrataConnector>();
builder.Services.AddScoped<IFeedConnector, CdcRssConnector>();
builder.Services.AddScoped<RegionMappingService>();
builder.Services.AddScoped<AlertThresholdService>();
builder.Services.AddScoped<IngestionService>();

// Background polling service
builder.Services.AddHostedService<FeedPollingBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);
}

app.UseExceptionHandler();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    Name = "Sniffle Report API",
    Status = "Initialized"
}));

app.Run();

public partial class Program;
