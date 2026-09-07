using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Scholarship_Management_System.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDivorceColumnNGuardianPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuardianPhone",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParentsDivorced",
                table: "StudentProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuardianPhone",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "ParentsDivorced",
                table: "StudentProfiles");
        }
    }
}
