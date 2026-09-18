namespace SmartSolarMicrogrid.API.Utilities;

/// <summary>
/// Centralised business rule constants for the reservation module.
/// </summary>
public static class BusinessRuleConstants
{
    /// <summary>Reservations can only be made up to this many days in advance.</summary>
    public const int MaxAdvanceBookingDays = 7;

    /// <summary>Reservations cannot be cancelled within this many hours of the slot start time.</summary>
    public const int MinCancellationHours = 12;
}
