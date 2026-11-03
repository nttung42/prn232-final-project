using AutoMapper;
using AutoMapper.QueryableExtensions;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Courts;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class CourtService : ICourtService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CourtService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public IQueryable<CourtDto> GetAllQuery(int? facilityId = null)
    {
        var query = _unitOfWork.Courts.Query()
            .Include(c => c.Facility)
            .AsNoTracking();

        if (facilityId.HasValue)
        {
            query = query.Where(c => c.FacilityId == facilityId.Value);
        }

        return query.ProjectTo<CourtDto>(_mapper.ConfigurationProvider);
    }

    public async Task<ApiResponse<CourtDto>> GetByIdAsync(int id)
    {
        var court = await _unitOfWork.Courts.Query()
            .Include(c => c.Facility)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (court == null)
        {
            return ApiResponse<CourtDto>.Fail("Không tìm thấy sân cầu lông.");
        }

        return ApiResponse<CourtDto>.Ok(_mapper.Map<CourtDto>(court));
    }

    public async Task<ApiResponse<CourtDto>> CreateAsync(int currentUserId, string role, SaveCourtDto dto)
    {
        var facility = await _unitOfWork.Facilities.GetByIdAsync(dto.FacilityId);
        if (facility == null)
        {
            return ApiResponse<CourtDto>.Fail("Không tìm thấy cơ sở chỉ định.");
        }

        if (role != AppRoles.Admin && facility.ProviderId != currentUserId)
        {
            return ApiResponse<CourtDto>.Fail("Bạn không có quyền thêm sân vào cơ sở này.");
        }

        var court = _mapper.Map<Court>(dto);
        await _unitOfWork.Courts.AddAsync(court);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CourtDto>.Ok(_mapper.Map<CourtDto>(court), "Thêm sân cầu lông thành công.");
    }

    public async Task<ApiResponse<CourtDto>> UpdateAsync(int id, int currentUserId, string role, SaveCourtDto dto)
    {
        var court = await _unitOfWork.Courts.Query()
            .Include(c => c.Facility)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (court == null)
        {
            return ApiResponse<CourtDto>.Fail("Không tìm thấy sân cầu lông.");
        }

        if (role != AppRoles.Admin && court.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<CourtDto>.Fail("Bạn không có quyền chỉnh sửa sân này.");
        }

        _mapper.Map(dto, court);
        _unitOfWork.Courts.Update(court);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CourtDto>.Ok(_mapper.Map<CourtDto>(court), "Cập nhật sân cầu lông thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role)
    {
        var court = await _unitOfWork.Courts.Query()
            .Include(c => c.Facility)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (court == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy sân cầu lông.");
        }

        if (role != AppRoles.Admin && court.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa sân này.");
        }

        var hasActiveBookings = await _unitOfWork.Bookings.Query()
            .AnyAsync(b => b.CourtId == id && 
                          (b.Status == BookingStatuses.Pending || b.Status == BookingStatuses.Confirmed));

        if (hasActiveBookings)
        {
            return ApiResponse<bool>.Fail("Sân đang có các đơn đặt chờ duyệt hoặc đã xác nhận, không thể xóa.");
        }

        _unitOfWork.Courts.Delete(court);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa sân cầu lông thành công.");
    }
}

