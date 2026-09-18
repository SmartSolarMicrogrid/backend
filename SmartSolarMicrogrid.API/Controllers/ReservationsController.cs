using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Reservations;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Route("api/v1/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    /// <summary>Create a new energy booking reservation — Prosumer or Backoffice.</summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleConstants.Prosumer},{RoleConstants.Backoffice}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var result = await _reservationService.CreateAsync(request, sub, role, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Search and list reservations — Backoffice or Grid Operator.</summary>
    [HttpGet]
    [Authorize(Roles = $"{RoleConstants.Backoffice},{RoleConstants.GridOperator}")]
    [ProducesResponseType(typeof(List<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ReservationSearchQuery query, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();

        var list = await _reservationService.SearchAsync(query, role, operatorNodeIds, ct);
        return Ok(list);
    }

    /// <summary>List own reservations — Prosumer only.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(typeof(List<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var list = await _reservationService.GetMineAsync(sub, ct);
        return Ok(list);
    }

    /// <summary>Get reservation by ID — Owner or Staff.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var result = await _reservationService.GetByIdAsync(id, sub, role, ct);
        return Ok(result);
    }

    /// <summary>Modify reservation energy or slot — Owner or Backoffice (BR-02).</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{RoleConstants.Prosumer},{RoleConstants.Backoffice}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Modify(string id, [FromBody] ModifyReservationRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var result = await _reservationService.ModifyAsync(id, request, sub, role, ct);
        return Ok(result);
    }

    /// <summary>Cancel a reservation — Owner or Staff (BR-03, BR-14).</summary>
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        await _reservationService.CancelAsync(id, sub, role, ct);
        return NoContent();
    }

    /// <summary>Approve a pending reservation — Grid Operator or Backoffice.</summary>
    [HttpPatch("{id}/approve")]
    [Authorize(Roles = $"{RoleConstants.Backoffice},{RoleConstants.GridOperator}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(string id, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();

        var result = await _reservationService.ApproveAsync(id, sub, role, operatorNodeIds, ct);
        return Ok(result);
    }

    /// <summary>Reject a pending reservation — Grid Operator or Backoffice (BR-14).</summary>
    [HttpPatch("{id}/reject")]
    [Authorize(Roles = $"{RoleConstants.Backoffice},{RoleConstants.GridOperator}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectReservationRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();

        var result = await _reservationService.RejectAsync(id, request, sub, role, operatorNodeIds, ct);
        return Ok(result);
    }
}
