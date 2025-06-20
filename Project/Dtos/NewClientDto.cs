namespace Project.Dtos;

public class NewClientDto
{
    public string Address { get; set; }
    public string Email { get; set; }
    public int Phone { get; set; }

    // Optional: Discriminator property
    public bool IsCompany { get; set; }

    // Company fields
    public string? CompanyName { get; set; }
    public int? KRS { get; set; }

    // Individual fields
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public int? PESEL { get; set; }
}