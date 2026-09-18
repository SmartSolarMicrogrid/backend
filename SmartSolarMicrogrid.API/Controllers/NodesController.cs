using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.DTOs.Nodes;
using SmartSolarMicrogrid.API.Services.Interfaces;

namespace SmartSolarMicrogrid.API.Controllers;

[ApiController]
[Route("api/v1/nodes")]
[Authorize]
public class NodesController : ControllerBase
{
    private readonly INodeService _nodeService;

    public NodesController(INodeService nodeService)
    {
        _nodeService = nodeService;
    }

    /// <summary>List all nodes. Prosumers see Active nodes only.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NodeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var prosumerOnly = role.Equals(RoleConstants.Prosumer, StringComparison.OrdinalIgnoreCase);
        var nodes = await _nodeService.GetAllAsync(activeOnly: prosumerOnly, ct);
        return Ok(nodes);
    }

    /// <summary>Find nearby active nodes via geospatial distance.</summary>
    [HttpGet("nearby")]
    [Authorize(Roles = RoleConstants.Prosumer)]
    [ProducesResponseType(typeof(List<NodeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Nearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 50,
        [FromQuery] int max = 10,
        CancellationToken ct = default)
    {
        var nodes = await _nodeService.GetNearbyAsync(lat, lng, radiusKm, max, ct);
        return Ok(nodes);
    }

    /// <summary>Get node details by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var node = await _nodeService.GetByIdAsync(id, ct);
        return Ok(node);
    }

    /// <summary>Create a new solar station node — Backoffice only.</summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(NodeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNodeDto request, CancellationToken ct)
    {
        var node = await _nodeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = node.Id }, node);
    }

    /// <summary>Update solar station node details — Backoffice only.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(typeof(NodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateNodeDto request, CancellationToken ct)
    {
        var node = await _nodeService.UpdateAsync(id, request, ct);
        return Ok(node);
    }

    /// <summary>Deactivate a solar station node — Backoffice only (BR-06 check).</summary>
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        await _nodeService.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>Activate a solar station node — Backoffice only.</summary>
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = RoleConstants.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id, CancellationToken ct)
    {
        await _nodeService.ActivateAsync(id, ct);
        return NoContent();
    }
}
