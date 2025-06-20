using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Project.Models;

namespace Project.Models;


[Table("Client")]
public class Contract
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Required]
    public int SellingClientId { get; set; } // Foreign Key

    [Required]
    public int BuyingClientId { get; set; } // Foreign Key

    [Required]
    public int SoftwareId { get; set; } // Foreign Key

    [Required]
    public DateTime StartTime { get; set; }
    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; } // DDL specified int, consider decimal for currency in real apps

    [Required]
    public int YearsOfUpdates { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string SoftwareVersion { get; set; } = string.Empty;

    [Required]
    public bool Signed { get; set; } // DDL smallint, typically maps to bool for 0/1

    public int? DiscountId { get; set; } // Nullable Foreign Key

    // Navigation properties
    [ForeignKey("SellingClientId")]
    public Client? SellingClient { get; set; }

    [ForeignKey("BuyingClientId")]
    public Client? BuyingClient { get; set; }

    [ForeignKey("SoftwareId")]
    public Software? Software { get; set; }

    [ForeignKey("DiscountId")]
    public Discount? Discount { get; set; } // Nullable navigation property

    public ICollection<Payment>? Payments { get; set; }
}