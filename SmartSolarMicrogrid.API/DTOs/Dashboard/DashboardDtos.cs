namespace SmartSolarMicrogrid.API.DTOs.Dashboard;

public class OperatorDashboardDto
{
    public List<string> AssignedNodeIds { get; set; } = new();
    public int ActiveSlotsToday { get; set; }
    public int PendingApprovalsCount { get; set; }
    public int InProgressTransfersCount { get; set; }
    public int CompletedTodayCount { get; set; }
    public decimal TodayEnergyTransferredKwh { get; set; }
}

public class BackofficeDashboardDto
{
    public int TotalNodesCount { get; set; }
    public int ActiveNodesCount { get; set; }
    public int TotalProsumersCount { get; set; }
    public int ActiveProsumersCount { get; set; }
    public int ActiveReservationsCount { get; set; }
    public decimal TotalEnergyTradedKwh { get; set; }
    public decimal TotalFinancialTurnover { get; set; }
}
