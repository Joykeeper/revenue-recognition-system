using System.ComponentModel.DataAnnotations;

namespace Project.Dtos;

public class LoginDto
{
    [Required]
    [MaxLength(50)]
    public string Login { get; set; }
    [Required]
    [MaxLength(100)]
    public string Password { get; set; }
}