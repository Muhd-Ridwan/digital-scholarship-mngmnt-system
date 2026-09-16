using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Scholarship_Management_System.API.Migrations
{
    /// <inheritdoc />
    public partial class ScholarshipFieldsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "University",
                table: "Scholarships",
                newName: "StudyLocation");

            migrationBuilder.Sql("UPDATE Scholarships SET StudyLocation = '' WHERE StudyLocation IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "StudyLocation",
                table: "Scholarships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundType",
                table: "Scholarships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrganisationType",
                table: "Scholarships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundType",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "OrganisationType",
                table: "Scholarships");

            migrationBuilder.AlterColumn<string>(
                name: "StudyLocation",
                table: "Scholarships",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.RenameColumn(
                name: "StudyLocation",
                table: "Scholarships",
                newName: "University");
        }
    }
}
