using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUnitToNosInSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 5,
                column: "Name",
                value: "nos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 5,
                column: "Name",
                value: "NOS");
        }
    }
}
