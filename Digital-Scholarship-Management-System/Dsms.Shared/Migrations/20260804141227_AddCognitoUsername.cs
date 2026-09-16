using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Scholarship_Management_System.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCognitoUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CognitoUsername",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            // Backfill accounts created before this column existed; registration populates it
            // from here on. Matched on CognitoSub, so this is a no-op where those rows are absent.
            migrationBuilder.Sql(@"
UPDATE Users SET CognitoUsername = 'ridwan'      WHERE CognitoSub = 'f9ee3428-1001-70d6-8ca2-9848ce997b04';
UPDATE Users SET CognitoUsername = 'testadmin'   WHERE CognitoSub = '09ce1478-5091-7056-60a2-f0af71e3bd5c';
UPDATE Users SET CognitoUsername = 'teststudent' WHERE CognitoSub = '590e5428-2031-705d-a40c-db88c96d5f73';
UPDATE Users SET CognitoUsername = 'testofficer' WHERE CognitoSub = 'e9be1468-8051-705f-e8db-49bca8baa311';
UPDATE Users SET CognitoUsername = 'testsponsor' WHERE CognitoSub = '59ae2488-10f1-7018-df99-e14d3b1ce060';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CognitoUsername",
                table: "Users");
        }
    }
}
