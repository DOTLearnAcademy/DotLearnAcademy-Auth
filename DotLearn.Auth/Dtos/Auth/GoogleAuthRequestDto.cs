namespace DotLearn.Auth.Dtos.Auth;

public class GoogleAuthRequestDto
{
    public string IdToken { get; set; } = null!;
    public string? Role { get; set; }
}
