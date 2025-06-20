using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Project.Models;

namespace Project.Models;


[Table("Company")] // Specifies that this class maps to the "Company" table
public class Company : Client
{
    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int KRS { get; set; }
}