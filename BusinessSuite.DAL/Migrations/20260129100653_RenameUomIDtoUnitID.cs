using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameUomIDtoUnitID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UomID",
                table: "UnitsOfMeasure",
                newName: "UnitID");

            migrationBuilder.RenameColumn(
                name: "UOM",
                table: "Products",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "UOM",
                table: "InvoiceItems",
                newName: "Unit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitID",
                table: "UnitsOfMeasure",
                newName: "UomID");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "Products",
                newName: "UOM");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "InvoiceItems",
                newName: "UOM");
        }
    }
}
