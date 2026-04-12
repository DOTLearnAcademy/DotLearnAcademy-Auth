using DotLearn.Auth.Models.DTOs;
using DotLearn.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

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

    [HttpGet("/auth/.well-known/jwks.json")]
    [AllowAnonymous]
    public IActionResult GetJwks()
    {
        var rsaKey = _configuration["dotlearn/jwt-private-key"];
        using var rsa = RSA.Create();
        rsa.ImportFromPem(rsaKey);
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
