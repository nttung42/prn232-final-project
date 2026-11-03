namespace BadmintonBooking.BLL.DTOs.Wallet;

public class WalletDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public List<WalletTransactionDto> RecentTransactions { get; set; } = new();
}

