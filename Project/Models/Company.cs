using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Project.Models;

namespace Project.Models;


[Table("Company")]
public class Company : Client
{
    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string KRS { get; set; }
}