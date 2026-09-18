namespace SmartSolarMicrogrid.API.Common.Errors;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string AccountDeactivated = "ACCOUNT_DEACTIVATED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotOwner = "NOT_OWNER";
    public const string NotAssignedToNode = "NOT_ASSIGNED_TO_NODE";
    public const string NotFound = "NOT_FOUND";
    public const string NicExists = "NIC_EXISTS";
    public const string EmailExists = "EMAIL_EXISTS";
    public const string SlotFull = "SLOT_FULL";
    public const string DuplicateBooking = "DUPLICATE_BOOKING";
    public const string SlotUnavailable = "SLOT_UNAVAILABLE";
    public const string SlotInUse = "SLOT_IN_USE";
    public const string NodeHasActiveReservations = "NODE_HAS_ACTIVE_RESERVATIONS";
    public const string HasActiveReservations = "HAS_ACTIVE_RESERVATIONS";
    public const string ConcurrentUpdate = "CONCURRENT_UPDATE";
    public const string InvalidState = "INVALID_STATE";
    public const string QrInvalid = "QR_INVALID";
    public const string QrUsed = "QR_USED";
    public const string QrExpired = "QR_EXPIRED";
    public const string QrNotYetValid = "QR_NOT_YET_VALID";
    public const string BackupCodeLocked = "BACKUP_CODE_LOCKED";
    public const string BookingWindow = "BOOKING_WINDOW";
    public const string ChangeCutoff = "CHANGE_CUTOFF";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
}
