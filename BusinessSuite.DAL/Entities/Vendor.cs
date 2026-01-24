using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Vendor
{
    [Key]
    public int VendorID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [Required]
    [MaxLength(100)]
    public string VendorName { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? GSTIN { get; set; }

    [MaxLength(15)]
    public string? ContactNo { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(50)]
    public string Country { get; set; } = "India";

    [MaxLength(50)]
    public string? GstTreatment { get; set; } = "Unregistered";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }
}
