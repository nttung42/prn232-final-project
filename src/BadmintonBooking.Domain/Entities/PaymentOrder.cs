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

    public virtual User? User { get; set; }
    public virtual Booking? Booking { get; set; }
}
