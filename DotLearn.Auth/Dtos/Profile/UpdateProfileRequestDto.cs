using System.ComponentModel.DataAnnotations;

namespace DotLearn.Auth.Dtos.Profile;

#nullable disable

public class UpdateProfileRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; }
}
