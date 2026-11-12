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

    public IQueryable<TimeSlotDto> GetAllQuery(int? facilityId = null, int? courtId = null)
    {
        var query = _unitOfWork.TimeSlots.Query()
            .Include(ts => ts.Facility)
            .AsNoTracking();

        if (facilityId.HasValue)
        {
            query = query.Where(ts => ts.FacilityId == facilityId.Value);
        }
        if (courtId.HasValue)
        {
            query = query.Where(ts => ts.CourtId == courtId.Value);
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

    /// <summary>
    /// Trạng thái từng khung giờ của một sân trong một ngày:
    /// Available (trống), Pending (đang chờ duyệt / đã trừ ví chờ duyệt), Booked (đã xác nhận).
    /// Đơn đã hoàn thành/hủy không chiếm chỗ.
    /// </summary>
    public async Task<ApiResponse<List<AvailableTimeSlotDto>>> GetAvailableSlotsAsync(int facilityId, int courtId, DateTime playDate)
    {
        var court = await _unitOfWork.Courts.GetByIdAsync(courtId);
        if (court == null || court.FacilityId != facilityId)
        {
            return ApiResponse<List<AvailableTimeSlotDto>>.Fail("Sân không tồn tại hoặc không thuộc cơ sở này.");
        }

        var slots = await _unitOfWork.TimeSlots.Query()
            .Where(ts => ts.CourtId == courtId && ts.IsActive)
            .OrderBy(ts => ts.StartTime)
            .ToListAsync();

        var targetDate = playDate.Date;
        var bookings = await _unitOfWork.Bookings.Query()
            .Where(b => b.CourtId == courtId &&
                        b.PlayDate.Date == targetDate &&
                        (b.Status == BookingStatuses.Pending || b.Status == BookingStatuses.Confirmed))
            .ToListAsync();

        var bookingBySlot = bookings.GroupBy(b => b.TimeSlotId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.Status == BookingStatuses.Confirmed).First());

        var result = slots.Select(s =>
        {
            var hasBooking = bookingBySlot.TryGetValue(s.Id, out var booking);
            var status = !hasBooking ? "Available"
                : booking!.Status == BookingStatuses.Confirmed ? "Booked" : "Pending";
            return new AvailableTimeSlotDto
            {
                Id = s.Id,
                FacilityId = s.FacilityId,
                CourtId = s.CourtId,
                SlotName = s.SlotName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Price = s.Price,
                IsAvailable = status == "Available",
                Status = status,
                ExistingBookingId = hasBooking ? booking!.Id : null
            };
        }).ToList();

        return ApiResponse<List<AvailableTimeSlotDto>>.Ok(result);
    }

    public async Task<ApiResponse<TimeSlotDto>> CreateAsync(int currentUserId, string role, SaveTimeSlotDto dto)
    {
        var court = await _unitOfWork.Courts.Query()
            .Include(c => c.Facility)
            .FirstOrDefaultAsync(c => c.Id == dto.CourtId);
        if (court == null)
        {
            return ApiResponse<TimeSlotDto>.Fail("Không tìm thấy sân con chỉ định.");
        }

        if (role != AppRoles.Admin && court.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<TimeSlotDto>.Fail("Bạn không có quyền thêm khung giờ cho sân này.");
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return ApiResponse<TimeSlotDto>.Fail("Giờ kết thúc ca chơi phải lớn hơn giờ bắt đầu.");
        }

        var overlaps = await _unitOfWork.TimeSlots.Query().AnyAsync(ts =>
            ts.CourtId == court.Id && ts.IsActive && ts.StartTime < dto.EndTime && dto.StartTime < ts.EndTime);
        if (overlaps)
        {
            return ApiResponse<TimeSlotDto>.Fail("Khung giờ này bị trùng với một khung giờ đã có của sân này.");
        }

        var slot = _mapper.Map<TimeSlot>(dto);
        slot.CourtId = court.Id;
        slot.FacilityId = court.FacilityId;
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

        var court = await _unitOfWork.Courts.GetByIdAsync(dto.CourtId);
        if (court == null || court.FacilityId != slot.FacilityId)
        {
            return ApiResponse<TimeSlotDto>.Fail("Sân không tồn tại hoặc không thuộc cơ sở của khung giờ này.");
        }

        _mapper.Map(dto, slot);
        slot.CourtId = court.Id;
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
