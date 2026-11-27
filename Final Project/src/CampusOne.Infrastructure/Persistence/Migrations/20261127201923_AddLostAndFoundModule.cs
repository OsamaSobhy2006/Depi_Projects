using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusOne.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLostAndFoundModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LostAndFoundItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostAndFoundItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LostAndFoundItems_AspNetUsers_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LostAndFoundClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LostItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoundItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimantUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SimilarityScore = table.Column<double>(type: "float", nullable: false),
                    VerificationNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostAndFoundClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LostAndFoundClaims_AspNetUsers_ClaimantUserId",
                        column: x => x.ClaimantUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LostAndFoundClaims_LostAndFoundItems_FoundItemId",
                        column: x => x.FoundItemId,
                        principalTable: "LostAndFoundItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LostAndFoundClaims_LostAndFoundItems_LostItemId",
                        column: x => x.LostItemId,
                        principalTable: "LostAndFoundItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LostAndFoundClaims_ClaimantUserId",
                table: "LostAndFoundClaims",
                column: "ClaimantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LostAndFoundClaims_FoundItemId",
                table: "LostAndFoundClaims",
                column: "FoundItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LostAndFoundClaims_LostItemId_FoundItemId",
                table: "LostAndFoundClaims",
                columns: new[] { "LostItemId", "FoundItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LostAndFoundItems_ReporterUserId",
                table: "LostAndFoundItems",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LostAndFoundItems_Type_Status_CreatedAt",
                table: "LostAndFoundItems",
                columns: new[] { "Type", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LostAndFoundClaims");

            migrationBuilder.DropTable(
                name: "LostAndFoundItems");
        }
    }
}
