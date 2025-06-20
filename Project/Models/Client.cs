namespace Project.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Client")]
public class Client
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(500)")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public int Phone { get; set; }
    public ICollection<Payment>? Payments { get; set; }

    [InverseProperty("SellingClient")]
    public ICollection<Contract>? ContractsAsSeller { get; set; }

    [InverseProperty("BuyingClient")]
    public ICollection<Contract>? ContractsAsBuyer { get; set; }
}