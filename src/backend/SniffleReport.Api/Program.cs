using Microsoft.EntityFrameworkCore;
using SniffleReport.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? builder.Configuration["ConnectionStrings__Default"]
        ?? "Host=localhost;Database=snifflereport;Username=sniffle;Password=localdev";

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);
}

app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new
{
    Name = "Sniffle Report API",
    Status = "Initialized"
}));

app.Run();
