using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Scholarship_Management_System.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsorApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added before the drop: the backfill below reads IsApproved.
            migrationBuilder.AddColumn<string>(
                name: "SponsorStatus",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecidedBy",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            // Sponsors only — the column stays null for every other role.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET SponsorStatus = CASE WHEN IsApproved = 1 THEN 'Approved' ELSE 'Pending' END
                WHERE Role = 'sponsor';");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Non-sponsors count as approved; sponsors only when their status says so.
            migrationBuilder.Sql(@"
                UPDATE Users
                SET IsApproved = CASE
                    WHEN Role <> 'sponsor' THEN 1
                    WHEN SponsorStatus = 'Approved' THEN 1
                    ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "SponsorStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DecidedBy",
                table: "Users");
        }
    }
}
