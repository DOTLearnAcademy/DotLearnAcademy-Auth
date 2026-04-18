namespace DotLearn.Auth.Models.DTOs;

public record RegisterRequestDto(string FullName, string Email, string Password, string Role = "Student");

public record LoginRequestDto(string Email, string Password);




public record RefreshRequestDto(string RefreshToken);

public record PasswordResetRequestDto(string Email);

public record PasswordResetConfirmDto(string Token, string NewPassword);
