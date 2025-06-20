using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

[Table("Discount")]
public class Discount
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int Percentage { get; set; } // DDL uses 'Procent' which maps to 'Percentage'

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")] // DDL specified varchar
    public string Name { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Contract>? Contracts { get; set; }
}