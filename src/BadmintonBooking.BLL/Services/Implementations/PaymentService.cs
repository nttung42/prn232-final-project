using System.Globalization;
using BadmintonBooking.BLL.DTOs.Payments;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.DAL.UnitOfWork;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BadmintonBooking.BLL.Services.Implementations;

/// <summary>
/// Nghiệp vụ thanh toán chuyển khoản.
/// - Đơn đặt sân: tạo PaymentOrder (Booking), người chơi chuyển khoản kèm mã; webhook khớp mã => Confirmed.
/// - Nạp ví: tạo PaymentOrder (TopUp) + WalletTransaction Pending; webhook khớp mã => cộng ví.
/// - Chống nhận trùng theo SePay id; thiếu tiền được giữ theo đơn và hoàn về ví khi hết hạn.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IVietQrService _vietQr;
    private readonly IConfiguration _config;
    private readonly IWalletService _wallet;

    public PaymentService(IUnitOfWork uow, IVietQrService vietQr, IConfiguration config, IWalletService wallet)
    {
        _uow = uow;
        _vietQr = vietQr;
        _config = config;
        _wallet = wallet;
    }

    /// <summary>Thời gian giữ chỗ đơn chờ thanh toán (phút). Mặc định 15.</summary>
    private int HoldMinutes => int.TryParse(_config["Payments:HoldMinutes"], out var m) && m > 0 ? m : 15;

    public async Task<PaymentQrDto> CreateBookingPaymentAsync(int userId, int bookingId)
    {
        var booking = await _uow.Bookings.Query().FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn đặt sân.");

        if (booking.PlayerId != userId)
            throw new UnauthorizedAccessException("Bạn không phải chủ sở hữu đơn đặt sân này.");
        if (booking.Status != BookingStatuses.Pending)
            throw new InvalidOperationException("Chỉ thanh toán được đơn đang chờ thanh toán.");

        var now = DateTime.UtcNow;
        var alreadyPaid = await _uow.PaymentOrders.Query().AnyAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking
            && (p.Status == PaymentOrderStatuses.Held || p.Status == PaymentOrderStatuses.Paid));
        if (alreadyPaid)
            throw new InvalidOperationException("Đơn này đã được thanh toán, không cần chuyển khoản thêm.");

        var existing = await _uow.PaymentOrders.Query().FirstOrDefaultAsync(p =>
            p.BookingId == bookingId && p.Purpose == PaymentPurposes.Booking
            && p.Status == PaymentOrderStatuses.Pending && p.ExpiresAt > now);
        if (existing != null) return ToQr(existing);

        var expiresAt = booking.CreatedAt.AddMinutes(HoldMinutes);
        if (expiresAt <= now)
            throw new InvalidOperationException("Đơn đã hết thời gian giữ chỗ. Vui lòng đặt lại.");

        var order = new PaymentOrder
        {
            Code = PaymentCodes.New(),
            Purpose = PaymentPurposes.Booking,
            UserId = userId,
            BookingId = bookingId,
            Amount = booking.TotalPrice,
            Status = PaymentOrderStatuses.Pending,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        await _uow.PaymentOrders.AddAsync(order);
        await _uow.SaveChangesAsync();
        return ToQr(order);
    }

    public async Task<PaymentStatusDto> GetStatusAsync(int userId, string code)
    {
        var order = await _uow.PaymentOrders.Query().AsNoTracking().FirstOrDefaultAsync(p => p.Code == code)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu thanh toán.");
        if (order.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu thanh toán này.");

        return new PaymentStatusDto
        {
            Code = order.Code,
            Purpose = order.Purpose,
            Status = order.Status,
            Amount = order.Amount,
            ExpiresAt = order.ExpiresAt,
            PaidAt = order.PaidAt
        };
    }

    public async Task<string> ProcessSepayWebhookAsync(SepayWebhookDto webhook)
    {
        if (!string.Equals(webhook.TransferType, "in", StringComparison.OrdinalIgnoreCase))
            return "ignored-outgoing";

        var text = $"{webhook.Content} {webhook.Code} {webhook.Description}".ToUpperInvariant();
        var match = PaymentCodes.Pattern.Match(text);
        var now = DateTime.UtcNow;

        await _uow.BeginTransactionAsync();
        try
        {
            // Chống nhận trùng: cùng một giao dịch SePay chỉ xử lý một lần
            if (await _uow.BankTransactions.Query().AnyAsync(b => b.SepayId == webhook.Id))
            {
                await _uow.CommitTransactionAsync();
                return "duplicate";
            }

            var bank = new BankTransaction
            {
                SepayId = webhook.Id,
                Gateway = webhook.Gateway,
                AccountNumber = webhook.AccountNumber,
                TransactionDate = ParseSepayDate(webhook.TransactionDate, now),
                Content = webhook.Content,
                TransferAmount = webhook.TransferAmount,
                MatchedCode = match.Success ? match.Value : null,
                ReceivedAt = now
            };
            await _uow.BankTransactions.AddAsync(bank);
            await _uow.SaveChangesAsync(); // Unique SepayId chặn trường hợp hai webhook trùng chạy song song

            if (!match.Success)
            {
                bank.Note = "Không tìm thấy mã thanh toán trong nội dung";
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();
                return "unmatched";
            }

            var order = await _uow.PaymentOrders.Query().FirstOrDefaultAsync(p => p.Code == match.Value);
            if (order == null)
            {
                bank.Note = "Mã thanh toán không tồn tại";
                await _uow.SaveChangesAsync();
                await _uow.CommitTransactionAsync();
                return "unknown-code";
            }

            bank.PaymentOrderId = order.Id;
            await _uow.SaveChangesAsync(); // Lưu liên kết trước để tính tổng tiền đã nhận của đơn

            var result = order.Purpose == PaymentPurposes.TopUp
                ? await FulfillTopUpAsync(order, webhook.TransferAmount, bank, now)
                : await FulfillBookingAsync(order, bank, now);

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
            return result;
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<int> ExpirePaymentsAsync(DateTime utcNow)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var orders = await _uow.PaymentOrders.Query()
                .Where(p => (p.Status == PaymentOrderStatuses.Pending || p.Status == PaymentOrderStatuses.Underpaid)
                            && p.ExpiresAt < utcNow)
                .ToListAsync();

            foreach (var order in orders)
            {
                // Tiền đã chuyển nhưng chưa đủ: hoàn lại ví
                var received = await _uow.BankTransactions.Query()
                    .Where(b => b.PaymentOrderId == order.Id)
                    .SumAsync(b => b.TransferAmount);

                if (order.Purpose == PaymentPurposes.Booking)
                {
                    var booking = await _uow.Bookings.Query().FirstOrDefaultAsync(b => b.Id == order.BookingId);
                    if (booking != null && booking.Status == BookingStatuses.Pending)
                    {
                        booking.Status = BookingStatuses.Cancelled;
                        booking.Note = (booking.Note ?? "") + " [Hết thời gian thanh toán]";
                    }
                    if (received > 0 && order.Status == PaymentOrderStatuses.Underpaid)
                    {
                        await CreditWalletAsync(order.UserId, received,
                            $"Hoàn số tiền chuyển thiếu của đơn #{order.BookingId} đã hết hạn", utcNow);
                    }
                }
                else
                {
                    var tx = await _uow.WalletTransactions.Query().FirstOrDefaultAsync(t =>
                        t.ReferenceCode == order.Code && t.TransactionType == WalletTransactionTypes.Deposit
                        && t.Status == WalletTransactionStatuses.Pending);
                    if (tx != null) tx.Status = WalletTransactionStatuses.Cancelled;
                }

                order.Status = PaymentOrderStatuses.Expired;
            }

            // Đơn đặt sân giữ chỗ quá hạn mà chưa từng tạo yêu cầu thanh toán
            var holdLimit = utcNow.AddMinutes(-HoldMinutes);
            var staleBookings = await _uow.Bookings.Query()
                .Where(b => b.Status == BookingStatuses.Pending && b.CreatedAt < holdLimit
                            && !_uow.PaymentOrders.Query().Any(p => p.BookingId == b.Id)) // đã có yêu cầu thanh toán thì do luồng đơn xử lý
                .ToListAsync();
            foreach (var booking in staleBookings)
            {
                booking.Status = BookingStatuses.Cancelled;
                booking.Note = (booking.Note ?? "") + " [Hết thời gian giữ chỗ]";
            }

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
            return orders.Count + staleBookings.Count;
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<List<BankTransactionDto>> GetBankTransactionsAsync(int take)
    {
        return await _uow.BankTransactions.Query()
            .AsNoTracking()
            .OrderByDescending(b => b.Id)
            .Take(Math.Clamp(take, 1, 200))
            .Select(b => new BankTransactionDto
            {
                Id = b.Id,
                SepayId = b.SepayId,
                Gateway = b.Gateway,
                AccountNumber = b.AccountNumber,
                TransactionDate = b.TransactionDate,
                Content = b.Content,
                TransferAmount = b.TransferAmount,
                MatchedCode = b.MatchedCode,
                PaymentOrderId = b.PaymentOrderId,
                Note = b.Note,
                ReceivedAt = b.ReceivedAt
            })
            .ToListAsync();
    }

    public async Task<List<PaymentReviewDto>> GetPendingReviewAsync(int actorUserId, string actorRole)
    {
        var now = DateTime.UtcNow;
        var query = _uow.PaymentOrders.Query().AsNoTracking()
            .Where(p => (p.Status == PaymentOrderStatuses.Pending && p.ExpiresAt > now)
                        || p.Status == PaymentOrderStatuses.Held); // đơn đã trừ ví chờ duyệt không hết hạn

        if (actorRole == AppRoles.Provider)
        {
            query = query.Where(p => p.Purpose == PaymentPurposes.Booking
                                     && p.Booking!.Court!.Facility!.ProviderId == actorUserId);
        }
        else if (actorRole != AppRoles.Admin)
        {
            return new List<PaymentReviewDto>();
        }

        return await query
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PaymentReviewDto
            {
                Code = p.Code,
                Purpose = p.Purpose,
                Amount = p.Amount,
                Status = p.Status,
                BookingId = p.BookingId,
                FacilityName = p.Booking != null && p.Booking.Court != null && p.Booking.Court.Facility != null
                    ? p.Booking.Court.Facility.Name : null,
                PlayerName = p.User != null ? p.User.FullName : null,
                CreatedAt = p.CreatedAt,
                ExpiresAt = p.ExpiresAt
            })
            .ToListAsync();
    }

    public async Task ConfirmManuallyAsync(string code, int actorUserId, string actorRole)
    {
        if (actorRole != AppRoles.Admin && actorRole != AppRoles.Provider)
            throw new UnauthorizedAccessException("Bạn không có quyền xác nhận thanh toán.");

        var now = DateTime.UtcNow;
        await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.PaymentOrders.Query().FirstOrDefaultAsync(p => p.Code == code)
                ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu thanh toán.");

            if (order.Status == PaymentOrderStatuses.Paid)
                throw new InvalidOperationException("Yêu cầu này đã được xác nhận trước đó.");
            var isHeldBooking = order.Status == PaymentOrderStatuses.Held && order.Purpose == PaymentPurposes.Booking;
            if (order.Status != PaymentOrderStatuses.Pending && !isHeldBooking)
                throw new InvalidOperationException($"Yêu cầu đang ở trạng thái {order.Status}, không thể xác nhận.");

            if (order.Purpose == PaymentPurposes.TopUp)
            {
                if (actorRole != AppRoles.Admin)
                    throw new UnauthorizedAccessException("Chỉ Admin được xác nhận nạp ví.");

                var wallet = await _uow.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == order.UserId)
                    ?? await CreateWalletAsync(order.UserId, now);
                wallet.Balance += order.Amount;
                wallet.UpdatedAt = now;

                var tx = await _uow.WalletTransactions.Query().FirstOrDefaultAsync(t =>
                    t.ReferenceCode == order.Code && t.TransactionType == WalletTransactionTypes.Deposit);
                if (tx != null)
                {
                    tx.Status = WalletTransactionStatuses.Completed;
                }
                else
                {
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction
                    {
                        Wallet = wallet,
                        Amount = order.Amount,
                        TransactionType = WalletTransactionTypes.Deposit,
                        Status = WalletTransactionStatuses.Completed,
                        Description = "Nạp tiền vào ví CourtSide qua chuyển khoản",
                        ReferenceCode = order.Code,
                        CreatedAt = now
                    });
                }
            }
            else
            {
                var booking = await _uow.Bookings.Query()
                    .Include(b => b.Court).ThenInclude(c => c!.Facility)
                    .FirstOrDefaultAsync(b => b.Id == order.BookingId)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn đặt sân.");

                if (actorRole == AppRoles.Provider && booking.Court?.Facility?.ProviderId != actorUserId)
                    throw new UnauthorizedAccessException("Đơn này không thuộc cơ sở của bạn.");
                if (booking.Status != BookingStatuses.Pending)
                    throw new InvalidOperationException("Đơn không còn ở trạng thái chờ thanh toán.");

                booking.Status = BookingStatuses.Confirmed;
                booking.Note = (booking.Note ?? "") + $" [Đã xác nhận chuyển khoản, mã {order.Code}]";
            }

            order.Status = PaymentOrderStatuses.Paid;
            order.PaidAt = now;

            // Đơn đặt sân được duyệt: lưu trạng thái trước rồi ghi doanh thu cho chủ sân (trừ phí nền tảng)
            await _uow.SaveChangesAsync();
            if (order.Purpose == PaymentPurposes.Booking && order.BookingId != null)
                await _wallet.SettleBookingIncomeAsync(order.BookingId.Value, now);

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RejectAsync(string code, int actorUserId, string actorRole)
    {
        if (actorRole != AppRoles.Admin && actorRole != AppRoles.Provider)
            throw new UnauthorizedAccessException("Bạn không có quyền từ chối thanh toán.");

        var now = DateTime.UtcNow;
        await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.PaymentOrders.Query().FirstOrDefaultAsync(p => p.Code == code)
                ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu thanh toán.");
            if (order.Purpose != PaymentPurposes.Booking)
                throw new InvalidOperationException("Chỉ từ chối được đơn đặt sân.");
            if (order.Status != PaymentOrderStatuses.Pending && order.Status != PaymentOrderStatuses.Held)
                throw new InvalidOperationException($"Yêu cầu đang ở trạng thái {order.Status}, không thể từ chối.");

            var booking = await _uow.Bookings.Query()
                .Include(b => b.Court).ThenInclude(c => c!.Facility)
                .FirstOrDefaultAsync(b => b.Id == order.BookingId)
                ?? throw new KeyNotFoundException("Không tìm thấy đơn đặt sân.");

            if (actorRole == AppRoles.Provider && booking.Court?.Facility?.ProviderId != actorUserId)
                throw new UnauthorizedAccessException("Đơn này không thuộc cơ sở của bạn.");
            if (booking.Status != BookingStatuses.Pending)
                throw new InvalidOperationException("Đơn không còn ở trạng thái chờ xác nhận.");

            // Tiền đã trừ ví (đơn trả bằng ví): hoàn lại đủ
            if (order.Status == PaymentOrderStatuses.Held)
                await CreditWalletAsync(order.UserId, order.Amount, $"Hoàn tiền đơn #{booking.Id} bị từ chối", now);

            booking.Status = BookingStatuses.Cancelled;
            booking.Note = (booking.Note ?? "") + " [Chủ sân từ chối đơn]";
            order.Status = PaymentOrderStatuses.Expired;

            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
        }
        catch
        {
            await _uow.RollbackTransactionAsync();
            throw;
        }
    }

    // ---------- Nghiệp vụ nội bộ ----------

    private async Task<string> FulfillTopUpAsync(PaymentOrder order, decimal amount, BankTransaction bank, DateTime now)
    {
        var wasPaid = order.Status == PaymentOrderStatuses.Paid;
        order.Status = PaymentOrderStatuses.Paid;
        order.PaidAt = now;

        var wallet = await _uow.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == order.UserId)
            ?? await CreateWalletAsync(order.UserId, now);
        wallet.Balance += amount;
        wallet.UpdatedAt = now;

        var tx = await _uow.WalletTransactions.Query().FirstOrDefaultAsync(t =>
            t.ReferenceCode == order.Code && t.TransactionType == WalletTransactionTypes.Deposit);
        if (tx != null)
        {
            tx.Amount = amount;
            tx.Status = WalletTransactionStatuses.Completed;
        }
        else
        {
            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                Wallet = wallet,
                Amount = amount,
                TransactionType = WalletTransactionTypes.Deposit,
                Status = WalletTransactionStatuses.Completed,
                Description = "Nạp tiền vào ví CourtSide qua chuyển khoản",
                ReferenceCode = order.Code,
                CreatedAt = now
            });
        }

        bank.Note = wasPaid ? "Nạp trùng yêu cầu đã thanh toán, đã cộng vào ví" : "Nạp ví thành công";
        return "topup-credited";
    }

    private async Task<string> FulfillBookingAsync(PaymentOrder order, BankTransaction bank, DateTime now)
    {
        var booking = await _uow.Bookings.Query().FirstOrDefaultAsync(b => b.Id == order.BookingId);

        // Tổng tiền đã nhận cho đơn này (gồm cả giao dịch hiện tại)
        var received = await _uow.BankTransactions.Query()
            .Where(b => b.PaymentOrderId == order.Id)
            .SumAsync(b => b.TransferAmount);

        if (booking == null)
        {
            order.Status = PaymentOrderStatuses.Paid;
            order.PaidAt = now;
            await CreditWalletAsync(order.UserId, bank.TransferAmount, "Hoàn tiền chuyển khoản cho đơn không tồn tại", now);
            bank.Note = "Đơn không tồn tại, đã hoàn vào ví";
            return "booking-missing";
        }

        if (booking.Status == BookingStatuses.Pending)
        {
            if (received < order.Amount)
            {
                // Chuyển thiếu: giữ số tiền theo đơn, chờ đủ hoặc hết hạn sẽ hoàn lại ví
                order.Status = PaymentOrderStatuses.Underpaid;
                bank.Note = $"Chuyển thiếu: đã nhận {received:N0}đ / cần {order.Amount:N0}đ";
                return "underpaid";
            }

            order.Status = PaymentOrderStatuses.Paid;
            order.PaidAt = now;
            booking.Status = BookingStatuses.Confirmed;
            booking.Note = (booking.Note ?? "") + $" [Đã thanh toán chuyển khoản, mã {order.Code}]";

            var excess = received - order.Amount;
            if (excess > 0)
                await CreditWalletAsync(order.UserId, excess, $"Thừa tiền thanh toán đơn #{booking.Id}", now);

            bank.Note = "Xác nhận đơn đặt sân";
            return "booking-confirmed";
        }

        // Đơn đã hủy/hoàn thành/xác nhận trước đó: không giữ tiền, hoàn toàn bộ vào ví
        order.Status = PaymentOrderStatuses.Paid;
        order.PaidAt = now;
        var reason = booking.Status == BookingStatuses.Confirmed
            ? $"Thanh toán trùng cho đơn #{booking.Id} đã xác nhận"
            : $"Thanh toán muộn cho đơn #{booking.Id} đã hủy";
        await CreditWalletAsync(order.UserId, bank.TransferAmount, $"{reason}, hoàn vào ví", now);
        bank.Note = $"Đơn ở trạng thái {booking.Status}, đã hoàn vào ví";
        return "late-payment-credited";
    }

    private async Task CreditWalletAsync(int userId, decimal amount, string description, DateTime now)
    {
        if (amount <= 0) return;
        var wallet = await _uow.Wallets.Query().FirstOrDefaultAsync(w => w.UserId == userId)
            ?? await CreateWalletAsync(userId, now);
        wallet.Balance += amount;
        wallet.UpdatedAt = now;
        await _uow.WalletTransactions.AddAsync(new WalletTransaction
        {
            Wallet = wallet,
            Amount = amount,
            TransactionType = WalletTransactionTypes.BookingRefund,
            Status = WalletTransactionStatuses.Completed,
            Description = description,
            ReferenceCode = $"CREDIT_{Guid.NewGuid():N}"[..20],
            CreatedAt = now
        });
    }

    private async Task<Wallet> CreateWalletAsync(int userId, DateTime now)
    {
        var wallet = new Wallet { UserId = userId, Balance = 0, UpdatedAt = now };
        await _uow.Wallets.AddAsync(wallet);
        return wallet;
    }

    private PaymentQrDto ToQr(PaymentOrder order)
    {
        var bankId = _config["VietQr:BankId"] ?? "MB";
        var accountNo = _config["VietQr:AccountNo"] ?? string.Empty;
        var accountName = _config["VietQr:AccountName"] ?? string.Empty;
        return new PaymentQrDto
        {
            Code = order.Code,
            Purpose = order.Purpose,
            BookingId = order.BookingId,
            Amount = order.Amount,
            Status = order.Status,
            ExpiresAt = order.ExpiresAt,
            QrUrl = _vietQr.GenerateVietQrUrl(bankId, accountNo, accountName, order.Amount, order.Code),
            BankId = bankId,
            AccountNo = accountNo,
            AccountName = accountName,
            TransferContent = order.Code
        };
    }

    private static DateTime ParseSepayDate(string? value, DateTime fallback)
    {
        return DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed) ? parsed : fallback;
    }
}
