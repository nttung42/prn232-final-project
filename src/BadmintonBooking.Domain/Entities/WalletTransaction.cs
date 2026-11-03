using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BadmintonBooking.Domain.Entities;

public class WalletTransaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey(nameof(Wallet))]
    public int WalletId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression("^(Deposit|Withdraw|BookingPayment|BookingRefund)$")]
    public string TransactionType { get; set; } = "Deposit";

    [Required]
    [RegularExpression("^(Pending|Completed|Failed|Cancelled)$")]
    public string Status { get; set; } = "Pending";

    [MaxLength(250)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? ReferenceCode { get; set; }

    [MaxLength(500)]
    public string? QrUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Wallet? Wallet { get; set; }
}
