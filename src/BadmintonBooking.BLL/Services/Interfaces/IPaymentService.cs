using BadmintonBooking.BLL.DTOs.Payments;


namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>Tạo (hoặc lấy lại) yêu cầu thanh toán chuyển khoản cho đơn đặt sân của chính người chơi.</summary>
    Task<PaymentQrDto> CreateBookingPaymentAsync(int userId, int bookingId);

    /// <summary>Trạng thái yêu cầu thanh toán (chỉ chủ yêu cầu).</summary>
    Task<PaymentStatusDto> GetStatusAsync(int userId, string code);

    /// <summary>Xử lý webhook từ SePay. Trả về kết quả xử lý (không ném lỗi với giao dịch đã xử lý).</summary>
    Task<string> ProcessSepayWebhookAsync(SepayWebhookDto webhook);

    /// <summary>Hết hạn các yêu cầu chưa thanh toán và hủy đơn giữ chỗ quá hạn.</summary>
    Task<int> ExpirePaymentsAsync(DateTime utcNow);

    /// <summary>Các yêu cầu chuyển khoản đang chờ xác nhận (Admin: tất cả; Provider: đơn của cơ sở mình).</summary>
    Task<List<PaymentReviewDto>> GetPendingReviewAsync(int actorUserId, string actorRole);

    /// <summary>Admin/Provider xác nhận đã nhận đủ tiền cho yêu cầu chuyển khoản theo mã.</summary>
    Task ConfirmManuallyAsync(string code, int actorUserId, string actorRole);

    /// <summary>Admin/Provider từ chối đơn đặt sân đang chờ; hoàn tiền về ví nếu đã trừ ví.</summary>
    Task RejectAsync(string code, int actorUserId, string actorRole);

    /// <summary>Lịch sử đối soát đã xử lý (Admin: tất cả; Provider: cơ sở của mình).</summary>
    Task<List<PaymentHistoryDto>> GetHistoryAsync(int actorUserId, string actorRole);

    /// <summary>Tổng hợp doanh thu và phí nền tảng.</summary>
    Task<PaymentSummaryDto> GetSummaryAsync(int actorUserId, string actorRole);

    /// <summary>Danh sách giao dịch ngân hàng gần đây cho Admin đối chiếu.</summary>
    Task<List<BankTransactionDto>> GetBankTransactionsAsync(int take);
}
