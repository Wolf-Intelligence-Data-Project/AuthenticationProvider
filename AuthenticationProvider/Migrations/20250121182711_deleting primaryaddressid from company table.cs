using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationProvider.Migrations
{
    /// <inheritdoc />
    public partial class deletingprimaryaddressidfromcompanytable : Migration
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

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                table: "Addresses",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Addresses_PrimaryAddressId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_PrimaryAddressId",
                table: "Companies");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                table: "Addresses",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PrimaryAddressId",
                table: "Companies",
                column: "PrimaryAddressId",
                unique: true,
                filter: "[PrimaryAddressId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Addresses_PrimaryAddressId",
                table: "Companies",
                column: "PrimaryAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
