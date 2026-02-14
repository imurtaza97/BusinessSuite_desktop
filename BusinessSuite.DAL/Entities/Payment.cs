using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank, UPI, etc.

    [Required]
    [MaxLength(20)]
    public string PaymentType { get; set; } = "Received"; // Received (from Customer) or Paid (to Vendor)

    public int? CustomerID { get; set; }

    [ForeignKey("CustomerID")]
    public Customer? Customer { get; set; }

    public int? VendorID { get; set; }

    [ForeignKey("VendorID")]
    public Vendor? Vendor { get; set; }

    public int? ReferenceID { get; set; } // Related Invoice or PO ID

    [MaxLength(255)]
    public string? Note { get; set; }
}
