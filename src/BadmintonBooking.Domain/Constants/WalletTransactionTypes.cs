namespace BadmintonBooking.Domain.Constants;

public static class WalletTransactionTypes
{
    public const string Deposit = "Deposit";
    public const string Withdraw = "Withdraw";
    public const string BookingPayment = "BookingPayment";
    public const string BookingRefund = "BookingRefund";
    public const string BookingIncome = "BookingIncome";             // Doanh thu chủ sân nhận khi đơn được duyệt (đã trừ phí nền tảng)
    public const string BookingIncomeReversal = "BookingIncomeReversal"; // Thu hồi doanh thu khi đơn đã duyệt bị hủy
}

