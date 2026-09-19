using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.Common.Errors;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.DTOs.Dashboard;
using SmartSolarMicrogrid.API.DTOs.Reservations;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Services.Policies;
using SmartSolarMicrogrid.API.Services.Pricing;

namespace SmartSolarMicrogrid.API.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly INodeRepository _nodeRepository;
    private readonly ISlotRepository _slotRepository;
    private readonly IProsumerRepository _prosumerRepository;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ReservationPolicy _policy;
    private readonly IEnumerable<ITradePricing> _pricingStrategies;

    public ReservationService(
        IReservationRepository reservationRepository,
        INodeRepository nodeRepository,
        ISlotRepository slotRepository,
        IProsumerRepository prosumerRepository,
        ITransactionRunner transactionRunner,
        ReservationPolicy policy,
        IEnumerable<ITradePricing> pricingStrategies)
    {
        _reservationRepository = reservationRepository;
        _nodeRepository = nodeRepository;
        _slotRepository = slotRepository;
        _prosumerRepository = prosumerRepository;
        _transactionRunner = transactionRunner;
        _policy = policy;
        _pricingStrategies = pricingStrategies;
    }

    public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request, string callerSub, string callerRole, CancellationToken ct = default)
    {
        var isBackoffice = callerRole.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase);
        var targetNic = isBackoffice && !string.IsNullOrEmpty(request.ProsumerNic) ? request.ProsumerNic : callerSub;

        // BR-10: Account re-read before write
        var prosumer = await _prosumerRepository.GetByNICAsync(targetNic);
        if (prosumer == null || prosumer.Status != ProsumerStatus.Active)
            throw new DomainException(ErrorCodes.AccountDeactivated, "Prosumer account is not active.");

        SolarStationInfo? node = null;
        if (ObjectId.TryParse(request.NodeId, out var nodeObjectId))
            node = await _nodeRepository.GetByIdAsync(nodeObjectId, ct);

        node ??= await _nodeRepository.GetByCodeAsync(request.NodeId, ct);

        if (node == null)
            throw new DomainException(ErrorCodes.NotFound, "Node not found.");

        if (!ObjectId.TryParse(request.SlotId, out var slotObjectId))
            throw new DomainException(ErrorCodes.ValidationFailed, "Invalid SlotId format.");

        // BR-07: Node must be active
        if (node.Status != NodeStatus.Active)
            throw new DomainException(ErrorCodes.SlotUnavailable, "Node is not active.");

        var slot = await _slotRepository.GetByIdAsync(slotObjectId, ct)
            ?? throw new DomainException(ErrorCodes.NotFound, "Slot not found.");

        if (slot.NodeId != node.Id)
            throw new DomainException(ErrorCodes.ValidationFailed, "Slot does not belong to the specified node.");

        // BR-07: Slot must be available
        if (slot.Status != SlotStatus.Available)
            throw new DomainException(ErrorCodes.SlotUnavailable, "Slot is not available.");

        // BR-01: Time window
        var (canBook, bookError) = _policy.CanBook(slot.StartUtc);
        if (!canBook)
            throw new DomainException(bookError ?? ErrorCodes.BookingWindow, "Booking slot is outside the allowed booking window.");

        // BR-05: Check energy
        var effectiveMaxKwh = node.MaxKwhPerReservation > 0 ? node.MaxKwhPerReservation : 50m;
        var (energyValid, energyError) = _policy.CheckEnergy(request.RequestedKwh, effectiveMaxKwh);
        if (!energyValid)
            throw new DomainException(energyError ?? ErrorCodes.ValidationFailed, $"Requested energy must be between {_policy.MinKwh} and {effectiveMaxKwh} kWh.");

        if (!Enum.TryParse<TradeType>(request.TradeType, true, out var tradeType))
            tradeType = TradeType.Export;

        // BR-13: Pricing strategy snapshot
        var pricingStrategy = _pricingStrategies.FirstOrDefault(p => p.TradeType == tradeType)
            ?? throw new DomainException(ErrorCodes.InternalError, $"No pricing strategy found for {tradeType}.");

        var unitPrice = pricingStrategy.UnitPrice(node.Pricing);

        var reservation = EnergyReservation.Create(
            targetNic,
            node,
            slot,
            tradeType,
            request.RequestedKwh,
            unitPrice,
            callerSub,
            DateTime.UtcNow);

        // BR-04: Atomic bay hold and insert in one transaction
        try
        {
            await _transactionRunner.RunAsync(async (session, token) =>
            {
                var held = await _slotRepository.TryHoldBayAsync(session, slot.Id, token);
                if (!held)
                    throw new DomainException(ErrorCodes.SlotFull, "No bays available in this slot.");

                await _reservationRepository.InsertAsync(session, reservation, token);
            }, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey
                                           || (ex.Message.Contains("ux_slot_prosumer_active", StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException(ErrorCodes.DuplicateBooking, "You already have an active booking in this slot.");
        }

        return MapToResponse(reservation);
    }

    public async Task<List<ReservationResponse>> SearchAsync(ReservationSearchQuery query, string callerRole, List<string>? operatorNodeIds, CancellationToken ct = default)
    {
        var isOperator = callerRole.Equals(RoleConstants.GridOperator, StringComparison.OrdinalIgnoreCase);
        List<ObjectId>? allowedNodes = null;

        if (isOperator)
        {
            allowedNodes = (operatorNodeIds ?? new List<string>())
                .Where(id => ObjectId.TryParse(id, out _))
                .Select(ObjectId.Parse)
                .ToList();
        }

        ObjectId? filterNode = null;
        if (!string.IsNullOrEmpty(query.NodeId) && ObjectId.TryParse(query.NodeId, out var parsedNode))
            filterNode = parsedNode;

        var list = await _reservationRepository.SearchAsync(
            query.Status,
            filterNode,
            allowedNodes,
            query.FromUtc,
            query.ToUtc,
            query.Limit,
            ct);

        return list.Select(MapToResponse).ToList();
    }

    public async Task<List<ReservationResponse>> GetMineAsync(string prosumerNic, CancellationToken ct = default)
    {
        var list = await _reservationRepository.GetByProsumerAsync(prosumerNic, ct);
        return list.Select(MapToResponse).ToList();
    }

    private async Task<EnergyReservation> FindReservationAsync(string id, CancellationToken ct)
    {
        EnergyReservation? reservation = null;
        if (ObjectId.TryParse(id, out var objectId))
        {
            reservation = await _reservationRepository.GetByIdAsync(objectId, ct);
        }

        if (reservation == null)
        {
            reservation = await _reservationRepository.GetByReservationNoAsync(id, ct);
        }

        return reservation ?? throw new DomainException(ErrorCodes.NotFound, "Reservation not found.");
    }

    public async Task<ReservationResponse> GetByIdAsync(string id, string callerSub, string callerRole, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(id, ct);

        var isOwner = reservation.ProsumerNic == callerSub;
        var isStaff = callerRole.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase) ||
                      callerRole.Equals(RoleConstants.GridOperator, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isStaff)
            throw new DomainException(ErrorCodes.NotOwner, "You are not authorized to view this reservation.");

        return MapToResponse(reservation);
    }

    public async Task<ReservationResponse> ModifyAsync(string id, ModifyReservationRequest request, string callerSub, string callerRole, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(id, ct);

        var isOwner = reservation.ProsumerNic == callerSub;
        var isBackoffice = callerRole.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isBackoffice)
            throw new DomainException(ErrorCodes.NotOwner, "Only the owner or backoffice can modify this reservation.");

        // BR-10: Status check
        var prosumer = await _prosumerRepository.GetByNICAsync(reservation.ProsumerNic);
        if (prosumer == null || prosumer.Status != ProsumerStatus.Active)
            throw new DomainException(ErrorCodes.AccountDeactivated, "Account is deactivated.");

        // BR-02: Change cutoff
        var (canChange, changeError) = _policy.CanChange(reservation.SlotStartUtc);
        if (!canChange)
            throw new DomainException(changeError ?? ErrorCodes.ChangeCutoff, "Modifications are closed within the change cutoff window.");

        var node = await _nodeRepository.GetByIdAsync(reservation.NodeId, ct)
            ?? throw new DomainException(ErrorCodes.NotFound, "Node not found.");

        if (request.NewRequestedKwh.HasValue)
        {
            // BR-05 check
            var (energyValid, energyError) = _policy.CheckEnergy(request.NewRequestedKwh.Value, node.MaxKwhPerReservation);
            if (!energyValid)
                throw new DomainException(energyError ?? ErrorCodes.ValidationFailed, $"Energy must be between {_policy.MinKwh} and {node.MaxKwhPerReservation} kWh.");

            reservation.RequestedKwh = request.NewRequestedKwh.Value;
        }

        if (!string.IsNullOrEmpty(request.NewTradeType) && Enum.TryParse<TradeType>(request.NewTradeType, true, out var newTradeType))
        {
            reservation.TradeType = newTradeType;
            var strategy = _pricingStrategies.FirstOrDefault(p => p.TradeType == newTradeType);
            if (strategy != null)
            {
                reservation.UnitPrice = strategy.UnitPrice(node.Pricing);
            }
        }

        reservation.EstimatedValue = Math.Round(reservation.RequestedKwh * reservation.UnitPrice, 2, MidpointRounding.AwayFromZero);

        // If slot is changed: handle bay release and hold
        if (!string.IsNullOrEmpty(request.NewSlotId) && ObjectId.TryParse(request.NewSlotId, out var newSlotId) && newSlotId != reservation.SlotId)
        {
            var newSlot = await _slotRepository.GetByIdAsync(newSlotId, ct)
                ?? throw new DomainException(ErrorCodes.NotFound, "Target slot not found.");

            var (canBookNew, bookNewError) = _policy.CanBook(newSlot.StartUtc);
            if (!canBookNew)
                throw new DomainException(bookNewError ?? ErrorCodes.BookingWindow, "New slot is outside booking window.");

            var oldSlotId = reservation.SlotId;
            reservation.SlotId = newSlot.Id;
            reservation.SlotStartUtc = newSlot.StartUtc;
            reservation.SlotEndUtc = newSlot.EndUtc;

            reservation.ReturnToPending(callerSub, DateTime.UtcNow);

            await _transactionRunner.RunAsync(async (session, token) =>
            {
                await _slotRepository.ReleaseBayAsync(session, oldSlotId, token);
                var held = await _slotRepository.TryHoldBayAsync(session, newSlot.Id, token);
                if (!held)
                    throw new DomainException(ErrorCodes.SlotFull, "Target slot has no available bays.");

                await _reservationRepository.SaveInTransactionAsync(session, reservation, token);
            }, ct);

            return MapToResponse(reservation);
        }

        reservation.ReturnToPending(callerSub, DateTime.UtcNow);
        await _reservationRepository.SaveAsync(reservation, ct);
        return MapToResponse(reservation);
    }

    public async Task CancelAsync(string id, string callerSub, string callerRole, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(id, ct);

        var isOwner = reservation.ProsumerNic == callerSub;
        var isStaff = callerRole.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase) ||
                      callerRole.Equals(RoleConstants.GridOperator, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isStaff)
            throw new DomainException(ErrorCodes.NotOwner, "Not authorized to cancel this reservation.");

        // BR-03: Cancellation cutoff check
        var (canChange, changeError) = _policy.CanChange(reservation.SlotStartUtc);
        if (!canChange)
            throw new DomainException(changeError ?? ErrorCodes.ChangeCutoff, "Cancellation is not permitted within the cutoff period.");

        reservation.Cancel(callerSub, DateTime.UtcNow);

        // BR-14: Bay released in the same transaction
        await _transactionRunner.RunAsync(async (session, token) =>
        {
            await _slotRepository.ReleaseBayAsync(session, reservation.SlotId, token);
            await _reservationRepository.SaveInTransactionAsync(session, reservation, token);
        }, ct);
    }

    public async Task<ReservationResponse> ApproveAsync(string id, string callerSub, string callerRole, List<string>? operatorNodeIds, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(id, ct);

        var isBackoffice = callerRole.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase);
        if (!isBackoffice)
        {
            // BR-11: Grid Operator node scope check
            if (operatorNodeIds == null || !operatorNodeIds.Contains(reservation.NodeId.ToString()))
                throw new DomainException(ErrorCodes.NotAssignedToNode, "You are not assigned to the node for this reservation.");
        }

        var nowUtc = DateTime.UtcNow;
        var validFromUtc = reservation.SlotStartUtc.AddMinutes(-30);
        reservation.Approve(callerSub, nowUtc, validFromUtc);

        await _reservationRepository.SaveAsync(reservation, ct);
        return MapToResponse(reservation);
    }

    public async Task<ReservationResponse> RejectAsync(string id, RejectReservationRequest request, string callerSub, string callerRole, List<string>? operatorNodeIds, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(id, ct);

        var isBackoffice = callerRole.Equals(RoleConstants.Backoffice, StringComparison.OrdinalIgnoreCase);
        if (!isBackoffice)
        {
            // BR-11: Operator check
            if (operatorNodeIds == null || !operatorNodeIds.Contains(reservation.NodeId.ToString()))
                throw new DomainException(ErrorCodes.NotAssignedToNode, "You are not assigned to the node for this reservation.");
        }

        reservation.Reject(callerSub, DateTime.UtcNow, request.Reason);

        // BR-14: Bay released in the same transaction
        await _transactionRunner.RunAsync(async (session, token) =>
        {
            await _slotRepository.ReleaseBayAsync(session, reservation.SlotId, token);
            await _reservationRepository.SaveInTransactionAsync(session, reservation, token);
        }, ct);

        return MapToResponse(reservation);
    }

    public async Task<ProsumerDashboardDto> GetProsumerDashboardAsync(string prosumerNic, CancellationToken ct = default)
    {
        var prosumer = await _prosumerRepository.GetByNICAsync(prosumerNic);
        var allReservations = await _reservationRepository.GetByProsumerAsync(prosumerNic, ct);

        var activeBookings = allReservations.Where(r => r.IsActive).ToList();
        var completedTransfers = allReservations.Where(r => r.Status == ReservationStatus.Completed).ToList();

        var exportedKwh = completedTransfers
            .Where(r => r.TradeType == TradeType.Export)
            .Sum(r => r.Transaction?.ActualKwh ?? 0m);

        var importedKwh = completedTransfers
            .Where(r => r.TradeType == TradeType.Import)
            .Sum(r => r.Transaction?.ActualKwh ?? 0m);

        var earnings = completedTransfers
            .Where(r => r.TradeType == TradeType.Export)
            .Sum(r => r.Transaction?.Value ?? 0m);

        var payments = completedTransfers
            .Where(r => r.TradeType == TradeType.Import)
            .Sum(r => r.Transaction?.Value ?? 0m);

        return new ProsumerDashboardDto
        {
            Nic = prosumerNic,
            FullName = prosumer?.FullName ?? string.Empty,
            ActiveBookingsCount = activeBookings.Count,
            CompletedTransfersCount = completedTransfers.Count,
            TotalEnergyExportedKwh = exportedKwh,
            TotalEnergyImportedKwh = importedKwh,
            NetEarnings = earnings - payments,
            UpcomingBookings = activeBookings.OrderBy(r => r.SlotStartUtc).Take(5).Select(MapToResponse).ToList(),
            RecentTransfers = completedTransfers.OrderByDescending(r => r.SlotStartUtc).Take(5).Select(MapToResponse).ToList()
        };
    }

    private ReservationResponse MapToResponse(EnergyReservation r)
    {
        var now = DateTime.UtcNow;
        var deadline = _policy.ChangeDeadlineUtc(r.SlotStartUtc);
        var canModifyOrCancel = (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved) && now < deadline;

        return new ReservationResponse
        {
            Id = r.Id.ToString(),
            ReservationNo = r.ReservationNo,
            ProsumerNic = r.ProsumerNic,
            NodeId = r.NodeId.ToString(),
            SlotId = r.SlotId.ToString(),
            NodeName = r.NodeName,
            SlotStartUtc = r.SlotStartUtc,
            SlotEndUtc = r.SlotEndUtc,
            TradeType = r.TradeType.ToString(),
            RequestedKwh = r.RequestedKwh,
            UnitPrice = r.UnitPrice,
            EstimatedValue = r.EstimatedValue,
            Status = r.Status.ToString(),
            IsActive = r.IsActive,
            Version = r.Version,
            ChangeDeadlineUtc = deadline,
            CanModify = canModifyOrCancel,
            CanCancel = canModifyOrCancel,
            QrVersion = r.Qr?.Version,
            QrIssuedAtUtc = r.Qr?.IssuedAtUtc,
            Transaction = r.Transaction == null ? null : new TransferRecordSummary
            {
                MeterStartKwh = r.Transaction.MeterStartKwh,
                MeterEndKwh = r.Transaction.MeterEndKwh,
                ActualKwh = r.Transaction.ActualKwh,
                Value = r.Transaction.Value,
                FinalizedBy = r.Transaction.FinalizedBy,
                FinalizedAtUtc = r.Transaction.FinalizedAtUtc
            }
        };
    }
}
