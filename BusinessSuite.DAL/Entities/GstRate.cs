using System.ComponentModel.DataAnnotations;

namespace BusinessSuite.DAL.Entities;

public class GstRate
{
    [Key]
    public int RateID { get; set; }

    [Required]
    public decimal Percentage { get; set; }

    [MaxLength(50)]
    public string? Description { get; set; }
}
