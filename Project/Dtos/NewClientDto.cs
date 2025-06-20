namespace Project.Dtos;

public class NewClientDto
{
    public string Address { get; set; }
    public string Email { get; set; }
    public int Phone { get; set; }
    
    public bool IsCompany { get; set; }
    
    public string? CompanyName { get; set; }
    public string? KRS { get; set; }
    
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public int? PESEL { get; set; }
}