using System.ComponentModel.DataAnnotations;

namespace DotLearn.Auth.Dtos.Auth;

public class GoogleCompleteSignupRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    public string Role { get; set; } = null!; // Student / Instructor

    [Required]
    public string GoogleSubjectId { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }
}
