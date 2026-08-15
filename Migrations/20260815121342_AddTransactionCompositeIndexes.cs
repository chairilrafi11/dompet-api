using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dompet.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_CategoryId",
                table: "Transactions",
                columns: new[] { "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_WalletId",
                table: "Transactions",
                columns: new[] { "UserId", "WalletId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_CategoryId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_WalletId",
                table: "Transactions");
        }
    }
}
