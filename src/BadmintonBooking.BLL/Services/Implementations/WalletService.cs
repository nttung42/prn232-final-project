using AutoMapper;
using BadmintonBooking.BLL.DTOs.Wallet;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonBooking.BLL.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVietQrService _vietQrService;
    private readonly IMapper _mapper;

    public WalletService(IUnitOfWork unitOfWork, IVietQrService vietQrService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _vietQrService = vietQrService;
        _mapper = mapper;
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

        var refCode = _vietQrService.GenerateReferenceCode("NAP");
        var bankId = "MB";
        var accountNo = "999988887777";
        var accountName = "CTY CP COURTSIDE VIETNAM";
        var transferContent = $"COURTSIDE {refCode}";

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

    public async Task<WalletDto> ConfirmTopupAsync(int transactionId)
    {
        var transaction = await _unitOfWork.WalletTransactions.GetByIdAsync(transactionId);
        if (transaction == null)
        {
            throw new KeyNotFoundException("Không tìm thấy giao dịch nạp tiền.");
        }

        if (transaction.Status == WalletTransactionStatuses.Completed)
        {
            throw new InvalidOperationException("Giao dịch này đã được xác nhận trước đó.");
        }

        var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId);
        if (wallet == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ví của người dùng.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            wallet.Balance += transaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            transaction.Status = WalletTransactionStatuses.Completed;

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return await GetOrCreateWalletByUserIdAsync(wallet.UserId);
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

        if (booking.Status == BookingStatuses.Confirmed)
        {
            throw new InvalidOperationException("Đơn đặt sân này đã được xác nhận thanh toán.");
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

            booking.Status = BookingStatuses.Confirmed;
            booking.Note = (booking.Note ?? "") + " [Đã thanh toán qua Ví CourtSide]";

            await _unitOfWork.WalletTransactions.AddAsync(transaction);
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

    public async Task<bool> RefundBookingToWalletAsync(int bookingId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        var wallet = await _unitOfWork.Wallets.Query()
            .FirstOrDefaultAsync(w => w.UserId == booking.PlayerId);

        if (wallet == null) return false;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            wallet.Balance += booking.TotalPrice;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = booking.TotalPrice,
                TransactionType = WalletTransactionTypes.BookingRefund,
                Status = WalletTransactionStatuses.Completed,
                Description = $"Hoàn tiền đơn đặt sân #{booking.Id} bị hủy",
                ReferenceCode = $"REFUND_{booking.Id}",
                CreatedAt = DateTime.UtcNow
            };

            booking.Status = BookingStatuses.Cancelled;
            await _unitOfWork.WalletTransactions.AddAsync(transaction);
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
}

