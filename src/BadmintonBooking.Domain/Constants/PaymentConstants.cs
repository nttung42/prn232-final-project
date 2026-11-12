namespace BadmintonBooking.Domain.Constants;

public static class PaymentPurposes
{
    public const string TopUp = "TopUp";
    public const string Booking = "Booking";
}

public static class PaymentOrderStatuses
{
    public const string Pending = "Pending";
    public const string Held = "Held";             // Đã trừ ví, chờ chủ sân xác nhận
    public const string Paid = "Paid";
    public const string Underpaid = "Underpaid";   // Đã nhận tiền nhưng thiếu so với số tiền yêu cầu
    public const string Expired = "Expired";       // Hết hạn chưa nhận tiền
}
