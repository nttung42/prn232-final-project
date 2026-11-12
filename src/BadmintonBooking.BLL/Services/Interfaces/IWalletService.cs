using BadmintonBooking.BLL.DTOs.Wallet;

namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IWalletService
{
    Task<WalletDto> GetOrCreateWalletByUserIdAsync(int userId);
    Task<List<WalletTransactionDto>> GetTransactionsByUserIdAsync(int userId);
    Task<TopupResponseDto> CreateTopupAsync(int userId, TopupRequestDto request);
    Task<bool> PayBookingByWalletAsync(int userId, int bookingId);
    Task<bool> RefundBookingToWalletAsync(int bookingId);
    Task<List<PendingTopupDto>> GetPendingTopupsAsync();
    Task<bool> SettleBookingIncomeAsync(int bookingId, DateTime now);
    Task<bool> ReverseBookingIncomeAsync(int bookingId, DateTime now);
}

