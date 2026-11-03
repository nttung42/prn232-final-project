using BadmintonBooking.BLL.DTOs.Bookings;
using BadmintonBooking.BLL.DTOs.Common;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IBookingService
{
    IQueryable<BookingDto> GetAllQuery(int currentUserId, string role);
    Task<ApiResponse<BookingDto>> GetByIdAsync(int id, int currentUserId, string role);
    Task<ApiResponse<BookingDto>> CreateAsync(int playerId, CreateBookingDto dto);
    Task<ApiResponse<BookingDto>> UpdateStatusAsync(int id, int currentUserId, string role, UpdateBookingStatusDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role);
}

