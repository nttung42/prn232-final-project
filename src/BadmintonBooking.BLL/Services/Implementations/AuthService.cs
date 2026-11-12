using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using BadmintonBooking.BLL.DTOs.Auth;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BadmintonBooking.BLL.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var emailLower = request.Email.Trim().ToLower();
        var existingUser = await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == emailLower);
        if (existingUser != null)
        {
            return ApiResponse<AuthResponseDto>.Fail("Email đã được sử dụng trong hệ thống.");
        }

        var user = _mapper.Map<User>(request);
        user.Email = emailLower;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.IsActive = true;
        user.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        var response = new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Đăng ký tài khoản thành công.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var emailLower = request.Email.Trim().ToLower();
        var user = await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == emailLower);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponseDto>.Fail("Email hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<AuthResponseDto>.Fail("Tài khoản của bạn đã bị khóa hoặc chưa kích hoạt.");
        }

        var token = GenerateJwtToken(user);

        var response = new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Đăng nhập thành công.");
    }

    public async Task<ApiResponse<AuthResponseDto>> GetCurrentUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<AuthResponseDto>.Fail("Người dùng không tồn tại.");
        }

        var token = GenerateJwtToken(user);
        var response = new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };

        return ApiResponse<AuthResponseDto>.Ok(response);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key (biến môi trường Jwt__Key).");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "BadmintonBookingAPI";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "BadmintonBookingClients";
        var expiryHours = int.TryParse(_configuration["Jwt:ExpiryInHours"], out var h) ? h : 24;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

