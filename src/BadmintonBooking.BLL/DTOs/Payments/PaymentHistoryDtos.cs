namespace BadmintonBooking.BLL.DTOs.Payments;

/// <summary>Một dòng lịch sử đối soát / thanh toán đã xử lý.</summary>
public class PaymentHistoryDto
{
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public string? FacilityName { get; set; }
    public string? PlayerName { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal ProviderAmount { get; set; }
    public DateTime? ReversedAt { get; set; }
}

/// <summary>Tổng hợp doanh thu: Admin xem toàn sàn (phí nền tảng), Provider xem phần của cơ sở mình.</summary>
public class PaymentSummaryDto
{
    public int SettledCount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal ProviderAmount { get; set; }
}
