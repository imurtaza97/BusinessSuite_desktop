using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForIndianGstCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UOM",
                table: "Products",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupply",
                table: "Invoices",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReverseCharge",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundOff",
                table: "Invoices",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCGST",
                table: "Invoices",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIGST",
                table: "Invoices",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSGST",
                table: "Invoices",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CGST_Amount",
                table: "InvoiceItems",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CGST_Rate",
                table: "InvoiceItems",
                type: "decimal(5, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HSNCode",
                table: "InvoiceItems",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IGST_Amount",
                table: "InvoiceItems",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IGST_Rate",
                table: "InvoiceItems",
                type: "decimal(5, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SGST_Amount",
                table: "InvoiceItems",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SGST_Rate",
                table: "InvoiceItems",
                type: "decimal(5, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UOM",
                table: "InvoiceItems",
                type: "TEXT",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UOM",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupply",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReverseCharge",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RoundOff",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalCGST",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalIGST",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalSGST",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CGST_Amount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "CGST_Rate",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "HSNCode",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IGST_Amount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IGST_Rate",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "SGST_Amount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "SGST_Rate",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "UOM",
                table: "InvoiceItems");
        }
    }
}
