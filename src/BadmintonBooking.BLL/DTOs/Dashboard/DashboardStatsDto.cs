namespace BadmintonBooking.BLL.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalPlayers { get; set; }
    public int TotalProviders { get; set; }
    public int TotalFacilities { get; set; }
    public int ActiveFacilities { get; set; }
    public int TotalCourts { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
}

