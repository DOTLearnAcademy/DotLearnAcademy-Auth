namespace DotLearn.Auth.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = "Student";
    public string? AvatarS3Key { get; set; }
    public string? OAuthProvider { get; set; }
    public string? OAuthSubject { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
