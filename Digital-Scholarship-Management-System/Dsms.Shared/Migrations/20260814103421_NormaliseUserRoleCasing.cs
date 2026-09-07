using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Scholarship_Management_System.API.Migrations
{
    /// <inheritdoc />
    public partial class NormaliseUserRoleCasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SyncAdminSchema backfills PascalCase; UserRole members are lowercase.
            // Unconditional: the default collation is case-insensitive, so a
            // "WHERE Role <> LOWER(Role)" guard would never match.
            migrationBuilder.Sql("UPDATE Users SET Role = LOWER(Role);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No inverse — PascalCase was never valid for the current enum.
        }
    }
}
