using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Category
{
    [Key]
    public int CategoryID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public override string ToString() => Name;
}
