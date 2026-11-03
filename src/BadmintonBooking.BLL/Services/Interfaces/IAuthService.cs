using BadmintonBooking.BLL.DTOs.Auth;
using BadmintonBooking.BLL.DTOs.Common;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<AuthResponseDto>> GetCurrentUserAsync(int userId);
}

