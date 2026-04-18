using DotLearn.Auth.Dtos.Auth;
using DotLearn.Auth.Dtos.Profile;
using DotLearn.Auth.Models.DTOs;

namespace DotLearn.Auth.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ConfirmPasswordResetAsync(string token, string newPassword);

    Task<GoogleAuthResponseDto> GoogleLoginAsync(GoogleAuthRequestDto request);
    Task<AuthResponseDto> CompleteGoogleSignupAsync(GoogleCompleteSignupRequestDto request);

    Task<ProfileResponseDto> GetProfileAsync(Guid userId);
    Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);

    // Admin Operations
    Task<IEnumerable<AuthUserDto>> GetAllUsersAsync(string? query, string? role);
    Task SuspendUserAsync(Guid targetUserId, Guid currentAdminId);
    Task UnsuspendUserAsync(Guid targetUserId);
    Task DeleteUserAsync(Guid targetUserId, Guid currentAdminId);
}
