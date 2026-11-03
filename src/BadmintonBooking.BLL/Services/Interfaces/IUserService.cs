using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Users;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IUserService
{
    IQueryable<UserDto> GetAllQuery();
    Task<ApiResponse<UserDto>> GetByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateAsync(CreateUserDto dto);
    Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}

