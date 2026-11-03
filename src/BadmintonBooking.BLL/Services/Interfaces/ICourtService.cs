using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Courts;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface ICourtService
{
    IQueryable<CourtDto> GetAllQuery(int? facilityId = null);
    Task<ApiResponse<CourtDto>> GetByIdAsync(int id);
    Task<ApiResponse<CourtDto>> CreateAsync(int currentUserId, string role, SaveCourtDto dto);
    Task<ApiResponse<CourtDto>> UpdateAsync(int id, int currentUserId, string role, SaveCourtDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role);
}

