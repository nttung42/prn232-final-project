using System.Web;
using BadmintonBooking.BLL.Services.Interfaces;

namespace BadmintonBooking.BLL.Services.Implementations;

public class VietQrService : IVietQrService
{
    public string GenerateVietQrUrl(string bankId, string accountNo, string accountName, decimal amount, string transferContent)
    {
        // Sử dụng QuickLink API chuẩn của VietQR / Napas247
        // Format: https://img.vietqr.io/image/<BANK_ID>-<ACCOUNT_NO>-compact2.png?amount=<AMOUNT>&addInfo=<CONTENT>&accountName=<NAME>
        var encodedContent = HttpUtility.UrlEncode(transferContent);
        var encodedName = HttpUtility.UrlEncode(accountName);
        var amountInt = (long)Math.Round(amount, 0);

        return $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={amountInt}&addInfo={encodedContent}&accountName={encodedName}";
    }

    public string GenerateReferenceCode(string prefix = "CSIDE")
    {
        var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
        var random = new Random().Next(100, 999);
        return $"{prefix}_{timestamp}_{random}";
    }
}

