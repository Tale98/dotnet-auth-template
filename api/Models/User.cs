using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models.User;

[Table("user", Schema = "api")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("name")]
    public required string Name { get; set; }
    [Column("hash_password")]
    public required string HashPassword { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}