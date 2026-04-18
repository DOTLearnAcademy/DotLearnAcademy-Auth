namespace DotLearn.Auth.Dtos.Auth;

#nullable disable

public class AuthResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public AuthUserDto User { get; set; }
}
