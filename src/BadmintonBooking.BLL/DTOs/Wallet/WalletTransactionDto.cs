namespace BadmintonBooking.BLL.DTOs.Wallet;

public class WalletTransactionDto
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public string? QrUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

