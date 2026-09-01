using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHandsVolunteerPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase_4_0_FixOrganizationSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationType",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationType",
                table: "Organizations");
        }
    }
}
