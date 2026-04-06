using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SniffleReport.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260405230000_AddPhaseOneAccessAndWaterData")]
public partial class AddPhaseOneAccessAndWaterData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AccessSignalsJson",
            table: "RegionSnapshots",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "EnvironmentalSignalsJson",
            table: "RegionSnapshots",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.CreateTable(
            name: "ShortageAreaDesignations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalSourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AreaName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Discipline = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                DesignationType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                PopulationGroup = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                HpsaScore = table.Column<int>(type: "integer", nullable: true),
                PopulationToProviderRatio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                SourceUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShortageAreaDesignations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShortageAreaDesignations_Regions_RegionId",
                    column: x => x.RegionId,
                    principalTable: "Regions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WaterSystems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalSourceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                SystemType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                CountyServed = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                PopulationServed = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WaterSystems", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WaterSystemViolations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WaterSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                ExternalSourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ViolationCategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                RuleName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                ContaminantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                IdentifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                SourceUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WaterSystemViolations", x => x.Id);
                table.ForeignKey(
                    name: "FK_WaterSystemViolations_Regions_RegionId",
                    column: x => x.RegionId,
                    principalTable: "Regions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_WaterSystemViolations_WaterSystems_WaterSystemId",
                    column: x => x.WaterSystemId,
                    principalTable: "WaterSystems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ShortageAreaDesignation_ExternalSourceId",
            table: "ShortageAreaDesignations",
            column: "ExternalSourceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ShortageAreaDesignation_RegionId_Discipline",
            table: "ShortageAreaDesignations",
            columns: new[] { "RegionId", "Discipline" });

        migrationBuilder.CreateIndex(
            name: "IX_WaterSystem_ExternalSourceId",
            table: "WaterSystems",
            column: "ExternalSourceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WaterSystemViolation_ExternalSourceId",
            table: "WaterSystemViolations",
            column: "ExternalSourceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WaterSystemViolation_RegionId_IsOpen",
            table: "WaterSystemViolations",
            columns: new[] { "RegionId", "IsOpen" });

        migrationBuilder.CreateIndex(
            name: "IX_WaterSystemViolations_WaterSystemId",
            table: "WaterSystemViolations",
            column: "WaterSystemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ShortageAreaDesignations");

        migrationBuilder.DropTable(
            name: "WaterSystemViolations");

        migrationBuilder.DropTable(
            name: "WaterSystems");

        migrationBuilder.DropColumn(
            name: "AccessSignalsJson",
            table: "RegionSnapshots");

        migrationBuilder.DropColumn(
            name: "EnvironmentalSignalsJson",
            table: "RegionSnapshots");
    }
}
