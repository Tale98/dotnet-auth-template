using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Api.Models.User;

[Table("user", Schema = "api")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("username")]
    public required string Username { get; set; }
    [Column("hash_password")]
    public required string HashPassword { get; set; }
    [Column("email")]
    public required string Email { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public record UserRegisterRequest(
    [property: Required(ErrorMessage ="Username is required.")]
    [property: StringLength(32,MinimumLength=3)]
    string Username,
    [property: Required(ErrorMessage ="Password is required.")]
    [property: StringLength(64,MinimumLength=8)]
    [property: RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must be at least 8 characters, include at least one uppercase letter, one lowercase letter, one number, and one of these special character(@$!%*?&).")]
    string Password,
    [property: Required(ErrorMessage ="Email is required.")]
    [property: EmailAddress]
    string Email
    );
public record UserLoginRequest(
    [property: Required(ErrorMessage ="Username is required.")]
    string Username,
    [property: Required(ErrorMessage ="Password is required.")]
    string Password
    );