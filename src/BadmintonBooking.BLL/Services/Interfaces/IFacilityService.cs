using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Facilities;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IFacilityService
{
    IQueryable<FacilityDto> GetAllQuery(bool onlyApproved = false);
    Task<ApiResponse<FacilityDto>> GetByIdAsync(int id);
    Task<ApiResponse<FacilityDto>> CreateAsync(int providerId, CreateFacilityDto dto);
    Task<ApiResponse<FacilityDto>> UpdateAsync(int id, int currentUserId, string role, UpdateFacilityDto dto);
    Task<ApiResponse<FacilityDto>> UpdateStatusAsync(int id, UpdateFacilityStatusDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role);
}

