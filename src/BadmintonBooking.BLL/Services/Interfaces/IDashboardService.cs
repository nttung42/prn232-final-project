using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Dashboard;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetSystemStatsAsync();
}

