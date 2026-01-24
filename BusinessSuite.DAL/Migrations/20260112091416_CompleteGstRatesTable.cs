using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class CompleteGstRatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GstRates",
                columns: table => new
                {
                    RateID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Percentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GstRates", x => x.RateID);
                });

            migrationBuilder.InsertData(
                table: "GstRates",
                columns: new[] { "RateID", "Description", "Percentage" },
                values: new object[,]
                {
                    { 1, "Exempt/Nil Rated", 0m },
                    { 2, "Essential Items", 5m },
                    { 3, "Standard Rate (Lower)", 12m },
                    { 4, "Standard Rate", 18m },
                    { 5, "Luxury/Demerit Items", 28m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GstRates");
        }
    }
}
