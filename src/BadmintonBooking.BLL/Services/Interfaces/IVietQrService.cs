namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IVietQrService
{
    string GenerateVietQrUrl(string bankId, string accountNo, string accountName, decimal amount, string transferContent);
    string GenerateReferenceCode(string prefix = "CSIDE");
}

