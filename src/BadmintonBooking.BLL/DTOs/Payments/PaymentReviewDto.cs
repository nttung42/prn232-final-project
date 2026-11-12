namespace BadmintonBooking.BLL.DTOs.Payments;

/// <summary>Yêu cầu chuyển khoản đang chờ admin/provider đối chiếu và xác nhận.</summary>
public class PaymentReviewDto
{
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public string? FacilityName { get; set; }
    public string? PlayerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
