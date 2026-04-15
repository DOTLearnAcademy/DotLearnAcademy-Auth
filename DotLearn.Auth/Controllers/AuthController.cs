using DotLearn.Auth.Dtos.Auth;
using DotLearn.Auth.Dtos.Profile;
using DotLearn.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using DotLearn.Auth.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DotLearn.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }
    }

    // POST /api/auth/google
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequestDto request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
        {
            return BadRequest(new { error = "Invalid Google token" });
        }

        try
        {
            var result = await _authService.GoogleLoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid Google Sign-In credentials." });
        }
    }

    // POST /api/auth/google/complete
    [HttpPost("google/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteGoogleSignup([FromBody] GoogleCompleteSignupRequestDto request)
    {
        try
        {
            var result = await _authService.CompleteGoogleSignupAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }
    }

    // POST /api/auth/password-reset/request
    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] PasswordResetRequestDto request)
    {
        await _authService.RequestPasswordResetAsync(request.Email);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    // POST /api/auth/password-reset/confirm
    [HttpPost("password-reset/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPasswordReset(
        [FromBody] PasswordResetConfirmDto request)
    {
        try
        {
            await _authService.ConfirmPasswordResetAsync(request.Token, request.NewPassword);
            return Ok(new { message = "Password reset successful." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _authService.GetProfileAsync(userId);
        return Ok(result);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userId = GetUserId();
        var result = await _authService.UpdateProfileAsync(userId, request);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var userId =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("User ID not found.");

        return Guid.Parse(userId);
    }

    [HttpGet("/auth/.well-known/jwks.json")]
    [AllowAnonymous]
    public IActionResult GetJwks()
    {
        var privateKeyPem = _configuration["Jwt:PrivateKeyPem"];

        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            var pemPath = _configuration["Jwt:PrivateKeyPath"];
            if (!string.IsNullOrWhiteSpace(pemPath) && System.IO.File.Exists(pemPath))
            {
                privateKeyPem = System.IO.File.ReadAllText(pemPath);
            }
        }

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new InvalidOperationException("JWT private key is not configured.");

        privateKeyPem = privateKeyPem.Replace("\\n", "\n").Trim();

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var parameters = rsa.ExportParameters(false); // public key only
        var jwks = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA", use = "sig", alg = "RS256",
                    kid = "dotlearn-key-1",
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        };
        return Ok(jwks);
    }
}
