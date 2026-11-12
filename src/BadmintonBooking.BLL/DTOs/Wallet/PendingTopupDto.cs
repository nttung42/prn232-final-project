namespace BadmintonBooking.BLL.DTOs.Wallet;

public class PendingTopupDto
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
