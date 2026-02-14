using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Warehouse
{
    [Key]
    public int WarehouseID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }

    [Required]
    [MaxLength(100)]
    public string WarehouseName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(10)]
    public string? ZipCode { get; set; }

    public bool IsMainWarehouse { get; set; }
}
