using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BadmintonBooking.BLL.Services.Implementations;

/// <summary>Quy ước mã thanh toán: "CSIDE" + 8 ký tự hex (A-F, 0-9). Mã đặt trong nội dung chuyển khoản.</summary>
public static class PaymentCodes
{
    public const string Prefix = "CSIDE";
    public static readonly Regex Pattern = new(@"CSIDE[A-F0-9]{8}", RegexOptions.Compiled);

    public static string New() => Prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
}
