using BadmintonBooking.BLL.DTOs.Auth;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng ký tài khoản mới (Player hoặc Provider)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Dữ liệu không hợp lệ", 
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Đăng ký không đăng nhập ngay: không trả token ra body
        if (result.Data != null) result.Data.AccessToken = string.Empty;
        return Ok(result);
    }

    /// <summary>
    /// Đăng nhập hệ thống, nhận JWT Bearer Token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")] // Chống dò mật khẩu: giới hạn theo IP
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Dữ liệu không hợp lệ", 
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Success || result.Data == null)
        {
            return BadRequest(result);
        }

        // Token chỉ đi qua cookie HttpOnly: JavaScript không đọc được, không đưa vào body
        Response.Cookies.Append(AuthCookieName, result.Data.AccessToken, BuildAuthCookieOptions(TimeSpan.FromHours(24)));
        result.Data.AccessToken = string.Empty;

        return Ok(result);
    }

    /// <summary>
    /// Đăng xuất: xóa cookie xác thực
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName, BuildAuthCookieOptions(null));
        return Ok(new { success = true, message = "Đã đăng xuất." });
    }

    public const string AuthCookieName = "courtside_token";

    private CookieOptions BuildAuthCookieOptions(TimeSpan? lifetime)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            // Luôn Secure: trình duyệt coi http://localhost là ngữ cảnh an toàn nên vẫn gửi cookie khi phát triển
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = lifetime.HasValue ? DateTimeOffset.UtcNow.Add(lifetime.Value) : null
        };
    }

    /// <summary>
    /// Lấy thông tin tài khoản hiện tại từ JWT Claims
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var result = await _authService.GetCurrentUserAsync(userId);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

