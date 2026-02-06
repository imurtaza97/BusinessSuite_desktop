using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiWarehouseStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ToWarehouseID",
                table: "StockTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseID",
                table: "StockTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessId = table.Column<int>(type: "INTEGER", nullable: false),
                    WarehouseName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IsMainWarehouse = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseID);
                    table.ForeignKey(
                        name: "FK_Warehouses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    StockID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    WarehouseID = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.StockID);
                    table.ForeignKey(
                        name: "FK_Stocks_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Stocks_Warehouses_WarehouseID",
                        column: x => x.WarehouseID,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ToWarehouseID",
                table: "StockTransactions",
                column: "ToWarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_WarehouseID",
                table: "StockTransactions",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductID_WarehouseID",
                table: "Stocks",
                columns: new[] { "ProductID", "WarehouseID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_WarehouseID",
                table: "Stocks",
                column: "WarehouseID");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId",
                table: "Warehouses",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_Warehouses_ToWarehouseID",
                table: "StockTransactions",
                column: "ToWarehouseID",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_Warehouses_WarehouseID",
                table: "StockTransactions",
                column: "WarehouseID",
                principalTable: "Warehouses",
                principalColumn: "WarehouseID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_Warehouses_ToWarehouseID",
                table: "StockTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_Warehouses_WarehouseID",
                table: "StockTransactions");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_ToWarehouseID",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_WarehouseID",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "ToWarehouseID",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "WarehouseID",
                table: "StockTransactions");
        }
    }
}
