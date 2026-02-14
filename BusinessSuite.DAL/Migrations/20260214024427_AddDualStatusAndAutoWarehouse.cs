using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDualStatusAndAutoWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "PurchaseOrders",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Invoices",
                newName: "PaymentStatus");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "Invoices",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "PurchaseOrders",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "Invoices",
                newName: "Status");
        }
    }
}
