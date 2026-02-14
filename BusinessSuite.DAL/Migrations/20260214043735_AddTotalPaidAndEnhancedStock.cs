using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalPaidAndEnhancedStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaid",
                table: "PurchaseOrders",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaid",
                table: "Invoices",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPaid",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TotalPaid",
                table: "Invoices");
        }
    }
}
