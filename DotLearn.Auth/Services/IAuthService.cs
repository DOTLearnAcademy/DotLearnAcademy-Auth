using DotLearn.Auth.Models.DTOs;

namespace DotLearn.Auth.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ConfirmPasswordResetAsync(string token, string newPassword);
}
