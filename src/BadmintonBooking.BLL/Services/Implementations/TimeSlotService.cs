using AutoMapper;
using AutoMapper.QueryableExtensions;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.TimeSlots;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class TimeSlotService : ITimeSlotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TimeSlotService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public IQueryable<TimeSlotDto> GetAllQuery(int? facilityId = null)
    {
        var query = _unitOfWork.TimeSlots.Query()
            .Include(ts => ts.Facility)
            .AsNoTracking();

        if (facilityId.HasValue)
        {
            query = query.Where(ts => ts.FacilityId == facilityId.Value);
        }

        return query.ProjectTo<TimeSlotDto>(_mapper.ConfigurationProvider);
    }

    public async Task<ApiResponse<TimeSlotDto>> GetByIdAsync(int id)
    {
        var slot = await _unitOfWork.TimeSlots.Query()
            .Include(ts => ts.Facility)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (slot == null)
        {
            return ApiResponse<TimeSlotDto>.Fail("Không tìm thấy khung giờ.");
        }

        return ApiResponse<TimeSlotDto>.Ok(_mapper.Map<TimeSlotDto>(slot));
    }

    public async Task<ApiResponse<List<AvailableTimeSlotDto>>> GetAvailableSlotsAsync(int facilityId, int courtId, DateTime playDate)
    {
        var court = await _unitOfWork.Courts.GetByIdAsync(courtId);
        if (court == null || court.FacilityId != facilityId)
        {
            return ApiResponse<List<AvailableTimeSlotDto>>.Fail("Sân không tồn tại hoặc không thuộc cơ sở này.");
        }

        // Active slots for this facility
        var slots = await _unitOfWork.TimeSlots.Query()
            .Where(ts => ts.FacilityId == facilityId && ts.IsActive)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();

        // Existing bookings for this court and playDate that are Pending or Confirmed
        var targetDate = playDate.Date;
        var bookings = await _unitOfWork.Bookings.Query()
            .Where(b => b.CourtId == courtId && 
                        b.PlayDate.Date == targetDate && 
                        (b.Status == BookingStatuses.Pending || b.Status == BookingStatuses.Confirmed))
            .ToListAsync();

        var bookedSlotDict = bookings.ToDictionary(b => b.TimeSlotId, b => b.Id);

        var result = slots.Select(s => new AvailableTimeSlotDto
        {
            Id = s.Id,
            FacilityId = s.FacilityId,
            SlotName = s.SlotName,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Price = s.Price,
            IsAvailable = !bookedSlotDict.ContainsKey(s.Id),
            ExistingBookingId = bookedSlotDict.TryGetValue(s.Id, out var bId) ? bId : null
        }).ToList();

        return ApiResponse<List<AvailableTimeSlotDto>>.Ok(result);
    }

    public async Task<ApiResponse<TimeSlotDto>> CreateAsync(int currentUserId, string role, SaveTimeSlotDto dto)
    {
        var facility = await _unitOfWork.Facilities.GetByIdAsync(dto.FacilityId);
        if (facility == null)
        {
            return ApiResponse<TimeSlotDto>.Fail("Không tìm thấy cơ sở chỉ định.");
        }

        if (role != AppRoles.Admin && facility.ProviderId != currentUserId)
        {
            return ApiResponse<TimeSlotDto>.Fail("Bạn không có quyền thêm khung giờ vào cơ sở này.");
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return ApiResponse<TimeSlotDto>.Fail("Giờ kết thúc ca chơi phải lớn hơn giờ bắt đầu.");
        }

        var slot = _mapper.Map<TimeSlot>(dto);
        await _unitOfWork.TimeSlots.AddAsync(slot);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<TimeSlotDto>.Ok(_mapper.Map<TimeSlotDto>(slot), "Tạo khung giờ & bảng giá thành công.");
    }

    public async Task<ApiResponse<TimeSlotDto>> UpdateAsync(int id, int currentUserId, string role, SaveTimeSlotDto dto)
    {
        var slot = await _unitOfWork.TimeSlots.Query()
            .Include(ts => ts.Facility)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (slot == null)
        {
            return ApiResponse<TimeSlotDto>.Fail("Không tìm thấy khung giờ.");
        }

        if (role != AppRoles.Admin && slot.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<TimeSlotDto>.Fail("Bạn không có quyền chỉnh sửa khung giờ này.");
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return ApiResponse<TimeSlotDto>.Fail("Giờ kết thúc ca chơi phải lớn hơn giờ bắt đầu.");
        }

        _mapper.Map(dto, slot);
        _unitOfWork.TimeSlots.Update(slot);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<TimeSlotDto>.Ok(_mapper.Map<TimeSlotDto>(slot), "Cập nhật khung giờ thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role)
    {
        var slot = await _unitOfWork.TimeSlots.Query()
            .Include(ts => ts.Facility)
            .FirstOrDefaultAsync(ts => ts.Id == id);

        if (slot == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy khung giờ.");
        }

        if (role != AppRoles.Admin && slot.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa khung giờ này.");
        }

        var hasActiveBookings = await _unitOfWork.Bookings.Query()
            .AnyAsync(b => b.TimeSlotId == id && 
                          (b.Status == BookingStatuses.Pending || b.Status == BookingStatuses.Confirmed));

        if (hasActiveBookings)
        {
            return ApiResponse<bool>.Fail("Khung giờ đang có đơn đặt chờ duyệt hoặc đã xác nhận, không thể xóa.");
        }

        _unitOfWork.TimeSlots.Delete(slot);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa khung giờ thành công.");
    }
}

