using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Dashboard;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetSystemStatsAsync()
    {
        var totalUsers = await _unitOfWork.Users.CountAsync();
        var totalPlayers = await _unitOfWork.Users.CountAsync(u => u.Role == AppRoles.Player);
        var totalProviders = await _unitOfWork.Users.CountAsync(u => u.Role == AppRoles.Provider);

        var totalFacilities = await _unitOfWork.Facilities.CountAsync();
        var activeFacilities = await _unitOfWork.Facilities.CountAsync(f => f.Status == FacilityStatuses.Approved);
        var totalCourts = await _unitOfWork.Courts.CountAsync();

        var bookings = await _unitOfWork.Bookings.Query().ToListAsync();
        var totalBookings = bookings.Count;
        var pendingBookings = bookings.Count(b => b.Status == BookingStatuses.Pending);
        var confirmedBookings = bookings.Count(b => b.Status == BookingStatuses.Confirmed);
        var completedBookings = bookings.Count(b => b.Status == BookingStatuses.Completed);
        var cancelledBookings = bookings.Count(b => b.Status == BookingStatuses.Cancelled);

        var totalRevenue = bookings
            .Where(b => b.Status == BookingStatuses.Completed || b.Status == BookingStatuses.Confirmed)
            .Sum(b => b.TotalPrice);

        var stats = new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            TotalPlayers = totalPlayers,
            TotalProviders = totalProviders,
            TotalFacilities = totalFacilities,
            ActiveFacilities = activeFacilities,
            TotalCourts = totalCourts,
            TotalBookings = totalBookings,
            PendingBookings = pendingBookings,
            ConfirmedBookings = confirmedBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings,
            TotalRevenue = totalRevenue
        };

        return ApiResponse<DashboardStatsDto>.Ok(stats);
    }
}

