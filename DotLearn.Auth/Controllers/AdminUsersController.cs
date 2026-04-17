using DotLearn.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DotLearn.Auth.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public AdminUsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPut("{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id)
    {
        try
        {
            var adminId = GetAdminId();

            await _authService.SuspendUserAsync(id, adminId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
    }

    [HttpPut("{id:guid}/unsuspend")]
    public async Task<IActionResult> UnsuspendUser(Guid id)
    {
        try
        {
            await _authService.UnsuspendUserAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var adminId = GetAdminId();

            await _authService.DeleteUserAsync(id, adminId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
    }

    private Guid GetAdminId()
    {
        var adminId =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(adminId) || !Guid.TryParse(adminId, out var parsed))
            throw new UnauthorizedAccessException("Admin user ID not found in token.");

        return parsed;
    }
}
