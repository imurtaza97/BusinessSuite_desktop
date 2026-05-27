using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSuite.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Phase1Audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserID",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserID",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserID",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserID",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostedByUserID",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Invoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserID",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserID",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserID",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "Invoices",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedByUserID",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostedByUserID",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessID = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DocumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ChangedByUserID = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IPAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogID);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ChangedByUserID",
                        column: x => x.ChangedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillOfMaterials",
                columns: table => new
                {
                    BOM_ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessID = table.Column<int>(type: "INTEGER", nullable: false),
                    FinishedProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    RawMaterialProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WastagePercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "INTEGER", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillOfMaterials", x => x.BOM_ID);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_FinishedProductID",
                        column: x => x.FinishedProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Products_RawMaterialProductID",
                        column: x => x.RawMaterialProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillOfMaterials_Users_ModifiedByUserID",
                        column: x => x.ModifiedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    CreditNoteID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessID = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalInvoiceID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreditNoteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCGST = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalSGST = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalIGST = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    DeletionReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "INTEGER", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalizedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.CreditNoteID);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Invoices_OriginalInvoiceID",
                        column: x => x.OriginalInvoiceID,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_CancelledByUserID",
                        column: x => x.CancelledByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_DeletedByUserID",
                        column: x => x.DeletedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_FinalizedByUserID",
                        column: x => x.FinalizedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_ModifiedByUserID",
                        column: x => x.ModifiedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "DebitNotes",
                columns: table => new
                {
                    DebitNoteID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessID = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalInvoiceID = table.Column<int>(type: "INTEGER", nullable: false),
                    DebitNoteNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DebitNoteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCGST = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalSGST = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalIGST = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    DeletionReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "INTEGER", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalizedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitNotes", x => x.DebitNoteID);
                    table.ForeignKey(
                        name: "FK_DebitNotes_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebitNotes_Invoices_OriginalInvoiceID",
                        column: x => x.OriginalInvoiceID,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebitNotes_Users_CancelledByUserID",
                        column: x => x.CancelledByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_DebitNotes_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebitNotes_Users_DeletedByUserID",
                        column: x => x.DeletedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_DebitNotes_Users_FinalizedByUserID",
                        column: x => x.FinalizedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_DebitNotes_Users_ModifiedByUserID",
                        column: x => x.ModifiedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    ProductionOrderID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductionOrderNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    QuantityToMake = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedEndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    QuantityCompleted = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    ActualCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedByUserID = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "INTEGER", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.ProductionOrderID);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "BusinessID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Users_DeletedByUserID",
                        column: x => x.DeletedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Users_ModifiedByUserID",
                        column: x => x.ModifiedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteItems",
                columns: table => new
                {
                    CreditNoteItemID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreditNoteID = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalInvoiceItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    HSNCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    CGST_Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    CGST_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    SGST_Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    SGST_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    IGST_Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    IGST_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTax = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNoteItems", x => x.CreditNoteItemID);
                    table.ForeignKey(
                        name: "FK_CreditNoteItems_CreditNotes_CreditNoteID",
                        column: x => x.CreditNoteID,
                        principalTable: "CreditNotes",
                        principalColumn: "CreditNoteID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNoteItems_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebitNoteItems",
                columns: table => new
                {
                    DebitNoteItemID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DebitNoteID = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalInvoiceItemID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductID = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    HSNCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    CGST_Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    CGST_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    SGST_Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    SGST_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    IGST_Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    IGST_Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTax = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitNoteItems", x => x.DebitNoteItemID);
                    table.ForeignKey(
                        name: "FK_DebitNoteItems_DebitNotes_DebitNoteID",
                        column: x => x.DebitNoteID,
                        principalTable: "DebitNotes",
                        principalColumn: "DebitNoteID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebitNoteItems_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CancelledByUserID",
                table: "PurchaseOrders",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedByUserID",
                table: "PurchaseOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DeletedByUserID",
                table: "PurchaseOrders",
                column: "DeletedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ModifiedByUserID",
                table: "PurchaseOrders",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PostedByUserID",
                table: "PurchaseOrders",
                column: "PostedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CancelledByUserID",
                table: "Invoices",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedByUserID",
                table: "Invoices",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DeletedByUserID",
                table: "Invoices",
                column: "DeletedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ModifiedByUserID",
                table: "Invoices",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PostedByUserID",
                table: "Invoices",
                column: "PostedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_BusinessID",
                table: "AuditLogs",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ChangedByUserID",
                table: "AuditLogs",
                column: "ChangedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_BusinessID",
                table: "BillOfMaterials",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_CreatedByUserID",
                table: "BillOfMaterials",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_FinishedProductID",
                table: "BillOfMaterials",
                column: "FinishedProductID");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_ModifiedByUserID",
                table: "BillOfMaterials",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterials_RawMaterialProductID",
                table: "BillOfMaterials",
                column: "RawMaterialProductID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteItems_CreditNoteID",
                table: "CreditNoteItems",
                column: "CreditNoteID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteItems_ProductID",
                table: "CreditNoteItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_BusinessID",
                table: "CreditNotes",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CancelledByUserID",
                table: "CreditNotes",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreatedByUserID",
                table: "CreditNotes",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_DeletedByUserID",
                table: "CreditNotes",
                column: "DeletedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_FinalizedByUserID",
                table: "CreditNotes",
                column: "FinalizedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_ModifiedByUserID",
                table: "CreditNotes",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_OriginalInvoiceID",
                table: "CreditNotes",
                column: "OriginalInvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNoteItems_DebitNoteID",
                table: "DebitNoteItems",
                column: "DebitNoteID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNoteItems_ProductID",
                table: "DebitNoteItems",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_BusinessID",
                table: "DebitNotes",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_CancelledByUserID",
                table: "DebitNotes",
                column: "CancelledByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_CreatedByUserID",
                table: "DebitNotes",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_DeletedByUserID",
                table: "DebitNotes",
                column: "DeletedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_FinalizedByUserID",
                table: "DebitNotes",
                column: "FinalizedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_ModifiedByUserID",
                table: "DebitNotes",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_OriginalInvoiceID",
                table: "DebitNotes",
                column: "OriginalInvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_BusinessID",
                table: "ProductionOrders",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CreatedByUserID",
                table: "ProductionOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_DeletedByUserID",
                table: "ProductionOrders",
                column: "DeletedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ModifiedByUserID",
                table: "ProductionOrders",
                column: "ModifiedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductID",
                table: "ProductionOrders",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CancelledByUserID",
                table: "Invoices",
                column: "CancelledByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedByUserID",
                table: "Invoices",
                column: "CreatedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_DeletedByUserID",
                table: "Invoices",
                column: "DeletedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_ModifiedByUserID",
                table: "Invoices",
                column: "ModifiedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_PostedByUserID",
                table: "Invoices",
                column: "PostedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_CancelledByUserID",
                table: "PurchaseOrders",
                column: "CancelledByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_CreatedByUserID",
                table: "PurchaseOrders",
                column: "CreatedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_DeletedByUserID",
                table: "PurchaseOrders",
                column: "DeletedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_ModifiedByUserID",
                table: "PurchaseOrders",
                column: "ModifiedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_PostedByUserID",
                table: "PurchaseOrders",
                column: "PostedByUserID",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CancelledByUserID",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedByUserID",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_DeletedByUserID",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_ModifiedByUserID",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_PostedByUserID",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_CancelledByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_CreatedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_DeletedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_ModifiedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_PostedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BillOfMaterials");

            migrationBuilder.DropTable(
                name: "CreditNoteItems");

            migrationBuilder.DropTable(
                name: "DebitNoteItems");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropTable(
                name: "DebitNotes");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CancelledByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_DeletedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ModifiedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PostedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CancelledByUserID",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CreatedByUserID",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_DeletedByUserID",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ModifiedByUserID",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PostedByUserID",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancelledByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PostedByUserID",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CancelledByUserID",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreatedByUserID",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedByUserID",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserID",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PostedByUserID",
                table: "Invoices");
        }
    }
}
