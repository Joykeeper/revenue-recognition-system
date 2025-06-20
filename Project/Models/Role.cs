using System.ComponentModel.DataAnnotations;

namespace Project.Models;

public class Role
{
    [Key]
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Name { get; set; }
    
    public ICollection<User>? Users { get; set; } = new List<User>();

}