using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Auth;
using SmartSolarMicrogrid.API.DTOs.User;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Get own profile — available to all authenticated roles.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException();
        var user = await _userService.GetUserByIdAsync(userId);
        return Ok(user);
    }

    /// <summary>List all web app users — Backoffice only.</summary>
    [HttpGet]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>Get a specific user by ID — Backoffice only.</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    /// <summary>Create a new Backoffice or GridOperator user — Backoffice only.</summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RegisterRequestDto request)
    {
        var user = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Update a user's name, role, or active status — Backoffice only.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto request)
    {
        var user = await _userService.UpdateUserAsync(id, request);
        return Ok(user);
    }

    /// <summary>Delete a user — Backoffice only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }

    /// <summary>Change a user's password — Backoffice only.</summary>
    [HttpPut("{id}/password")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto request)
    {
        await _userService.ChangePasswordAsync(id, request);
        return NoContent();
    }
}
