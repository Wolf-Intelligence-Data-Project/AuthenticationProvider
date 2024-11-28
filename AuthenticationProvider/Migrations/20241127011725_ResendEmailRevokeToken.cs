using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationProvider.Migrations
{
    /// <inheritdoc />
    public partial class ResendEmailRevokeToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastEmailVerificationToken",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastEmailVerificationToken",
                table: "Companies");
        }
    }
}
