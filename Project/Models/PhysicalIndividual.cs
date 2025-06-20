using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

// Represents the 'PhysicalIndividual' table
// This entity uses a shared primary key with Client
[Table("PhysicalIndividual")] // Specifies that this class maps to the "PhysicalIndividual" table
public class PhysicalIndividual : Client
{
    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string Surname { get; set; } = string.Empty;

    [Required]
    public int PESEL { get; set; } // Note: PESEL is typically a string, but DDL specified int.
}
