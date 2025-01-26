using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationProvider.Migrations
{
    /// <inheritdoc />
    public partial class deletingprimaryaddressidfromcompanyentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Addresses_PrimaryAddressId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_PrimaryAddressId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PrimaryAddressId",
                table: "Companies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryAddressId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PrimaryAddressId",
                table: "Companies",
                column: "PrimaryAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Addresses_PrimaryAddressId",
                table: "Companies",
                column: "PrimaryAddressId",
                principalTable: "Addresses",
                principalColumn: "Id");
        }
    }
}
