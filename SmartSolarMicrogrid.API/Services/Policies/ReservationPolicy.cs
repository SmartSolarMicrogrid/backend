using SmartSolarMicrogrid.API.Common.Errors;

namespace SmartSolarMicrogrid.API.Services.Policies;

public class ReservationPolicy
{
    public int MaxAdvanceDays { get; set; } = 7;
    public int ChangeCutoffHours { get; set; } = 12;
    public decimal MinKwh { get; set; } = 0.5m;

    // BR-01: Slot starts after now and no more than MaxAdvanceDays ahead
    public (bool Success, string? ErrorCode) CanBook(DateTime slotStartUtc)
    {
        var now = DateTime.UtcNow;
        if (slotStartUtc <= now || slotStartUtc > now.AddDays(MaxAdvanceDays))
            return (false, ErrorCodes.BookingWindow);

        return (true, null);
    }

    // BR-02 & BR-03: Changes and cancellations close ChangeCutoffHours before slot starts
    public (bool Success, string? ErrorCode) CanChange(DateTime slotStartUtc)
    {
        var now = DateTime.UtcNow;
        if (slotStartUtc - now < TimeSpan.FromHours(ChangeCutoffHours))
            return (false, ErrorCodes.ChangeCutoff);

        return (true, null);
    }

    // BR-05: Requested energy between MinKwh and node maximum
    public (bool Success, string? ErrorCode) CheckEnergy(decimal requestedKwh, decimal nodeMaxKwh)
    {
        if (requestedKwh < MinKwh || requestedKwh > nodeMaxKwh)
            return (false, ErrorCodes.ValidationFailed);

        return (true, null);
    }

    public DateTime ChangeDeadlineUtc(DateTime slotStartUtc) => slotStartUtc.AddHours(-ChangeCutoffHours);
}
