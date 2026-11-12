namespace BadmintonBooking.BLL.DTOs.Payments;

/// <summary>Payload webhook SePay gửi khi có biến động số dư (xem docs/04).</summary>
public class SepayWebhookDto
{
    public long Id { get; set; }
    public string? Gateway { get; set; }
    public string? TransactionDate { get; set; }   // "YYYY-MM-DD HH:mm:ss", giờ Việt Nam
    public string? AccountNumber { get; set; }
    public string? SubAccount { get; set; }
    public string? Code { get; set; }
    public string? Content { get; set; }
    public string? TransferType { get; set; }      // in | out
    public string? Description { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal Accumulated { get; set; }
    public string? ReferenceCode { get; set; }
}

public class PaymentQrDto
{
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string QrUrl { get; set; } = string.Empty;
    public string BankId { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
}

public class PaymentStatusDto
{
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class BankTransactionDto
{
    public int Id { get; set; }
    public long SepayId { get; set; }
    public string? Gateway { get; set; }
    public string? AccountNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Content { get; set; }
    public decimal TransferAmount { get; set; }
    public string? MatchedCode { get; set; }
    public int? PaymentOrderId { get; set; }
    public string? Note { get; set; }
    public DateTime ReceivedAt { get; set; }
}
