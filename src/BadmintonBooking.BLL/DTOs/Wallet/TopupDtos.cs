using System.ComponentModel.DataAnnotations;

namespace BadmintonBooking.BLL.DTOs.Wallet;

public class TopupRequestDto
{
    [Required]
    [Range(10000, 50000000, ErrorMessage = "Số tiền nạp tối thiểu là 10.000đ và tối đa là 50.000.000đ")]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}

public class TopupResponseDto
{
    public int TransactionId { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string QrUrl { get; set; } = string.Empty;
    public string BankId { get; set; } = "MB";
    public string BankName { get; set; } = "MB Bank";
    public string AccountNo { get; set; } = "999988887777";
    public string AccountName { get; set; } = "CTY CP COURTSIDE VIETNAM";
    public string TransferContent { get; set; } = string.Empty;
}

public class PayBookingByWalletDto
{
    [Required]
    public int BookingId { get; set; }
}

