using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    QuotationID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessID = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerID = table.Column<int>(type: "INTEGER", nullable: false),
                    QuotationNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    QuotationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAutoRoundOff = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PaymentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsItemLevelDiscount = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PaymentTerms = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TermsAndConditions = table.Column<string>(type: "TEXT", nullable: true),
                    PlaceOfSupply = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ReverseCharge = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoundOff = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TotalCGST = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TotalSGST = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TotalIGST = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ShippingCharges = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PostedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    DeletionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.QuotationID);
                    table.ForeignKey(
                        name: "FK_Quotations_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotations_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quotations_Users_CancelledByUserID",
                        column: x => x.CancelledByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Quotations_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Quotations_Users_DeletedByUserID",
                        column: x => x.DeletedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Quotations_Users_ModifiedByUserID",
                        column: x => x.ModifiedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Quotations_Users_PostedByUserID",
                        column: x => x.PostedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "QuotationItems",
                columns: table => new
                {
                    QuotationItemID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuotationID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    HSNCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CGST_Rate = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    CGST_Amount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SGST_Rate = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    SGST_Amount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IGST_Rate = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    IGST_Amount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationItems", x => x.QuotationItemID);
                    table.ForeignKey(
                        name: "FK_QuotationItems_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuotationItems_Quotations_QuotationID",
                        column: x => x.QuotationID,
                        principalTable: "Quotations",
                        principalColumn: "QuotationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_ProductID",
                table: "QuotationItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationItems_QuotationID",
                table: "QuotationItems",
                column: "QuotationID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_BusinessID",
                table: "Quotations",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CancelledByUserID",
                table: "Quotations",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CreatedByUserID",
                table: "Quotations",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CustomerID",
                table: "Quotations",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_DeletedByUserID",
                table: "Quotations",
                column: "DeletedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_ModifiedByUserID",
                table: "Quotations",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_PostedByUserID",
                table: "Quotations",
                column: "PostedByUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuotationItems");

            migrationBuilder.DropTable(
                name: "Quotations");
        }
    }
}
