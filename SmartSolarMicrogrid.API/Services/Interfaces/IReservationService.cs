using SmartSolarMicrogrid.API.DTOs.Dashboard;
using SmartSolarMicrogrid.API.DTOs.Reservations;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationResponse> CreateAsync(CreateReservationRequest request, string callerSub, string callerRole, CancellationToken ct = default);
    Task<List<ReservationResponse>> SearchAsync(ReservationSearchQuery query, string callerRole, List<string>? operatorNodeIds, CancellationToken ct = default);
    Task<List<ReservationResponse>> GetMineAsync(string prosumerNic, CancellationToken ct = default);
    Task<ReservationResponse> GetByIdAsync(string id, string callerSub, string callerRole, CancellationToken ct = default);
    Task<ReservationResponse> ModifyAsync(string id, ModifyReservationRequest request, string callerSub, string callerRole, CancellationToken ct = default);
    Task CancelAsync(string id, string callerSub, string callerRole, CancellationToken ct = default);
    Task<ReservationResponse> ApproveAsync(string id, string callerSub, string callerRole, List<string>? operatorNodeIds, CancellationToken ct = default);
    Task<ReservationResponse> RejectAsync(string id, RejectReservationRequest request, string callerSub, string callerRole, List<string>? operatorNodeIds, CancellationToken ct = default);
    Task<ProsumerDashboardDto> GetProsumerDashboardAsync(string prosumerNic, CancellationToken ct = default);
}
