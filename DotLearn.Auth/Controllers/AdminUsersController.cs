using DotLearn.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdStr, out var adminId)) return Unauthorized();

            await _authService.SuspendUserAsync(id, adminId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(adminIdStr, out var adminId)) return Unauthorized();

            await _authService.DeleteUserAsync(id, adminId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User not found" });
        }
    }
}
