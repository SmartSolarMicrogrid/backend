using SmartSolarMicrogrid.API.DTOs.Reservations;

namespace SmartSolarMicrogrid.API.DTOs.Dashboard;

public class ProsumerDashboardDto
{
    public string Nic { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ActiveBookingsCount { get; set; }
    public int CompletedTransfersCount { get; set; }
    public decimal TotalEnergyExportedKwh { get; set; }
    public decimal TotalEnergyImportedKwh { get; set; }
    public decimal NetEarnings { get; set; }
    public List<ReservationResponse> UpcomingBookings { get; set; } = new();
    public List<ReservationResponse> RecentTransfers { get; set; } = new();
}
