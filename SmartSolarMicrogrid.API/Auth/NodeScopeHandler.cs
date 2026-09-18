using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SmartSolarMicrogrid.API.Auth;

public class NodeScopeRequirement : IAuthorizationRequirement
{
    public string NodeId { get; }

    public NodeScopeRequirement(string nodeId)
    {
        NodeId = nodeId;
    }
}

public class NodeScopeHandler : AuthorizationHandler<NodeScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NodeScopeRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // Backoffice bypasses node assignment checks
        if (role.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (role.Equals(RoleConstants.GridOperator, StringComparison.OrdinalIgnoreCase))
        {
            var assignedNodes = context.User.FindAll("nodeIds").Select(c => c.Value);
            if (assignedNodes.Contains(requirement.NodeId))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
