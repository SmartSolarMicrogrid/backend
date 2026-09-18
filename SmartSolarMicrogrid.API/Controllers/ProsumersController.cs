using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Prosumer;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Route("api/v1/prosumers")]
public class ProsumersController : ControllerBase
{
    private readonly IProsumerService _prosumerService;

    public ProsumersController(IProsumerService prosumerService)
    {
        _prosumerService = prosumerService;
    }

    /// <summary>List all prosumers — Backoffice only.</summary>
    [HttpGet]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(List<ProsumerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var prosumers = await _prosumerService.GetAllAsync();
        return Ok(prosumers);
    }

    /// <summary>Create a prosumer directly — Backoffice only.</summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProsumerDto request)
    {
        var prosumer = await _prosumerService.CreateByBackofficeAsync(request);
        return CreatedAtAction(nameof(Get), new { nic = prosumer.NIC }, prosumer);
    }

    /// <summary>Get prosumer by NIC or ID — Backoffice or Owner.</summary>
    [HttpGet("{nic}")]
    [Authorize]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string nic)
    {
        if (!await CanAccessProsumerAsync(nic))
            return Forbid();

        var prosumer = await _prosumerService.GetByNicOrIdAsync(nic);
        return Ok(prosumer);
    }

    /// <summary>Update prosumer profile by NIC or ID — Backoffice or Owner.</summary>
    [HttpPut("{nic}")]
    [Authorize]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string nic, [FromBody] ProsumerUpdateDto request)
    {
        if (!await CanAccessProsumerAsync(nic))
            return Forbid();

        var prosumer = await _prosumerService.UpdateByNicOrIdAsync(nic, request);
        return Ok(prosumer);
    }

    /// <summary>Delete prosumer by NIC or ID — Backoffice only.</summary>
    [HttpDelete("{nic}")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string nic)
    {
        await _prosumerService.DeleteByNicOrIdAsync(nic);
        return NoContent();
    }

    /// <summary>Deactivate prosumer account — Owner or Backoffice.</summary>
    [HttpPatch("{nic}/deactivate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string nic)
    {
        if (!await CanAccessProsumerAsync(nic))
            return Forbid();

        await _prosumerService.DeactivateByNicOrIdAsync(nic);
        return NoContent();
    }

    /// <summary>Reactivate prosumer account — Backoffice only.</summary>
    [HttpPatch("{nic}/reactivate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(string nic)
    {
        await _prosumerService.ReactivateByNicOrIdAsync(nic);
        return NoContent();
    }

    // ── Existing helper and backward-compatible endpoints ─────────────

    /// <summary>Prosumer self-registration — anonymous.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] ProsumerRegisterDto request)
    {
        var prosumer = await _prosumerService.RegisterAsync(request);
        return CreatedAtAction(nameof(Get), new { nic = prosumer.NIC }, prosumer);
    }

    /// <summary>List pending activation requests — Backoffice only.</summary>
    [HttpGet("pending")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(List<ProsumerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending()
    {
        var pending = await _prosumerService.GetPendingAsync();
        return Ok(pending);
    }

    /// <summary>Activate a pending prosumer — Backoffice only (backward compatible).</summary>
    [HttpPut("{id}/activate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id)
    {
        await _prosumerService.ActivateAsync(id);
        return NoContent();
    }

    /// <summary>Deactivate a prosumer — Backoffice only (backward compatible).</summary>
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LegacyDeactivate(string id)
    {
        await _prosumerService.DeactivateAsync(id);
        return NoContent();
    }

    /// <summary>Get own profile — Prosumer role only.</summary>
    [HttpGet("me")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")!;
        var prosumer = await _prosumerService.GetByNicOrIdAsync(id);
        return Ok(prosumer);
    }

    /// <summary>Edit own profile (name, phone, address) — Prosumer role only.</summary>
    [HttpPut("me")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe([FromBody] ProsumerUpdateDto request)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")!;
        var prosumer = await _prosumerService.UpdateProfileAsync(id, request);
        return Ok(prosumer);
    }

    /// <summary>Request own account deactivation — Prosumer role only.</summary>
    [HttpPut("me/request-deactivation")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestDeactivation()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")!;
        await _prosumerService.RequestDeactivationAsync(id);
        return NoContent();
    }

    private async Task<bool> CanAccessProsumerAsync(string nicOrId)
    {
        if (User.IsInRole(RoleConstants.Backoffice)) return true;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(currentUserId)) return false;

        if (string.Equals(currentUserId, nicOrId, StringComparison.OrdinalIgnoreCase)) return true;

        try
        {
            var p = await _prosumerService.GetByNicOrIdAsync(nicOrId);
            return string.Equals(p.Id, currentUserId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.NIC, currentUserId, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
