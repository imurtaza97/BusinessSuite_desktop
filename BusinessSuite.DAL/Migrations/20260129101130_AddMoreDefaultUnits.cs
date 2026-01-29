using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreDefaultUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "UnitsOfMeasure",
                columns: new[] { "UnitID", "BusinessId", "Description", "Name" },
                values: new object[,]
                {
                    { 6, 0, "Litres", "LTR" },
                    { 7, 0, "Grams", "GMS" },
                    { 8, 0, "Millilitres", "ML" },
                    { 9, 0, "Dozen", "DOZ" },
                    { 10, 0, "Pair", "PAIR" },
                    { 11, 0, "Set", "SET" },
                    { 12, 0, "Packet", "PKT" },
                    { 13, 0, "Tin", "TIN" },
                    { 14, 0, "Bag", "BAG" },
                    { 15, 0, "Bottle", "BTL" },
                    { 16, 0, "Jar", "JAR" },
                    { 17, 0, "Can", "CAN" },
                    { 18, 0, "Tube", "TUBE" },
                    { 19, 0, "Roll", "ROLL" },
                    { 20, 0, "Sheet", "SHEET" },
                    { 21, 0, "Square Feet", "SQFT" },
                    { 22, 0, "Square Meter", "SQM" },
                    { 23, 0, "Cubic Feet", "CFT" },
                    { 24, 0, "Cubic Meter", "CUM" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "UnitsOfMeasure",
                keyColumn: "UnitID",
                keyValue: 24);
        }
    }
}
