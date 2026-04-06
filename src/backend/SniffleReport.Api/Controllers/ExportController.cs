using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SniffleReport.Api.Services.Snapshots;
using SniffleReport.Api.StaticExport;

namespace SniffleReport.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/export")]
public sealed class ExportController(
    StaticSiteExporter exporter,
    RegionSnapshotBuilder snapshotBuilder) : ControllerBase
{
    [HttpPost("static")]
    public async Task<ActionResult<ExportResult>> ExportStatic(CancellationToken cancellationToken)
    {
        await snapshotBuilder.RebuildAllAsync(cancellationToken);

        var outputDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "static-export", "data"));

        var result = await exporter.ExportAsync(outputDir, cancellationToken);

        return Ok(result);
    }
}
