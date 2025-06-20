using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Project.Models;

namespace Project.Models;


[Table("Contract")]
public class Contract
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SellingClientId { get; set; }

    [Required]
    public int BuyingClientId { get; set; }

    [Required]
    public int SoftwareId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }
    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    public int YearsOfUpdates { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string SoftwareVersion { get; set; } = string.Empty;

    [Required]
    public bool Signed { get; set; }

    public int? DiscountId { get; set; }

    [ForeignKey("SellingClientId")]
    public Client? SellingClient { get; set; }

    [ForeignKey("BuyingClientId")]
    public Client? BuyingClient { get; set; }

    [ForeignKey("SoftwareId")]
    public Software? Software { get; set; }

    [ForeignKey("DiscountId")]
    public Discount? Discount { get; set; }

    public ICollection<Payment>? Payments { get; set; }
}