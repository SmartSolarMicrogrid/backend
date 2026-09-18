using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Prosumer;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Route("api/prosumers")]
public class ProsumersController : ControllerBase
{
    private readonly IProsumerService _prosumerService;

    public ProsumersController(IProsumerService prosumerService)
    {
        _prosumerService = prosumerService;
    }

    /// <summary>Prosumer self-registration — anonymous. NIC is the primary identifier.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] ProsumerRegisterDto request)
    {
        var prosumer = await _prosumerService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = prosumer.Id }, prosumer);
    }

    /// <summary>List all prosumers — Backoffice only.</summary>
    [HttpGet]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(List<ProsumerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var prosumers = await _prosumerService.GetAllAsync();
        return Ok(prosumers);
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

    /// <summary>Get prosumer by ID — Backoffice only.</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(ProsumerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var prosumer = await _prosumerService.GetByIdAsync(id);
        return Ok(prosumer);
    }

    /// <summary>Activate a pending prosumer — Backoffice only.</summary>
    [HttpPut("{id}/activate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id)
    {
        await _prosumerService.ActivateAsync(id);
        return NoContent();
    }

    /// <summary>Deactivate a prosumer — Backoffice only.</summary>
    [HttpPut("{id}/deactivate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
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
        var prosumer = await _prosumerService.GetByIdAsync(id);
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
}
