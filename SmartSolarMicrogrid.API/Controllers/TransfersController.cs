using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Reservations;
using SmartSolarMicrogrid.API.DTOs.Transfers;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>Get QR code signature payload and backup code for an approved reservation — Owner only.</summary>
    [HttpGet("api/v1/reservations/{id}/qr")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(typeof(QrResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQr(string id, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var qr = await _transferService.GetQrAsync(id, sub, ct);
        return Ok(qr);
    }

    /// <summary>Scan and verify QR code or backup code — Grid Operator only (BR-11, BR-12).</summary>
    [HttpPost("api/v1/transfers/verify")]
    [Authorize(Roles = RoleConstants.GridOperator)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verify([FromBody] VerifyTransferRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();

        var result = await _transferService.VerifyAsync(request, sub, operatorNodeIds, ct);
        return Ok(result);
    }

    /// <summary>Finalize energy transfer with meter readings — Grid Operator assigned node (BR-13).</summary>
    [HttpPost("api/v1/transfers/{reservationId}/finalize")]
    [Authorize(Roles = RoleConstants.GridOperator)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finalize(string reservationId, [FromBody] FinalizeTransferRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();

        var result = await _transferService.FinalizeAsync(reservationId, request, sub, operatorNodeIds, ct);
        return Ok(result);
    }

    /// <summary>List completed energy transfer transactions — Backoffice and Grid Operator.</summary>
    [HttpGet("api/v1/transfers")]
    [Authorize(Roles = $"{RoleConstants.Backoffice},{RoleConstants.GridOperator}")]
    [ProducesResponseType(typeof(List<TransferRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? nodeId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();

        var list = await _transferService.ListTransfersAsync(nodeId, fromUtc, toUtc, role, operatorNodeIds, limit, ct);
        return Ok(list);
    }
}
