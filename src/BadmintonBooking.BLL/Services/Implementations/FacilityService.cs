using AutoMapper;
using AutoMapper.QueryableExtensions;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Facilities;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class FacilityService : IFacilityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FacilityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public IQueryable<FacilityDto> GetAllQuery(bool onlyApproved = false)
    {
        var query = _unitOfWork.Facilities.Query()
            .Include(f => f.Provider)
            .Include(f => f.Courts)
            .Include(f => f.TimeSlots)
            .AsNoTracking();

        if (onlyApproved)
        {
            query = query.Where(f => f.Status == FacilityStatuses.Approved);
        }

        return query.ProjectTo<FacilityDto>(_mapper.ConfigurationProvider);
    }

    public async Task<ApiResponse<FacilityDto>> GetByIdAsync(int id)
    {
        var facility = await _unitOfWork.Facilities.Query()
            .Include(f => f.Provider)
            .Include(f => f.Courts)
            .Include(f => f.TimeSlots)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (facility == null)
        {
            return ApiResponse<FacilityDto>.Fail("Không tìm thấy cơ sở cầu lông.");
        }

        return ApiResponse<FacilityDto>.Ok(_mapper.Map<FacilityDto>(facility));
    }

    public async Task<ApiResponse<FacilityDto>> CreateAsync(int providerId, CreateFacilityDto dto)
    {
        var provider = await _unitOfWork.Users.GetByIdAsync(providerId);
        if (provider == null || (provider.Role != AppRoles.Provider && provider.Role != AppRoles.Admin))
        {
            return ApiResponse<FacilityDto>.Fail("Chỉ tài khoản Chủ sân (Provider) mới có quyền tạo cơ sở.");
        }

        if (dto.CloseTime <= dto.OpenTime)
        {
            return ApiResponse<FacilityDto>.Fail("Giờ đóng cửa phải lớn hơn giờ mở cửa.");
        }

        var facility = _mapper.Map<Facility>(dto);
        facility.ProviderId = providerId;
        facility.Status = FacilityStatuses.Pending;
        facility.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Facilities.AddAsync(facility);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<FacilityDto>.Ok(_mapper.Map<FacilityDto>(facility), "Đăng ký cơ sở thành công, đang chờ Admin phê duyệt.");
    }

    public async Task<ApiResponse<FacilityDto>> UpdateAsync(int id, int currentUserId, string role, UpdateFacilityDto dto)
    {
        var facility = await _unitOfWork.Facilities.GetByIdAsync(id);
        if (facility == null)
        {
            return ApiResponse<FacilityDto>.Fail("Không tìm thấy cơ sở cầu lông.");
        }

        if (role != AppRoles.Admin && facility.ProviderId != currentUserId)
        {
            return ApiResponse<FacilityDto>.Fail("Bạn không có quyền chỉnh sửa cơ sở này.");
        }

        if (dto.CloseTime <= dto.OpenTime)
        {
            return ApiResponse<FacilityDto>.Fail("Giờ đóng cửa phải lớn hơn giờ mở cửa.");
        }

        _mapper.Map(dto, facility);
        _unitOfWork.Facilities.Update(facility);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<FacilityDto>.Ok(_mapper.Map<FacilityDto>(facility), "Cập nhật cơ sở thành công.");
    }

    public async Task<ApiResponse<FacilityDto>> UpdateStatusAsync(int id, UpdateFacilityStatusDto dto)
    {
        var facility = await _unitOfWork.Facilities.GetByIdAsync(id);
        if (facility == null)
        {
            return ApiResponse<FacilityDto>.Fail("Không tìm thấy cơ sở cầu lông.");
        }

        facility.Status = dto.Status;
        _unitOfWork.Facilities.Update(facility);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<FacilityDto>.Ok(_mapper.Map<FacilityDto>(facility), $"Cập nhật trạng thái cơ sở thành công sang {dto.Status}.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role)
    {
        var facility = await _unitOfWork.Facilities.GetByIdAsync(id);
        if (facility == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy cơ sở cầu lông.");
        }

        if (role != AppRoles.Admin && facility.ProviderId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa cơ sở này.");
        }

        // Check if there are active bookings
        var hasActiveBookings = await _unitOfWork.Bookings.Query()
            .Include(b => b.Court)
            .AnyAsync(b => b.Court != null && b.Court.FacilityId == id && 
                          (b.Status == BookingStatuses.Pending || b.Status == BookingStatuses.Confirmed));

        if (hasActiveBookings)
        {
            return ApiResponse<bool>.Fail("Cơ sở đang có các đơn đặt sân chưa hoàn tất, không thể xóa.");
        }

        _unitOfWork.Facilities.Delete(facility);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa cơ sở thành công.");
    }
}

