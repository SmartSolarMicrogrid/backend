using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Dashboard;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IDashboardService? _dashboardService;

    public DashboardController(IReservationService reservationService, IServiceProvider serviceProvider)
    {
        _reservationService = reservationService;
        _dashboardService = serviceProvider.GetService(typeof(IDashboardService)) as IDashboardService;
    }

    /// <summary>Get prosumer personal trading and booking summary dashboard.</summary>
    [HttpGet("prosumer")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(typeof(ProsumerDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Prosumer(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;
        var dashboard = await _reservationService.GetProsumerDashboardAsync(sub, ct);
        return Ok(dashboard);
    }

    /// <summary>Get grid operator dashboard for assigned nodes.</summary>
    [HttpGet("operator")]
    [Authorize(Roles = RoleConstants.GridOperator)]
    public async Task<IActionResult> Operator(CancellationToken ct)
    {
        if (_dashboardService == null)
            return StatusCode(501, new { message = "Dashboard service not yet loaded." });

        var operatorNodeIds = User.FindAll("nodeIds").Select(c => c.Value).ToList();
        var result = await _dashboardService.GetOperatorDashboardAsync(operatorNodeIds, ct);
        return Ok(result);
    }

    /// <summary>Get backoffice system-wide trading and microgrid analytics dashboard.</summary>
    [HttpGet("backoffice")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    public async Task<IActionResult> Backoffice(CancellationToken ct)
    {
        if (_dashboardService == null)
            return StatusCode(501, new { message = "Dashboard service not yet loaded." });

        var result = await _dashboardService.GetBackofficeDashboardAsync(ct);
        return Ok(result);
    }
}
