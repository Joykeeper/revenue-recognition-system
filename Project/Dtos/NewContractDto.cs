namespace Project.Dtos;

public class NewContractDto
{
    public int BuyingClientId { get; set; }
    public int SellingClientId { get; set; }
    public int SoftwareId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int YearsOfUpdates { get; set; } // 1, 2 or 3
    public string SoftwareVersion { get; set; } = null!;

    public int? DiscountId { get; set; } // Optional

    public bool IsReturningClient { get; set; } // Used to apply extra 5% discount
}