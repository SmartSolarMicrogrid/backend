using SmartSolarMicrogrid.API.DTOs.Slots;

namespace SmartSolarMicrogrid.API.Services.Interfaces;

public interface ISlotService
{
    Task<List<SlotResponseDto>> GetSlotsForDayAsync(string nodeId, string localDate, bool prosumerView = false, CancellationToken ct = default);
    Task<int> GenerateSlotsForNodeAsync(string nodeId, string fromDate, string toDate, CancellationToken ct = default);
    Task<int> GenerateDailySlotsForAllActiveNodesAsync(int forwardDays = 7, CancellationToken ct = default);
    Task<SlotResponseDto> UpdateSlotAsync(string slotId, UpdateSlotDto request, CancellationToken ct = default);
}
