using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHandsVolunteerPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase_4_1_FixVolunteerSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateId",
                table: "Applications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CertificateId",
                table: "Applications",
                column: "CertificateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Certificates_CertificateId",
                table: "Applications",
                column: "CertificateId",
                principalTable: "Certificates",
                principalColumn: "CertificateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Certificates_CertificateId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CertificateId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CertificateId",
                table: "Applications");
        }
    }
}
