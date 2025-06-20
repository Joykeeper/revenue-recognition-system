namespace Project.Dtos;

public class PaymentDto
{
    public int ClientId { get; set; }
    public int ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}