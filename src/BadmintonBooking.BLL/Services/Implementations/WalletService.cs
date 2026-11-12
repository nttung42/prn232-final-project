using System.Globalization;
using AutoMapper;
using BadmintonBooking.BLL.DTOs.Wallet;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BadmintonBooking.BLL.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVietQrService _vietQrService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;

    public WalletService(IUnitOfWork unitOfWork, IVietQrService vietQrService, IMapper mapper, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _vietQrService = vietQrService;
        _mapper = mapper;
        _config = config;
    }

    public async Task<WalletDto> GetOrCreateWalletByUserIdAsync(int userId)
    {
        var wallet = await _unitOfWork.Wallets.Query()
            .Include(w => w.User)
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(20))
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy người dùng với Id {userId}");
            }

            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Wallets.AddAsync(wallet);
            await _unitOfWork.SaveChangesAsync();
            wallet.User = user;
        }

        var dto = _mapper.Map<WalletDto>(wallet);
        dto.FullName = wallet.User?.FullName;
        dto.Email = wallet.User?.Email;
        dto.RecentTransactions = _mapper.Map<List<WalletTransactionDto>>(wallet.Transactions);
        return dto;
    }

    public async Task<List<WalletTransactionDto>> GetTransactionsByUserIdAsync(int userId)
    {
        var wallet = await _unitOfWork.Wallets.Query()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null) return new List<WalletTransactionDto>();

        var transactions = await _unitOfWork.WalletTransactions.Query()
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<WalletTransactionDto>>(transactions);
    }

    public async Task<TopupResponseDto> CreateTopupAsync(int userId, TopupRequestDto request)
    {
        var wallet = await _unitOfWork.Wallets.Query()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            await GetOrCreateWalletByUserIdAsync(userId);
            wallet = await _unitOfWork.Wallets.Query().FirstAsync(w => w.UserId == userId);
        }

        // Mã thanh toán duy nhất đặt trong nội dung chuyển khoản; webhook SePay khớp mã này để cộng ví
        var refCode = PaymentCodes.New();
        var bankId = _config["VietQr:BankId"] ?? "MB";
        var accountNo = _config["VietQr:AccountNo"] ?? string.Empty;
        var accountName = _config["VietQr:AccountName"] ?? string.Empty;
        var transferContent = refCode;

        var qrUrl = _vietQrService.GenerateVietQrUrl(bankId, accountNo, accountName, request.Amount, transferContent);

        var transaction = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = request.Amount,
            TransactionType = WalletTransactionTypes.Deposit,
            Status = WalletTransactionStatuses.Pending,
            Description = request.Description ?? $"Nạp tiền vào ví CourtSide qua VietQR",
            ReferenceCode = refCode,
            QrUrl = qrUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.WalletTransactions.AddAsync(transaction);
        await _unitOfWork.PaymentOrders.AddAsync(new PaymentOrder
        {
            Code = refCode,
            Purpose = PaymentPurposes.TopUp,
            UserId = userId,
            Amount = request.Amount,
            Status = PaymentOrderStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
        await _unitOfWork.SaveChangesAsync();

        return new TopupResponseDto
        {
            TransactionId = transaction.Id,
            ReferenceCode = refCode,
            Amount = request.Amount,
            QrUrl = qrUrl,
            BankId = bankId,
            BankName = "MB Bank (Ngân hàng Quân Đội)",
            AccountNo = accountNo,
            AccountName = accountName,
            TransferContent = transferContent
        };
    }

    public async Task<bool> PayBookingByWalletAsync(int userId, int bookingId)
    {
        var wallet = await _unitOfWork.Wallets.Query()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            throw new InvalidOperationException("Tài khoản chưa được kích hoạt ví.");
        }

        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
        {
            throw new KeyNotFoundException("Không tìm thấy đơn đặt sân.");
        }

        if (booking.PlayerId != userId)
        {
            throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu đơn đặt sân này.");
        }

        if (booking.Status != BookingStatuses.Pending)
        {
            throw new InvalidOperationException("Chỉ thanh toán được đơn đang chờ thanh toán.");
        }

        var alreadyHeld = await _unitOfWork.PaymentOrders.Query().AnyAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking
            && (p.Status == PaymentOrderStatuses.Held || p.Status == PaymentOrderStatuses.Paid));
        if (alreadyHeld)
        {
            throw new InvalidOperationException("Đơn đặt sân này đã được thanh toán, đang chờ chủ sân xác nhận.");
        }

        if (wallet.Balance < booking.TotalPrice)
        {
            throw new InvalidOperationException($"Số dư ví không đủ (Số dư: {wallet.Balance:N0}đ, Tiền sân: {booking.TotalPrice:N0}đ). Vui lòng nạp thêm tiền.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            wallet.Balance -= booking.TotalPrice;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = booking.TotalPrice,
                TransactionType = WalletTransactionTypes.BookingPayment,
                Status = WalletTransactionStatuses.Completed,
                Description = $"Thanh toán đơn đặt sân #{booking.Id}",
                ReferenceCode = $"BK_{booking.Id}",
                CreatedAt = DateTime.UtcNow
            };

            // Tiền đã được giữ trong đơn; chủ sân/Admin xác nhận đơn (hoặc từ chối để hoàn ví).
            booking.Note = (booking.Note ?? "") + " [Đã trừ Ví CourtSide, chờ chủ sân xác nhận]";

            await _unitOfWork.WalletTransactions.AddAsync(transaction);
            await _unitOfWork.PaymentOrders.AddAsync(new PaymentOrder
            {
                Code = PaymentCodes.New(),
                Purpose = PaymentPurposes.Booking,
                UserId = userId,
                BookingId = bookingId,
                Amount = booking.TotalPrice,
                Status = PaymentOrderStatuses.Held,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Hoàn tiền về ví cho đơn đã thanh toán bằng ví. Chỉ hoàn khi đơn thực sự đã trả bằng ví
    /// và chưa được hoàn trước đó (tránh tạo tiền từ không có).
    /// Không tự đổi trạng thái đơn; người gọi chịu trách nhiệm đổi trạng thái.
    /// </summary>
    public async Task<bool> RefundBookingToWalletAsync(int bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        // Đã thanh toán bằng ví, hoặc bằng chuyển khoản (yêu cầu thanh toán đã Paid)
        var paidByWallet = await _unitOfWork.WalletTransactions.Query().AnyAsync(t =>
            t.ReferenceCode == $"BK_{bookingId}" && t.TransactionType == WalletTransactionTypes.BookingPayment);
        var paidByTransfer = await _unitOfWork.PaymentOrders.Query().AnyAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking && p.Status == PaymentOrderStatuses.Paid);
        if (!paidByWallet && !paidByTransfer) return false;

        var alreadyRefunded = await _unitOfWork.WalletTransactions.Query().AnyAsync(t =>
            t.ReferenceCode == $"REFUND_{bookingId}" && t.TransactionType == WalletTransactionTypes.BookingRefund);
        if (alreadyRefunded) return false;

        var wallet = await _unitOfWork.Wallets.Query()
            .FirstOrDefaultAsync(w => w.UserId == booking.PlayerId);
        if (wallet == null) return false;

        wallet.Balance += booking.TotalPrice;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = booking.TotalPrice,
            TransactionType = WalletTransactionTypes.BookingRefund,
            Status = WalletTransactionStatuses.Completed,
            Description = $"Hoàn tiền đơn đặt sân #{booking.Id} bị hủy",
            ReferenceCode = $"REFUND_{booking.Id}",
            CreatedAt = DateTime.UtcNow
        });

        return true;
    }

    /// <summary>
    /// Ghi nhận doanh thu cho chủ sân khi đơn đã thanh toán online (ví giữ tiền hoặc QR đã xác nhận) được duyệt.
    /// Chủ sân nhận giá sân trừ phí nền tảng (Payments:PlatformFeePercent, mặc định 5%). Không lưu thay đổi; người gọi lưu.
    /// </summary>
    public async Task<bool> SettleBookingIncomeAsync(int bookingId, DateTime now)
    {
        var booking = await _unitOfWork.Bookings.Query()
            .Include(b => b.Court).ThenInclude(c => c!.Facility)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        var providerId = booking?.Court?.Facility?.ProviderId;
        if (booking == null || providerId == null) return false;

        var paidOnline = await _unitOfWork.PaymentOrders.Query().AnyAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking
            && (p.Status == PaymentOrderStatuses.Held || p.Status == PaymentOrderStatuses.Pending || p.Status == PaymentOrderStatuses.Paid));
        if (!paidOnline) return false; // đơn tiền mặt tại quầy không có yêu cầu thanh toán: không ghi doanh thu qua ví

        var reference = $"INCOME_{bookingId}";
        if (await _unitOfWork.WalletTransactions.Query().AnyAsync(t =>
            t.ReferenceCode == reference && t.TransactionType == WalletTransactionTypes.BookingIncome))
            return false; // đã ghi nhận rồi

        var feePercent = decimal.TryParse(_config["Payments:PlatformFeePercent"], NumberStyles.Number, CultureInfo.InvariantCulture, out var p) && p >= 0 ? p : 5m;
        var fee = Math.Round(booking.TotalPrice * feePercent / 100m, 0, MidpointRounding.AwayFromZero);
        var net = booking.TotalPrice - fee;

        var wallet = await _unitOfWork.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == providerId.Value);
        if (wallet == null)
        {
            wallet = new Wallet { UserId = providerId.Value, Balance = 0, UpdatedAt = now };
            await _unitOfWork.Wallets.AddAsync(wallet);
        }
        wallet.Balance += net;
        wallet.UpdatedAt = now;

        // Lưu số liệu doanh thu/phí lên yêu cầu thanh toán để lịch sử và dashboard tính được
        var order = await _unitOfWork.PaymentOrders.Query().FirstOrDefaultAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking && p.SettledAt == null
            && (p.Status == PaymentOrderStatuses.Held || p.Status == PaymentOrderStatuses.Pending || p.Status == PaymentOrderStatuses.Paid));
        if (order != null)
        {
            order.PlatformFee = fee;
            order.ProviderAmount = net;
            order.SettledAt = now;
            order.ReversedAt = null;
        }

        await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
        {
            Wallet = wallet,
            Amount = net,
            TransactionType = WalletTransactionTypes.BookingIncome,
            Status = WalletTransactionStatuses.Completed,
            Description = $"Doanh thu đơn #{bookingId} (đã trừ phí nền tảng {fee:N0}đ)",
            ReferenceCode = reference,
            CreatedAt = now
        });
        return true;
    }

    /// <summary>
    /// Thu hồi doanh thu đã ghi nhận của chủ sân khi đơn đã duyệt bị hủy. Số dư có thể âm nếu chủ sân đã dùng hết.
    /// </summary>
    public async Task<bool> ReverseBookingIncomeAsync(int bookingId, DateTime now)
    {
        var income = await _unitOfWork.WalletTransactions.Query().FirstOrDefaultAsync(t =>
            t.ReferenceCode == $"INCOME_{bookingId}" && t.TransactionType == WalletTransactionTypes.BookingIncome);
        if (income == null) return false;

        var reversalRef = $"REVERSE_{bookingId}";
        if (await _unitOfWork.WalletTransactions.Query().AnyAsync(t =>
            t.ReferenceCode == reversalRef && t.TransactionType == WalletTransactionTypes.BookingIncomeReversal))
            return false;

        var wallet = await _unitOfWork.Wallets.Query().FirstAsync(w => w.Id == income.WalletId);
        wallet.Balance -= income.Amount;
        wallet.UpdatedAt = now;

        var settledOrder = await _unitOfWork.PaymentOrders.Query().FirstOrDefaultAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking && p.SettledAt != null && p.ReversedAt == null);
        if (settledOrder != null)
        {
            settledOrder.ReversedAt = now;
            settledOrder.PlatformFee = 0;
            settledOrder.ProviderAmount = 0;
        }

        await _unitOfWork.WalletTransactions.AddAsync(new WalletTransaction
        {
            Wallet = wallet,
            Amount = income.Amount,
            TransactionType = WalletTransactionTypes.BookingIncomeReversal,
            Status = WalletTransactionStatuses.Completed,
            Description = $"Thu hồi doanh thu đơn #{bookingId} đã hủy",
            ReferenceCode = reversalRef,
            CreatedAt = now
        });
        return true;
    }

    public async Task<List<PendingTopupDto>> GetPendingTopupsAsync()
    {
        return await _unitOfWork.WalletTransactions.Query()
            .Include(t => t.Wallet).ThenInclude(w => w!.User)
            .Where(t => t.TransactionType == WalletTransactionTypes.Deposit && t.Status == WalletTransactionStatuses.Pending)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new PendingTopupDto
            {
                TransactionId = t.Id,
                UserId = t.Wallet!.UserId,
                UserFullName = t.Wallet!.User!.FullName,
                UserEmail = t.Wallet!.User!.Email,
                Amount = t.Amount,
                ReferenceCode = t.ReferenceCode,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync();
    }
}

