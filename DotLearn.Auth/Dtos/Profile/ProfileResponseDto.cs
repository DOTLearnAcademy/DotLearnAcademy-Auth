namespace DotLearn.Auth.Dtos.Profile;

#nullable disable

public class ProfileResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string AuthProvider { get; set; }
    public string? ProfileImageUrl { get; set; }
}
