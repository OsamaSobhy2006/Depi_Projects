
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusOne.Infrastructure.Persistence.Migrations
{
    public partial class AddBookDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update Books table
            migrationBuilder.AlterColumn<string>(
                name: "Cover",
                table: "Books",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Books",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "Books",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            // Update existing BorrowRequests table
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "BorrowRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCheckoutSessionId",
                table: "BorrowRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "BorrowRequests");

            migrationBuilder.DropColumn(
                name: "StripeCheckoutSessionId",
                table: "BorrowRequests");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "Books");

            migrationBuilder.AlterColumn<string>(
                name: "Cover",
                table: "Books",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
