using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SniffleReport.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedIngestionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IngestedRecordId",
                table: "NewsItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IngestedRecordId",
                table: "HealthAlerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IngestedRecordId",
                table: "DiseaseTrends",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeedSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SoqlQuery = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PollingInterval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSyncError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordsFetched = table.Column<int>(type: "integer", nullable: false),
                    RecordsCreated = table.Column<int>(type: "integer", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RecordsSkippedDuplicate = table.Column<int>(type: "integer", nullable: false),
                    RecordsSkippedUnmappable = table.Column<int>(type: "integer", nullable: false),
                    AlertsPromoted = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedSyncLogs_FeedSources_FeedSourceId",
                        column: x => x.FeedSourceId,
                        principalTable: "FeedSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestedRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSourceId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetEntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstIngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastIngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IngestCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngestedRecords_FeedSources_FeedSourceId",
                        column: x => x.FeedSourceId,
                        principalTable: "FeedSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsItems_IngestedRecordId",
                table: "NewsItems",
                column: "IngestedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_IngestedRecordId",
                table: "HealthAlerts",
                column: "IngestedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTrends_IngestedRecordId",
                table: "DiseaseTrends",
                column: "IngestedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedSource_IsEnabled_LastSyncStartedAt",
                table: "FeedSources",
                columns: new[] { "IsEnabled", "LastSyncStartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedSyncLog_FeedSourceId_StartedAt",
                table: "FeedSyncLogs",
                columns: new[] { "FeedSourceId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestedRecord_FeedSourceId_ExternalSourceId",
                table: "IngestedRecords",
                columns: new[] { "FeedSourceId", "ExternalSourceId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DiseaseTrends_IngestedRecords_IngestedRecordId",
                table: "DiseaseTrends",
                column: "IngestedRecordId",
                principalTable: "IngestedRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HealthAlerts_IngestedRecords_IngestedRecordId",
                table: "HealthAlerts",
                column: "IngestedRecordId",
                principalTable: "IngestedRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsItems_IngestedRecords_IngestedRecordId",
                table: "NewsItems",
                column: "IngestedRecordId",
                principalTable: "IngestedRecords",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiseaseTrends_IngestedRecords_IngestedRecordId",
                table: "DiseaseTrends");

            migrationBuilder.DropForeignKey(
                name: "FK_HealthAlerts_IngestedRecords_IngestedRecordId",
                table: "HealthAlerts");

            migrationBuilder.DropForeignKey(
                name: "FK_NewsItems_IngestedRecords_IngestedRecordId",
                table: "NewsItems");

            migrationBuilder.DropTable(
                name: "FeedSyncLogs");

            migrationBuilder.DropTable(
                name: "IngestedRecords");

            migrationBuilder.DropTable(
                name: "FeedSources");

            migrationBuilder.DropIndex(
                name: "IX_NewsItems_IngestedRecordId",
                table: "NewsItems");

            migrationBuilder.DropIndex(
                name: "IX_HealthAlerts_IngestedRecordId",
                table: "HealthAlerts");

            migrationBuilder.DropIndex(
                name: "IX_DiseaseTrends_IngestedRecordId",
                table: "DiseaseTrends");

            migrationBuilder.DropColumn(
                name: "IngestedRecordId",
                table: "NewsItems");

            migrationBuilder.DropColumn(
                name: "IngestedRecordId",
                table: "HealthAlerts");

            migrationBuilder.DropColumn(
                name: "IngestedRecordId",
                table: "DiseaseTrends");
        }
    }
}
