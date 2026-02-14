using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Stock
{
    [Key]
    public int StockID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [Required]
    public int WarehouseID { get; set; }

    [ForeignKey("WarehouseID")]
    public Warehouse? Warehouse { get; set; }

    [Required]
    public decimal Quantity { get; set; } = 0;
}
