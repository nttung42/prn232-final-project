using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.TimeSlots;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface ITimeSlotService
{
    IQueryable<TimeSlotDto> GetAllQuery(int? facilityId = null, int? courtId = null);
    Task<ApiResponse<TimeSlotDto>> GetByIdAsync(int id);
    Task<ApiResponse<List<AvailableTimeSlotDto>>> GetAvailableSlotsAsync(int facilityId, int courtId, DateTime playDate);
    Task<ApiResponse<TimeSlotDto>> CreateAsync(int currentUserId, string role, SaveTimeSlotDto dto);
    Task<ApiResponse<TimeSlotDto>> UpdateAsync(int id, int currentUserId, string role, SaveTimeSlotDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role);
}

