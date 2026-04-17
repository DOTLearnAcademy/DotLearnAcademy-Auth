using DotLearn.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotLearn.Auth.Controllers;

[ApiController]
[Route("internal/users")]
public class InternalUsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public InternalUsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var user = await _authService.GetProfileAsync(id);
            return Ok(new { id = user.Id, fullName = user.FullName });
        }
        catch
        {
            return NotFound();
        }
    }
}
