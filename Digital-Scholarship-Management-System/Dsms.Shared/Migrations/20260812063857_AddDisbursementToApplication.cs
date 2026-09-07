using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Scholarship_Management_System.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDisbursementToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DisbursedAmount",
                table: "Applications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisbursedAt",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisbursedByUserId",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisbursementStatus",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_DisbursedByUserId",
                table: "Applications",
                column: "DisbursedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Users_DisbursedByUserId",
                table: "Applications",
                column: "DisbursedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Users_DisbursedByUserId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_DisbursedByUserId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DisbursedAmount",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DisbursedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DisbursedByUserId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DisbursementStatus",
                table: "Applications");
        }
    }
}
