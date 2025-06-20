namespace Project.Dtos;

public class UpdateClientDto
{
    public string Address { get; set; }
    public string Email { get; set; }
    public int Phone { get; set; }

    // Optional updates
    public string? CompanyName { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
}