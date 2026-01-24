using System.ComponentModel.DataAnnotations;

namespace BusinessSuite.DAL.Entities;

public class UnitOfMeasure
{
    [Key]
    public int UomID { get; set; }
    
    [Required]
    public int BusinessId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty; // e.g., PCS, KG, BOX
    
    [MaxLength(50)]
    public string? Description { get; set; }
}
