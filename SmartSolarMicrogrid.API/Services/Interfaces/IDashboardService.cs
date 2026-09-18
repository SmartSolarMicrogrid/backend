using SmartSolarMicrogrid.API.DTOs.Dashboard;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IDashboardService
{
    Task<OperatorDashboardDto> GetOperatorDashboardAsync(List<string> operatorNodeIds, CancellationToken ct = default);
    Task<BackofficeDashboardDto> GetBackofficeDashboardAsync(CancellationToken ct = default);
}
