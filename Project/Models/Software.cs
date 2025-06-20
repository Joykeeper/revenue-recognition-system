using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

[Table("Software")]
public class Software
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PriceForYear { get; set; }
    
    public ICollection<Contract>? Contracts { get; set; }
}