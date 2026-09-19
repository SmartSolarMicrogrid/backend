using MongoDB.Bson;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.Common.Errors;
using SmartSolarMicrogrid.API.DTOs.Reservations;
using SmartSolarMicrogrid.API.DTOs.Transfers;
using SmartSolarMicrogrid.API.Models;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Services.Policies;

namespace SmartSolarMicrogrid.API.Services;

public class TransferService : ITransferService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly QrService _qrService;
    private readonly ReservationPolicy _policy;

    public TransferService(
        IReservationRepository reservationRepository,
        QrService qrService,
        ReservationPolicy policy)
    {
        _reservationRepository = reservationRepository;
        _qrService = qrService;
        _policy = policy;
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

    public async Task<QrResponseDto> GetQrAsync(string reservationId, string callerSub, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(reservationId, ct);

        if (reservation.ProsumerNic != callerSub)
            throw new DomainException(ErrorCodes.NotOwner, "Only the reservation owner can retrieve the QR code.");

        if (reservation.Status != ReservationStatus.Approved || reservation.Qr == null)
            throw new DomainException(ErrorCodes.InvalidState, "QR code is only available for Approved reservations.");

        var payload = _qrService.CreatePayload(reservation.Id.ToString(), reservation.Qr.Version);
        var backupCode = _qrService.BackupCode(reservation.Id.ToString(), reservation.Qr.Version);

        return new QrResponseDto
        {
            ReservationId = reservation.Id.ToString(),
            ReservationNo = reservation.ReservationNo,
            Version = reservation.Qr.Version,
            Payload = payload,
            BackupCode = backupCode,
            ValidFromUtc = reservation.Qr.ValidFromUtc,
            ValidToUtc = reservation.Qr.ValidToUtc
        };
    }

    public async Task<ReservationResponse> VerifyAsync(VerifyTransferRequest request, string operatorSub, List<string>? operatorNodeIds, CancellationToken ct = default)
    {
        string targetReservationId;
        int targetQrVersion;

        if (!string.IsNullOrEmpty(request.Payload))
        {
            if (!_qrService.TryRead(request.Payload, out targetReservationId, out targetQrVersion))
                throw new DomainException(ErrorCodes.QrInvalid, "QR code payload format or cryptographic signature is invalid.");
        }
        else if (!string.IsNullOrEmpty(request.ReservationId) && !string.IsNullOrEmpty(request.BackupCode))
        {
            targetReservationId = request.ReservationId;
            if (!ObjectId.TryParse(targetReservationId, out var resId))
                throw new DomainException(ErrorCodes.QrInvalid, "Invalid reservation ID.");

            var res = await _reservationRepository.GetByIdAsync(resId, ct)
                ?? throw new DomainException(ErrorCodes.QrInvalid, "Reservation not found.");

            if (res.Qr == null)
                throw new DomainException(ErrorCodes.QrInvalid, "Reservation has no QR information.");

            var expectedCode = _qrService.BackupCode(res.Id.ToString(), res.Qr.Version);
            if (expectedCode != request.BackupCode)
                throw new DomainException(ErrorCodes.QrInvalid, "Backup code does not match.");

            targetQrVersion = res.Qr.Version;
        }
        else
        {
            throw new DomainException(ErrorCodes.QrInvalid, "Missing QR payload or backup code.");
        }

        if (!ObjectId.TryParse(targetReservationId, out var objectId))
            throw new DomainException(ErrorCodes.QrInvalid, "Reservation ID encoded in QR is invalid.");

        var reservation = await _reservationRepository.GetByIdAsync(objectId, ct)
            ?? throw new DomainException(ErrorCodes.QrInvalid, "Reservation does not exist.");

        // BR-11: Operator assigned node check
        if (operatorNodeIds == null || !operatorNodeIds.Contains(reservation.NodeId.ToString()))
            throw new DomainException(ErrorCodes.NotAssignedToNode, "You are not assigned to this station node.");

        // Check reservation status
        if (reservation.Status is ReservationStatus.InProgress or ReservationStatus.Completed)
            throw new DomainException(ErrorCodes.QrUsed, "QR code has already been used for this transfer.");

        if (reservation.Status != ReservationStatus.Approved)
            throw new DomainException(ErrorCodes.QrInvalid, $"Reservation is in {reservation.Status} state, not Approved.");

        // Check QR version
        if (reservation.Qr == null || reservation.Qr.Version != targetQrVersion)
            throw new DomainException(ErrorCodes.QrInvalid, "QR version is outdated or invalid.");

        // Time window checks (BR-12)
        var now = DateTime.UtcNow;
        var earlyWindowStart = reservation.SlotStartUtc.AddMinutes(-30);
        if (now < earlyWindowStart)
            throw new DomainException(ErrorCodes.QrNotYetValid, "QR code is not yet valid. Transfers open 30 minutes before slot start.");

        if (now > reservation.SlotEndUtc)
            throw new DomainException(ErrorCodes.QrExpired, "QR code has expired. Slot has already ended.");

        // Start transfer
        reservation.StartTransfer(operatorSub, now);
        await _reservationRepository.SaveAsync(reservation, ct);

        return MapToResponse(reservation);
    }

    public async Task<ReservationResponse> FinalizeAsync(string reservationId, FinalizeTransferRequest request, string operatorSub, List<string>? operatorNodeIds, CancellationToken ct = default)
    {
        var reservation = await FindReservationAsync(reservationId, ct);

        // BR-11: Operator node check
        if (operatorNodeIds == null || !operatorNodeIds.Contains(reservation.NodeId.ToString()))
            throw new DomainException(ErrorCodes.NotAssignedToNode, "You are not assigned to this station node.");

        if (reservation.Status != ReservationStatus.InProgress)
            throw new DomainException(ErrorCodes.InvalidState, "Only InProgress reservations can be finalized.");

        var actualKwh = request.MeterEndKwh - request.MeterStartKwh;
        if (actualKwh <= 0)
            throw new DomainException(ErrorCodes.ValidationFailed, "Meter end reading must exceed start reading (positive transferred energy).");

        // BR-13: Value = actual kWh * unit price, rounded to 2 decimals
        var totalValue = Math.Round(actualKwh * reservation.UnitPrice, 2, MidpointRounding.AwayFromZero);

        var transferRecord = new TransferRecord(
            request.MeterStartKwh,
            request.MeterEndKwh,
            actualKwh,
            totalValue,
            operatorSub,
            DateTime.UtcNow);

        reservation.Complete(transferRecord, operatorSub, DateTime.UtcNow);
        await _reservationRepository.SaveAsync(reservation, ct);

        return MapToResponse(reservation);
    }

    public async Task<List<TransferRecordDto>> ListTransfersAsync(string? nodeId, DateTime? fromUtc, DateTime? toUtc, string callerRole, List<string>? operatorNodeIds, int limit = 50, CancellationToken ct = default)
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
        if (!string.IsNullOrEmpty(nodeId) && ObjectId.TryParse(nodeId, out var parsedNode))
            filterNode = parsedNode;

        var completedReservations = await _reservationRepository.SearchAsync(
            status: ReservationStatus.Completed.ToString(),
            nodeId: filterNode,
            allowedNodeIds: allowedNodes,
            fromUtc: fromUtc,
            toUtc: toUtc,
            limit: limit,
            ct: ct);

        return completedReservations
            .Where(r => r.Transaction != null)
            .Select(r => new TransferRecordDto
            {
                ReservationId = r.Id.ToString(),
                ReservationNo = r.ReservationNo,
                ProsumerNic = r.ProsumerNic,
                NodeId = r.NodeId.ToString(),
                NodeName = r.NodeName,
                TradeType = r.TradeType.ToString(),
                MeterStartKwh = r.Transaction!.MeterStartKwh,
                MeterEndKwh = r.Transaction.MeterEndKwh,
                ActualKwh = r.Transaction.ActualKwh,
                UnitPrice = r.UnitPrice,
                Value = r.Transaction.Value,
                FinalizedBy = r.Transaction.FinalizedBy,
                FinalizedAtUtc = r.Transaction.FinalizedAtUtc
            })
            .ToList();
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
