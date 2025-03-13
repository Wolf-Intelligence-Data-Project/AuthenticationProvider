using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationProvider.Migrations
{
    /// <inheritdoc />
    public partial class fixingentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Users_AddressId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ResetPasswordTokens_Users_UserId",
                table: "ResetPasswordTokens");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ResetPasswordTokens",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResetPasswordTokens_UserId",
                table: "ResetPasswordTokens",
                newName: "IX_ResetPasswordTokens_UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId",
                table: "Addresses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Users_UserId",
                table: "Addresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResetPasswordTokens_Users_UserId",
                table: "ResetPasswordTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Users_UserId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ResetPasswordTokens_Users_UserId",
                table: "ResetPasswordTokens");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_UserId",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ResetPasswordTokens",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ResetPasswordTokens_UserId",
                table: "ResetPasswordTokens",
                newName: "IX_ResetPasswordTokens_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Users_AddressId",
                table: "Addresses",
                column: "AddressId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResetPasswordTokens_Users_UserId",
                table: "ResetPasswordTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
