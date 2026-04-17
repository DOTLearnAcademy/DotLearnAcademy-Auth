using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using DotLearn.Auth.Dtos.Auth;
using DotLearn.Auth.Dtos.Profile;
using DotLearn.Auth.Models.DTOs;
using DotLearn.Auth.Models.Entities;
using DotLearn.Auth.Repositories;
using DotLearn.Auth.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;
using System.Security.Cryptography;

namespace DotLearn.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IGoogleAuthService _googleAuthService;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, IGoogleAuthService googleAuthService)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _googleAuthService = googleAuthService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role,
            AuthProvider = "Local"
        };

        await _userRepository.AddAsync(user);
        var tokens = await GenerateTokensAsync(user);
        return tokens;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsActive || user.IsDeleted)
            throw new UnauthorizedAccessException("Account is suspended or deleted.");

        var tokens = await GenerateTokensAsync(user);
        return tokens;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        if (!user.IsActive || user.IsDeleted)
            throw new UnauthorizedAccessException("Account is suspended or deleted.");

        // Invalidate old token immediately
        user.RefreshToken = null;
        await _userRepository.UpdateAsync(user);

        var tokens = await GenerateTokensAsync(user);
        return tokens;
    }

    public async Task<GoogleAuthResponseDto> GoogleLoginAsync(GoogleAuthRequestDto request)
    {
        var payload = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);

        var googleSubjectId = payload.Subject;
        var email = payload.Email;
        var fullName = payload.Name;
        var picture = payload.Picture;

        var user = await _userRepository.GetByOAuthSubjectAsync("Google", googleSubjectId);

        if (user == null)
        {
            user = await _userRepository.GetByEmailAsync(email);
        }

        if (user != null)
        {
            if (!user.IsActive || user.IsDeleted)
                throw new UnauthorizedAccessException("Account is suspended or deleted.");

            if (string.IsNullOrWhiteSpace(user.GoogleSubjectId))
            {
                user.GoogleSubjectId = googleSubjectId;
                user.AuthProvider = "Google";
                user.ProfileImageUrl ??= picture;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            var tokens = await GenerateTokensAsync(user);

            return new GoogleAuthResponseDto
            {
                RequiresOnboarding = false,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresIn = tokens.ExpiresIn,
                User = MapUser(user)
            };
        }

        return new GoogleAuthResponseDto
        {
            RequiresOnboarding = true,
            Email = email,
            FullName = fullName,
            GoogleSubjectId = googleSubjectId,
            ProfileImageUrl = picture
        };
    }

    public async Task<AuthResponseDto> CompleteGoogleSignupAsync(GoogleCompleteSignupRequestDto request)
    {
        if (request.Role != "Student" && request.Role != "Instructor")
            throw new ArgumentException("Invalid role.");

        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null) throw new InvalidOperationException("User already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Role = request.Role,
            AuthProvider = "Google",
            GoogleSubjectId = request.GoogleSubjectId,
            ProfileImageUrl = request.ProfileImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        var tokens = await GenerateTokensAsync(user);
        return tokens;
    }

    public async Task<ProfileResponseDto> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        return new ProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AuthProvider = user.AuthProvider,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        user.FullName = request.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return new ProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AuthProvider = user.AuthProvider,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return true; // Don't reveal if email exists

        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
        await _userRepository.UpdateAsync(user);

        // TODO: Publish PasswordResetRequested to SQS in Phase 4
        return true;
    }

    public async Task<bool> ConfirmPasswordResetAsync(string token, string newPassword)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(token);

        if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired reset token");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        await _userRepository.UpdateAsync(user);

        return true;
    }

    // ─── ADMIN OPERATIONS ────────────────────────────────────────────────────────

    public async Task<IEnumerable<AuthUserDto>> GetAllUsersAsync(string? query, string? role)
    {
        var users = await _userRepository.GetAllUsersAsync(query, role);
        return users.Select(MapUser);
    }

    public async Task SuspendUserAsync(Guid targetUserId, Guid currentAdminId)
    {
        if (targetUserId == currentAdminId)
            throw new InvalidOperationException("You cannot suspend your own account.");

        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == "Admin")
        {
            var activeAdmins = await _userRepository.GetActiveAdminCountAsync();
            if (activeAdmins <= 1 && user.IsActive && !user.IsDeleted)
                throw new InvalidOperationException("Cannot suspend the only active administrator.");
        }

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);
    }

    public async Task UnsuspendUserAsync(Guid targetUserId)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(Guid targetUserId, Guid currentAdminId)
    {
        if (targetUserId == currentAdminId)
            throw new InvalidOperationException("You cannot delete your own account.");

        var user = await _userRepository.GetByIdAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == "Admin")
        {
            var activeAdmins = await _userRepository.GetActiveAdminCountAsync();
            if (activeAdmins <= 1 && user.IsActive && !user.IsDeleted)
                throw new InvalidOperationException("Cannot delete the only active administrator.");
        }

        user.IsActive = false;
        user.IsDeleted = true;
        
        await _userRepository.UpdateAsync(user);
    }

    private AuthUserDto MapUser(User user)
    {
        return new AuthUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AuthProvider = user.AuthProvider ?? "Local",
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    private async Task<AuthResponseDto> GenerateTokensAsync(User user)
    {
        var accessToken = GenerateJwt(user);
        var refreshToken = Guid.NewGuid().ToString();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            User = MapUser(user)
        };
    }

    private string GenerateJwt(User user)
    {
        var privateKeyPem = _configuration["Jwt:PrivateKeyPem"];

        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            var pemPath = _configuration["Jwt:PrivateKeyPath"];
            if (!string.IsNullOrWhiteSpace(pemPath) && File.Exists(pemPath))
            {
                privateKeyPem = File.ReadAllText(pemPath);
            }
        }

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new InvalidOperationException("JWT private key is not configured.");

        privateKeyPem = privateKeyPem.Replace("\\n", "\n").Trim();

        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        
        // Export/Import into a new RSA to use with credentials without disposing
        var rsaParameters = rsa.ExportParameters(true);
        var rsaForCredentials = RSA.Create();
        rsaForCredentials.ImportParameters(rsaParameters);

        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsaForCredentials) { KeyId = "dotlearn-key-1" },
            SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "dotlearn-auth",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
