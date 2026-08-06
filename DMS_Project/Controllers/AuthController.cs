using System.Security.Claims;
using DMS_Project.Auth;
using DMS_Project.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS_Project.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[ApiGroup("main")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var resp = _auth.Login(req);
        return resp.Success ? Ok(resp) : Unauthorized(resp);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var id = CurrentUserId();
        if (id == null) return Unauthorized();
        var u = _auth.GetCurrentUser(id.Value);
        if (u == null) return Unauthorized();
        return Ok(new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.DisplayName,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        });
    }

    [HttpGet("users")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public IActionResult ListUsers() => Ok(_auth.ListUsers());

    [HttpPost("users")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateUser([FromBody] CreateUserRequest req)
    {
        var actorId = CurrentUserId() ?? 0;
        var u = _auth.CreateUser(req, actorId);
        if (u == null) return BadRequest(new { message = "Tạo user thất bại (username trùng hoặc role không hợp lệ)" });
        return Created($"/api/auth/users/{u.Id}", u);
    }

    [HttpPatch("users/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateUser(int id, [FromBody] UpdateUserRequest req)
    {
        var u = _auth.UpdateUser(id, req);
        if (u == null) return NotFound(new { message = "User không tồn tại hoặc dữ liệu không hợp lệ" });
        return Ok(u);
    }

    [HttpPost("users/{id:int}/password")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AdminResetPassword(int id, [FromBody] ChangePasswordRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "newPassword là bắt buộc" });
        var ok = _auth.ChangePassword(id, req.NewPassword, isAdminReset: true);
        return ok ? Ok(new { message = "OK" }) : NotFound(new { message = "User không tồn tại" });
    }

    [HttpPost("me/password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ChangeOwnPassword([FromBody] ChangeOwnPasswordRequest req)
    {
        var id = CurrentUserId();
        if (id == null) return Unauthorized();
        if (req == null || string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "oldPassword và newPassword là bắt buộc" });
        var ok = _auth.ChangeOwnPassword(id.Value, req.OldPassword, req.NewPassword);
        return ok ? Ok(new { message = "OK" }) : BadRequest(new { message = "Mật khẩu cũ không đúng" });
    }

    private int? CurrentUserId()
    {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }
}

public class ChangeOwnPasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}