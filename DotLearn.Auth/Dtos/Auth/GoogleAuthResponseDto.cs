namespace DotLearn.Auth.Dtos.Auth;

#nullable disable

public class GoogleAuthResponseDto
{
    public bool RequiresOnboarding { get; set; }

    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int? ExpiresIn { get; set; }

    public AuthUserDto? User { get; set; }

    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? GoogleSubjectId { get; set; }
    public string? ProfileImageUrl { get; set; }
}
