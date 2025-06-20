using System.ComponentModel.DataAnnotations;

namespace Project.Dtos;

public class RegisterUserDto
{
    [Required]
    [MaxLength(50)]
    public string Login { get; set; }
    [Required]
    [MaxLength(100)]
    public string Password { get; set; }
    [Required]
    [MaxLength(50)]
    public string? Role { get; set; }
    [Required]
    [MaxLength(100)]
    public string? Email { get; set; }
}