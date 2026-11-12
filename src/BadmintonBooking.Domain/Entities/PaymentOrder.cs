using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

/// <summary>
/// Yêu cầu thanh toán chuyển khoản. Mỗi yêu cầu có mã riêng (Code) đặt trong nội dung chuyển khoản,
/// dùng để khớp với webhook của ngân hàng/SePay.
/// </summary>
public class PaymentOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Purpose { get; set; } = string.Empty;   // TopUp | Booking

    public int UserId { get; set; }

    public int? BookingId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>Người (Admin/Provider) xác nhận đã nhận tiền hoặc duyệt đơn.</summary>
    public int? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Phí nền tảng thu từ đơn đã duyệt (0 nếu chưa duyệt hoặc đã hoàn).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PlatformFee { get; set; }

    /// <summary>Số tiền chủ sân nhận từ đơn đã duyệt (giá sân trừ phí).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProviderAmount { get; set; }

    public DateTime? SettledAt { get; set; }
    public DateTime? ReversedAt { get; set; }

    public virtual User? User { get; set; }
    public virtual Booking? Booking { get; set; }
}
