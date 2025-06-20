using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(50)]
    public string Login { get; set; }
    [Required]
    [MaxLength(100)]
    public string Password { get; set; }
    [Required]
    [MaxLength(100)]
    public string UserSalt { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    public int RoleId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }
}