using MongoDB.Bson;
using SmartSolarMicrogrid.API.Common.Errors;
using SmartSolarMicrogrid.API.DTOs.Slots;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Utilities;

namespace SmartSolarMicrogrid.API.Services;

public class SlotService : ISlotService
{
    private readonly ISlotRepository _slotRepository;
    private readonly INodeRepository _nodeRepository;

    public SlotService(ISlotRepository slotRepository, INodeRepository nodeRepository)
    {
        _slotRepository = slotRepository;
        _nodeRepository = nodeRepository;
    }

    public async Task<List<SlotResponseDto>> GetSlotsForDayAsync(string nodeId, string localDate, bool prosumerView = false, CancellationToken ct = default)
    {
        SolarStationInfo? node = null;
        if (ObjectId.TryParse(nodeId, out var nodeObjectId))
            node = await _nodeRepository.GetByIdAsync(nodeObjectId, ct);

        node ??= await _nodeRepository.GetByCodeAsync(nodeId, ct);

        if (node == null)
            throw new DomainException(ErrorCodes.NotFound, "Node not found.");

        if (prosumerView && node.Status != NodeStatus.Active)
            throw new DomainException(ErrorCodes.SlotUnavailable, "Node is not active.");

        var slots = await _slotRepository.GetByNodeAndDayAsync(node.Id, localDate, ct);

        if (prosumerView)
        {
            // Prosumers see only Available slots that are not in the past
            slots = slots.Where(s => s.Status == SlotStatus.Available && s.StartUtc > DateTime.UtcNow).ToList();
        }

        return slots.Select(MapToDto).ToList();
    }

    public async Task<int> GenerateSlotsForNodeAsync(string nodeId, string fromDate, string toDate, CancellationToken ct = default)
    {
        SolarStationInfo? node = null;
        if (ObjectId.TryParse(nodeId, out var nodeObjectId))
            node = await _nodeRepository.GetByIdAsync(nodeObjectId, ct);

        node ??= await _nodeRepository.GetByCodeAsync(nodeId, ct);

        if (node == null)
            throw new DomainException(ErrorCodes.NotFound, "Node not found.");

        if (!DateOnly.TryParse(fromDate, out var startDate) || !DateOnly.TryParse(toDate, out var endDate))
            throw new DomainException(ErrorCodes.ValidationFailed, "Invalid date format. Expected YYYY-MM-DD.");

        var slots = GenerateSlotsForDateRange(node, startDate, endDate);
        await _slotRepository.UpsertSlotsAsync(slots, ct);
        return slots.Count;
    }

    public async Task<int> GenerateDailySlotsForAllActiveNodesAsync(int forwardDays = 7, CancellationToken ct = default)
    {
        var activeNodes = await _nodeRepository.GetAllAsync(activeOnly: true, ct);
        var totalGenerated = 0;
        var today = DateOnly.FromDateTime(ColomboTime.NowLocal());
        var endDay = today.AddDays(forwardDays);

        foreach (var node in activeNodes)
        {
            var slots = GenerateSlotsForDateRange(node, today, endDay);
            await _slotRepository.UpsertSlotsAsync(slots, ct);
            totalGenerated += slots.Count;
        }

        return totalGenerated;
    }

    public async Task<SlotResponseDto> UpdateSlotAsync(string slotId, UpdateSlotDto request, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(slotId, out var slotObjectId))
            throw new DomainException(ErrorCodes.NotFound, "Invalid slot ID format.");

        var slot = await _slotRepository.GetByIdAsync(slotObjectId, ct)
            ?? throw new DomainException(ErrorCodes.NotFound, "Slot not found.");

        // BR-08: Block only an empty slot; capacity never below bookedCount
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<SlotStatus>(request.Status, true, out var newStatus))
            {
                if (newStatus == SlotStatus.Blocked && slot.BookedCount > 0)
                    throw new DomainException(ErrorCodes.SlotInUse, "Cannot block a slot that has existing bookings.");

                slot.Status = newStatus;
            }
        }

        if (request.Capacity.HasValue)
        {
            if (request.Capacity.Value < slot.BookedCount)
                throw new DomainException(ErrorCodes.SlotInUse, $"Capacity cannot be less than current booked count ({slot.BookedCount}).");

            slot.Capacity = request.Capacity.Value;
            if (slot.BookedCount >= slot.Capacity && slot.Status == SlotStatus.Available)
            {
                slot.Status = SlotStatus.Full;
            }
            else if (slot.BookedCount < slot.Capacity && slot.Status == SlotStatus.Full)
            {
                slot.Status = SlotStatus.Available;
            }
        }

        await _slotRepository.UpdateAsync(slot, ct);
        return MapToDto(slot);
    }

    private static List<EnergyBookingSlot> GenerateSlotsForDateRange(SolarStationInfo node, DateOnly startDate, DateOnly endDate)
    {
        var slots = new List<EnergyBookingSlot>();
        var duration = TimeSpan.FromMinutes(node.OpeningHours.SlotDurationMinutes > 0 ? node.OpeningHours.SlotDurationMinutes : 60);

        if (!TimeOnly.TryParse(node.OpeningHours.OpenTime, out var openTime))
            openTime = new TimeOnly(6, 0);

        if (!TimeOnly.TryParse(node.OpeningHours.CloseTime, out var closeTime))
            closeTime = new TimeOnly(20, 0);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dateString = date.ToString("yyyy-MM-dd");
            var currentStart = openTime;

            while (currentStart.Add(duration) <= closeTime)
            {
                var currentEnd = currentStart.Add(duration);
                var startUtc = ColomboTime.ToUtc(date, currentStart);
                var endUtc = ColomboTime.ToUtc(date, currentEnd);

                slots.Add(new EnergyBookingSlot
                {
                    Id = ObjectId.GenerateNewId(),
                    NodeId = node.Id,
                    LocalDate = dateString,
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    Capacity = node.CapacityBays,
                    BookedCount = 0,
                    Status = SlotStatus.Available,
                    Version = 1
                });

                currentStart = currentEnd;
            }
        }

        return slots;
    }

    private static SlotResponseDto MapToDto(EnergyBookingSlot slot) => new()
    {
        Id = slot.Id.ToString(),
        NodeId = slot.NodeId.ToString(),
        LocalDate = slot.LocalDate,
        StartUtc = slot.StartUtc,
        EndUtc = slot.EndUtc,
        Capacity = slot.Capacity,
        BookedCount = slot.BookedCount,
        Status = slot.Status.ToString(),
        Version = slot.Version
    };
}
