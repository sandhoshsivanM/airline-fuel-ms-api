using AirlineFuelMS.Core.DTOs.Auth;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Login — returns JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result is null)
            return Unauthorized(new { message = "Invalid username or password" });
        return Ok(result);
    }
}
