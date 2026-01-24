using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Customer
{
    [Key]
    public int CustomerID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? GSTIN { get; set; }

    [MaxLength(15)]
    public string? ContactNo { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? BillingAddress { get; set; }

    [MaxLength(255)]
    public string? ShippingAddress { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string Country { get; set; } = "India";

    [MaxLength(50)]
    public string? GstTreatment { get; set; } // Regular, Composition, Unregistered, etc.

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? AccountNumber { get; set; }

    [MaxLength(20)]
    public string? IFSC { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }
    public override string ToString() => CustomerName;
}
