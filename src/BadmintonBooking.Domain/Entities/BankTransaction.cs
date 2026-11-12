using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

/// <summary>
/// Bản ghi biến động số dư nhận từ webhook (SePay). SepayId là khóa chống xử lý trùng.
/// </summary>
public class BankTransaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Mã giao dịch của SePay (id trong payload). Duy nhất để chống nhận trùng.</summary>
    public long SepayId { get; set; }

    [MaxLength(100)]
    public string? Gateway { get; set; }

    [MaxLength(50)]
    public string? AccountNumber { get; set; }

    public DateTime TransactionDate { get; set; }

    [MaxLength(1000)]
    public string? Content { get; set; }

    [MaxLength(10)]
    public string TransferType { get; set; } = "in";

    [Column(TypeName = "decimal(18,2)")]
    public decimal TransferAmount { get; set; }

    [MaxLength(32)]
    public string? MatchedCode { get; set; }

    public int? PaymentOrderId { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
