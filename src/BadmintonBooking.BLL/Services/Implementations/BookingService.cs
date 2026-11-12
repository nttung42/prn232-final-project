using AutoMapper;
using AutoMapper.QueryableExtensions;
using BadmintonBooking.BLL.DTOs.Bookings;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IWalletService _walletService;

    public BookingService(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _walletService = walletService;
    }

    public IQueryable<BookingDto> GetAllQuery(int currentUserId, string role)
    {
        var query = _unitOfWork.Bookings.Query()
            .Include(b => b.Player)
            .Include(b => b.Court)
                .ThenInclude(c => c!.Facility)
            .Include(b => b.TimeSlot)
            .AsNoTracking();

        if (role == AppRoles.Player)
        {
            query = query.Where(b => b.PlayerId == currentUserId);
        }
        else if (role == AppRoles.Provider)
        {
            query = query.Where(b => b.Court != null && b.Court.Facility != null && b.Court.Facility.ProviderId == currentUserId);
        }
        else if (role != AppRoles.Admin)
        {
            // Vai trò không xác định: không trả về dữ liệu nào
            query = query.Where(b => false);
        }
        // Admin sees all

        return query.ProjectTo<BookingDto>(_mapper.ConfigurationProvider);
    }

    public async Task<ApiResponse<BookingDto>> GetByIdAsync(int id, int currentUserId, string role)
    {
        var booking = await _unitOfWork.Bookings.Query()
            .Include(b => b.Player)
            .Include(b => b.Court)
                .ThenInclude(c => c!.Facility)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return ApiResponse<BookingDto>.Fail("Không tìm thấy đơn đặt sân.");
        }

        // Check ownership
        if (role == AppRoles.Player && booking.PlayerId != currentUserId)
        {
            return ApiResponse<BookingDto>.Fail("Bạn không có quyền xem đơn đặt sân này.");
        }
        if (role == AppRoles.Provider && booking.Court?.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<BookingDto>.Fail("Đơn đặt sân này không thuộc cơ sở của bạn.");
        }

        return ApiResponse<BookingDto>.Ok(_mapper.Map<BookingDto>(booking));
    }

    public async Task<ApiResponse<BookingDto>> CreateAsync(int playerId, CreateBookingDto dto)
    {
        // 1. Validate PlayDate >= Today (BR-04)
        var targetDate = dto.PlayDate.Date;
        var today = DateTime.UtcNow.Date;
        if (targetDate < today)
        {
            return ApiResponse<BookingDto>.Fail("Ngày đặt chơi (PlayDate) phải từ ngày hôm nay trở đi.");
        }

        // 2. Validate Court
        var court = await _unitOfWork.Courts.Query()
            .Include(c => c.Facility)
            .FirstOrDefaultAsync(c => c.Id == dto.CourtId);

        if (court == null)
        {
            return ApiResponse<BookingDto>.Fail("Sân cầu lông không tồn tại.");
        }

        if (court.Status != CourtStatuses.Active)
        {
            return ApiResponse<BookingDto>.Fail("Sân cầu lông hiện đang bảo trì hoặc tạm ngưng.");
        }

        // 3. Validate Facility
        var facility = court.Facility;
        if (facility == null || facility.Status != FacilityStatuses.Approved)
        {
            return ApiResponse<BookingDto>.Fail("Cơ sở cầu lông chưa được phê duyệt hoặc đang tạm ngưng.");
        }

        // 4. Validate TimeSlot
        var slot = await _unitOfWork.TimeSlots.GetByIdAsync(dto.TimeSlotId);
        if (slot == null)
        {
            return ApiResponse<BookingDto>.Fail("Khung giờ không tồn tại.");
        }

        if (!slot.IsActive)
        {
            return ApiResponse<BookingDto>.Fail("Khung giờ hiện không khả dụng.");
        }

        if (slot.FacilityId != court.FacilityId)
        {
            return ApiResponse<BookingDto>.Fail("Khung giờ và sân cầu lông không thuộc cùng một cơ sở.");
        }

        // 5. Anti Double-Booking check (BR-03)
        var hasConflict = await _unitOfWork.Bookings.Query()
            .AnyAsync(b => b.CourtId == dto.CourtId &&
                           b.TimeSlotId == dto.TimeSlotId &&
                           b.PlayDate.Date == targetDate &&
                           (b.Status == BookingStatuses.Pending || b.Status == BookingStatuses.Confirmed));

        if (hasConflict)
        {
            return ApiResponse<BookingDto>.Fail("Khung giờ này của sân đã có người đặt trước (trạng thái Chờ duyệt hoặc Đã xác nhận).");
        }

        // 6. Create booking
        var booking = new Booking
        {
            PlayerId = playerId,
            CourtId = dto.CourtId,
            TimeSlotId = dto.TimeSlotId,
            PlayDate = targetDate,
            TotalPrice = slot.Price,
            Status = BookingStatuses.Pending,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        // Reload with navigation properties for DTO projection
        var createdBooking = await _unitOfWork.Bookings.Query()
            .Include(b => b.Player)
            .Include(b => b.Court)
                .ThenInclude(c => c!.Facility)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        return ApiResponse<BookingDto>.Ok(_mapper.Map<BookingDto>(createdBooking), "Đặt sân thành công, vui lòng chờ chủ sân xác nhận.");
    }

    public async Task<ApiResponse<BookingDto>> UpdateStatusAsync(int id, int currentUserId, string role, UpdateBookingStatusDto dto)
    {
        var booking = await _unitOfWork.Bookings.Query()
            .Include(b => b.Player)
            .Include(b => b.Court)
                .ThenInclude(c => c!.Facility)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return ApiResponse<BookingDto>.Fail("Không tìm thấy đơn đặt sân.");
        }

        // Check if already in final state (BR-05)
        if (booking.Status == BookingStatuses.Cancelled || booking.Status == BookingStatuses.Completed)
        {
            return ApiResponse<BookingDto>.Fail("Đơn đặt sân đã ở trạng thái kết thúc (Cancelled hoặc Completed), không thể thay đổi.");
        }

        var newStatus = dto.Status;
        var previousStatus = booking.Status;

        if (newStatus != BookingStatuses.Pending && newStatus != BookingStatuses.Confirmed
            && newStatus != BookingStatuses.Cancelled && newStatus != BookingStatuses.Completed)
        {
            return ApiResponse<BookingDto>.Fail("Trạng thái đơn không hợp lệ.");
        }

        if (role == AppRoles.Player)
        {
            if (booking.PlayerId != currentUserId)
            {
                return ApiResponse<BookingDto>.Fail("Bạn không có quyền thao tác trên đơn này.");
            }

            // Player can only cancel pending booking (BR-05)
            if (booking.Status == BookingStatuses.Pending && newStatus == BookingStatuses.Cancelled)
            {
                booking.Status = BookingStatuses.Cancelled;
            }
            else
            {
                return ApiResponse<BookingDto>.Fail("Người chơi chỉ được phép hủy đơn khi đơn đang ở trạng thái Chờ duyệt (Pending).");
            }
        }
        else if (role == AppRoles.Provider)
        {
            if (booking.Court?.Facility?.ProviderId != currentUserId)
            {
                return ApiResponse<BookingDto>.Fail("Đơn đặt sân này không thuộc cơ sở của bạn.");
            }

            // Provider transitions (BR-05)
            if (booking.Status == BookingStatuses.Pending)
            {
                if (newStatus == BookingStatuses.Confirmed || newStatus == BookingStatuses.Cancelled)
                {
                    booking.Status = newStatus;
                }
                else
                {
                    return ApiResponse<BookingDto>.Fail("Đơn chờ duyệt chỉ có thể chuyển sang Xác nhận (Confirmed) hoặc Từ chối (Cancelled).");
                }
            }
            else if (booking.Status == BookingStatuses.Confirmed)
            {
                if (newStatus == BookingStatuses.Completed || newStatus == BookingStatuses.Cancelled)
                {
                    booking.Status = newStatus;
                }
                else
                {
                    return ApiResponse<BookingDto>.Fail("Đơn đã xác nhận chỉ có thể chuyển sang Hoàn thành (Completed) hoặc Hủy (Cancelled).");
                }
            }
        }
        else if (role == AppRoles.Admin)
        {
            booking.Status = newStatus;
        }

        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            booking.Note = dto.Note;
        }

        // Đồng bộ yêu cầu chuyển khoản của đơn: đơn được xác nhận => yêu cầu đã thanh toán; đơn bị hủy => yêu cầu hết hiệu lực
        if (booking.Status == BookingStatuses.Confirmed || booking.Status == BookingStatuses.Cancelled)
        {
            var pendingOrders = await _unitOfWork.PaymentOrders.Query()
                .Where(p => p.BookingId == booking.Id && p.Purpose == PaymentPurposes.Booking
                            && (p.Status == PaymentOrderStatuses.Pending || p.Status == PaymentOrderStatuses.Held))
                .ToListAsync();
            foreach (var order in pendingOrders)
            {
                if (booking.Status == BookingStatuses.Confirmed)
                {
                    order.Status = PaymentOrderStatuses.Paid;
                    order.PaidAt = DateTime.UtcNow;
                }
                else
                {
                    order.Status = PaymentOrderStatuses.Expired;
                }
            }
        }

        // Hủy đơn đã xác nhận đã trả bằng ví thì hoàn tiền về ví (lưu cùng lần SaveChanges)
        // Doanh thu chủ sân: duyệt đơn đã thanh toán online => ghi nhận; hủy đơn đã duyệt => thu hồi
        if (booking.Status == BookingStatuses.Confirmed)
            await _walletService.SettleBookingIncomeAsync(booking.Id, DateTime.UtcNow);
        if (booking.Status == BookingStatuses.Cancelled && previousStatus == BookingStatuses.Confirmed)
            await _walletService.ReverseBookingIncomeAsync(booking.Id, DateTime.UtcNow);

        // Hủy đơn đã trừ ví (đang chờ duyệt hoặc đã xác nhận) thì hoàn tiền; đơn chưa thanh toán không được hoàn (wallet tự kiểm tra)
        if (booking.Status == BookingStatuses.Cancelled && (previousStatus == BookingStatuses.Confirmed || previousStatus == BookingStatuses.Pending))
        {
            await _walletService.RefundBookingToWalletAsync(booking.Id);
        }

        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<BookingDto>.Ok(_mapper.Map<BookingDto>(booking), $"Cập nhật trạng thái đơn thành công sang {booking.Status}.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int currentUserId, string role)
    {
        var booking = await _unitOfWork.Bookings.Query()
            .Include(b => b.Court)
                .ThenInclude(c => c!.Facility)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy đơn đặt sân.");
        }

        // Only Pending or Cancelled bookings can be deleted
        if (booking.Status != BookingStatuses.Pending && booking.Status != BookingStatuses.Cancelled)
        {
            return ApiResponse<bool>.Fail("Chỉ có thể xóa các đơn ở trạng thái Chờ duyệt (Pending) hoặc Đã hủy (Cancelled).");
        }

        if (role == AppRoles.Player && booking.PlayerId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa đơn này.");
        }

        if (role == AppRoles.Provider && booking.Court?.Facility?.ProviderId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền xóa đơn này.");
        }

        _unitOfWork.Bookings.Delete(booking);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa đơn đặt sân thành công.");
    }
}

