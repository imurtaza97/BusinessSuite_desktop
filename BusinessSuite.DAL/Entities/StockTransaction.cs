using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class StockTransaction
{
    [Key]
    public int TransactionID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [Required]
    public string TransactionType { get; set; } = string.Empty; // Sales (Invoice), Purchase (PO), Adjustment, Return, Transfer

    [Required]
    public decimal Quantity { get; set; } // Positive for stock-in, negative for stock-out

    public int? WarehouseID { get; set; }
    
    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    public int? ToWarehouseID { get; set; } // For transfers

    [ForeignKey("ToWarehouseID")]
    public Warehouse? ToWarehouse { get; set; }

    public string? TransactionNumber { get; set; } // ST-001
    public string? ReferenceType { get; set; } // Purchase, Sale, Transfer, Adjustment
    public int? ReferenceID { get; set; } // InvoiceID or PurchaseOrderID or TransferID
    
    public decimal? PreviousQuantity { get; set; }
    public decimal? NewQuantity { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }
}
