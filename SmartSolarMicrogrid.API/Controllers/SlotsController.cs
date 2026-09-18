using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Slots;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly ISlotService _slotService;

    public SlotsController(ISlotService slotService)
    {
        _slotService = slotService;
    }

    /// <summary>List booking slots for a node on a specific local date.</summary>
    [HttpGet("api/v1/nodes/{id}/slots")]
    [HttpGet("api/nodes/{id}/slots")]
    [ProducesResponseType(typeof(List<SlotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForDay(
        string id,
        [FromQuery] string? date,
        CancellationToken ct)
    {
        var targetDate = string.IsNullOrWhiteSpace(date)
            ? DateOnly.FromDateTime(ColomboTime.NowLocal()).ToString("yyyy-MM-dd")
            : date;

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var prosumerView = role.Equals(RoleConstants.Prosumer, StringComparison.OrdinalIgnoreCase);

        var slots = await _slotService.GetSlotsForDayAsync(id, targetDate, prosumerView, ct);
        return Ok(slots);
    }

    /// <summary>Generate booking slots for a node over a date range — Backoffice only.</summary>
    [HttpPost("api/v1/nodes/{id}/slots/generate")]
    [HttpPost("api/nodes/{id}/slots/generate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(
        string id,
        [FromBody] GenerateSlotsRequestDto request,
        CancellationToken ct)
    {
        var count = await _slotService.GenerateSlotsForNodeAsync(id, request.FromDate, request.ToDate, ct);
        return Ok(new { message = $"Successfully generated or updated {count} slots." });
    }

    /// <summary>Update slot capacity or availability status — Grid Operator or Backoffice.</summary>
    [HttpPatch("api/v1/slots/{id}")]
    [HttpPatch("api/slots/{id}")]
    [Authorize(Roles = $"{RoleConstants.Backoffice},{RoleConstants.GridOperator}")]
    [ProducesResponseType(typeof(SlotResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateSlotDto request,
        CancellationToken ct)
    {
        var updated = await _slotService.UpdateSlotAsync(id, request, ct);
        return Ok(updated);
    }
}
