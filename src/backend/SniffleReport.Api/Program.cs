using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;
using SniffleReport.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["ConnectionStrings__Default"]
        ?? "Host=localhost;Database=snifflereport;Username=sniffle;Password=localdev";

    options.UseNpgsql(connectionString);
});
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<RegionService>();
builder.Services.AddScoped<ResourceService>();

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
