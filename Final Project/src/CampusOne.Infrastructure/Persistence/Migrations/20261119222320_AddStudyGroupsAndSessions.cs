using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusOne.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyGroupsAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyGroups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudyGroupMembers",
                columns: table => new
                {
                    StudyGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGroupMembers", x => new { x.StudyGroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_StudyGroupMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudyGroupMembers_StudyGroups_StudyGroupId",
                        column: x => x.StudyGroupId,
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsLive = table.Column<bool>(type: "bit", nullable: false),
                    StartedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySessions_AspNetUsers_StartedByUserId",
                        column: x => x.StartedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudySessions_StudyGroups_StudyGroupId",
                        column: x => x.StudyGroupId,
                        principalTable: "StudyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroupMembers_UserId",
                table: "StudyGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_CourseCode",
                table: "StudyGroups",
                column: "CourseCode");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_CreatedAt",
                table: "StudyGroups",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGroups_CreatedByUserId",
                table: "StudyGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_RoomCode",
                table: "StudySessions",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_StartedByUserId",
                table: "StudySessions",
                column: "StartedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_StudyGroupId_IsLive",
                table: "StudySessions",
                columns: new[] { "StudyGroupId", "IsLive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyGroupMembers");

            migrationBuilder.DropTable(
                name: "StudySessions");

            migrationBuilder.DropTable(
                name: "StudyGroups");
        }
    }
}
