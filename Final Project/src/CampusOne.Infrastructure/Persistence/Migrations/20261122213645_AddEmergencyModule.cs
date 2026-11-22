using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusOne.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedResponderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AiSuggestedCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AiStructuredSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    AccuracyMeters = table.Column<double>(type: "float", nullable: true),
                    LocationLabel = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondingAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvestigatingAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyIncidents_AspNetUsers_AssignedResponderId",
                        column: x => x.AssignedResponderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmergencyIncidents_AspNetUsers_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmergencyUpdateLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyUpdateLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyUpdateLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmergencyUpdateLogs_EmergencyIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "EmergencyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyIncidents_AssignedResponderId",
                table: "EmergencyIncidents",
                column: "AssignedResponderId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyIncidents_ReporterUserId",
                table: "EmergencyIncidents",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyIncidents_Status_CreatedAt",
                table: "EmergencyIncidents",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyUpdateLogs_ActorUserId",
                table: "EmergencyUpdateLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyUpdateLogs_IncidentId_CreatedAt",
                table: "EmergencyUpdateLogs",
                columns: new[] { "IncidentId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyUpdateLogs");

            migrationBuilder.DropTable(
                name: "EmergencyIncidents");
        }
    }
}
