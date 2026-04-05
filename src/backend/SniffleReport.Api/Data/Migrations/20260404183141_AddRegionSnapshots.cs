using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SniffleReport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAlertCount = table.Column<int>(type: "integer", nullable: false),
                    TopAlertsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TrendHighlightsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResourceCountsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PreventionHighlightsJson = table.Column<string>(type: "jsonb", nullable: false),
                    NewsHighlightsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionSnapshots_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnapshot_ComputedAt",
                table: "RegionSnapshots",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSnapshot_RegionId",
                table: "RegionSnapshots",
                column: "RegionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegionSnapshots");
        }
    }
}
