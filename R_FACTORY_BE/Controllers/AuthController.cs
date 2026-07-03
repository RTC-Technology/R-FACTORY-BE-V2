using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Services;
using System.Security.Claims;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public AuthController(IAuthService authService, IPermissionService permissionService)
    {
        _authService = authService;
        _permissionService = permissionService;
    }

    private string? GetClientIp()
    {
        var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return !string.IsNullOrEmpty(forwarded) ? forwarded.Split(',')[0].Trim() : HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetDeviceInfo()
    {
        return HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        var deviceInfo = GetDeviceInfo();
        var result = await _authService.LoginAsync(request, deviceInfo);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        var deviceInfo = GetDeviceInfo();
        var result = await _authService.RefreshAsync(request.RefreshToken, deviceInfo);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await _authService.LogoutAsync(userId.Value, request?.RefreshToken, GetClientIp());
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound();

        return Ok(user);
    }

    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> GetPermissions()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var permissions = await _permissionService.GetCurrentUserPermissionsAsync(userId.Value);
        return Ok(new UserPermissionsResponse { Permissions = permissions });
    }

    [HttpPost("hash-password")]
    [AllowAnonymous]
    public IActionResult HashPassword([FromBody] string password)
    {
        var hash = AuthService.HashPassword(password);
        return Ok(new { hash });
    }
}
