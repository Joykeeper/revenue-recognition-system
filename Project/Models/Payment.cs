using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;


[Table("Payment")]
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    public int ClientId { get; set; } // Foreign Key

    [Required]
    public int ContractId { get; set; } // Foreign Key

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public bool Returned { get; set; } // DDL smallint, typically maps to bool for 0/1

    // Navigation properties
    [ForeignKey("ClientId")]
    public Client? Client { get; set; }

    [ForeignKey("ContractId")]
    public Contract? Contract { get; set; }
}