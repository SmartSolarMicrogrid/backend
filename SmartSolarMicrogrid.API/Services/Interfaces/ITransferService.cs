using SmartSolarMicrogrid.API.DTOs.Reservations;
using SmartSolarMicrogrid.API.DTOs.Transfers;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface ITransferService
{
    Task<QrResponseDto> GetQrAsync(string reservationId, string callerSub, CancellationToken ct = default);
    Task<ReservationResponse> VerifyAsync(VerifyTransferRequest request, string operatorSub, List<string>? operatorNodeIds, CancellationToken ct = default);
    Task<ReservationResponse> FinalizeAsync(string reservationId, FinalizeTransferRequest request, string operatorSub, List<string>? operatorNodeIds, CancellationToken ct = default);
    Task<List<TransferRecordDto>> ListTransfersAsync(string? nodeId, DateTime? fromUtc, DateTime? toUtc, string callerRole, List<string>? operatorNodeIds, int limit = 50, CancellationToken ct = default);
}
