using Microsoft.AspNetCore.Http;

namespace SmartSolarMicrogrid.API.Common.Errors;

public static class ErrorCatalog
{
    private static readonly Dictionary<string, (int Status, string Title)> Entries = new()
    {
        [ErrorCodes.ValidationFailed] = (400, "Validation failed"),
        [ErrorCodes.InvalidCredentials] = (401, "Sign-in failed"),
        [ErrorCodes.Unauthorized] = (401, "Sign-in required"),
        [ErrorCodes.AccountDeactivated] = (403, "Account deactivated"),
        [ErrorCodes.Forbidden] = (403, "Not allowed"),
        [ErrorCodes.NotOwner] = (403, "Not your record"),
        [ErrorCodes.NotAssignedToNode] = (403, "Not assigned to this node"),
        [ErrorCodes.NotFound] = (404, "Not found"),
        [ErrorCodes.NicExists] = (409, "NIC already registered"),
        [ErrorCodes.EmailExists] = (409, "Email already in use"),
        [ErrorCodes.SlotFull] = (409, "Slot is full"),
        [ErrorCodes.DuplicateBooking] = (409, "Already booked in this slot"),
        [ErrorCodes.SlotUnavailable] = (409, "Slot not available"),
        [ErrorCodes.SlotInUse] = (409, "Slot has bookings"),
        [ErrorCodes.NodeHasActiveReservations] = (409, "Node has active bookings"),
        [ErrorCodes.HasActiveReservations] = (409, "Account has active bookings"),
        [ErrorCodes.ConcurrentUpdate] = (409, "Changed by someone else; reload and retry"),
        [ErrorCodes.InvalidState] = (409, "Not allowed in the current status"),
        [ErrorCodes.QrInvalid] = (409, "QR code not valid"),
        [ErrorCodes.QrUsed] = (409, "QR code already used"),
        [ErrorCodes.QrExpired] = (409, "QR code expired"),
        [ErrorCodes.QrNotYetValid] = (409, "QR code not valid yet"),
        [ErrorCodes.BackupCodeLocked] = (409, "Too many backup-code attempts"),
        [ErrorCodes.BookingWindow] = (422, "Outside the booking window"),
        [ErrorCodes.ChangeCutoff] = (422, "Change window closed"),
        [ErrorCodes.RateLimited] = (429, "Too many attempts"),
        [ErrorCodes.InternalError] = (500, "Unexpected error")
    };

    public static (int Status, string Title) Get(string code) =>
        Entries.TryGetValue(code, out var entry) ? entry : (500, "Unexpected error");

    public static void AddCode(ProblemDetailsContext context) =>
        context.ProblemDetails.Extensions.TryAdd("code", context.ProblemDetails.Status switch
        {
            400 => ErrorCodes.ValidationFailed,
            401 => ErrorCodes.Unauthorized,
            403 => ErrorCodes.Forbidden,
            404 => ErrorCodes.NotFound,
            429 => ErrorCodes.RateLimited,
            _ => ErrorCodes.InternalError
        });
}
