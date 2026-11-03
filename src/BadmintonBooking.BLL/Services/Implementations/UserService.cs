using AutoMapper;
using AutoMapper.QueryableExtensions;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Users;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public IQueryable<UserDto> GetAllQuery()
    {
        return _unitOfWork.Users.Query()
            .AsNoTracking()
            .ProjectTo<UserDto>(_mapper.ConfigurationProvider);
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng.");
        }

        return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(user));
    }

    public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var emailLower = dto.Email.Trim().ToLower();
        var exists = await _unitOfWork.Users.ExistsAsync(u => u.Email.ToLower() == emailLower);
        if (exists)
        {
            return ApiResponse<UserDto>.Fail("Email đã tồn tại.");
        }

        var user = _mapper.Map<User>(dto);
        user.Email = emailLower;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(user), "Tạo người dùng thành công.");
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse<UserDto>.Fail("Không tìm thấy người dùng.");
        }

        _mapper.Map(dto, user);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(user), "Cập nhật người dùng thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy người dùng.");
        }

        // Check if user has associated facilities or bookings
        var hasFacilities = await _unitOfWork.Facilities.ExistsAsync(f => f.ProviderId == id);
        var hasBookings = await _unitOfWork.Bookings.ExistsAsync(b => b.PlayerId == id);

        if (hasFacilities || hasBookings)
        {
            // Soft delete/deactivate if data exists
            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Người dùng đã phát sinh dữ liệu, đã chuyển sang trạng thái Vô hiệu hóa (IsActive = false).");
        }

        _unitOfWork.Users.Delete(user);
        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Đã xóa người dùng thành công.");
    }
}

