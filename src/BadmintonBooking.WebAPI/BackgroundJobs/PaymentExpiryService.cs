using BadmintonBooking.BLL.Services.Interfaces;

namespace BadmintonBooking.WebAPI.BackgroundJobs;

/// <summary>Mỗi phút: hết hạn yêu cầu chưa thanh toán, hoàn tiền chuyển thiếu, hủy đơn giữ chỗ quá hạn.</summary>
public class PaymentExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentExpiryService> _logger;

    public PaymentExpiryService(IServiceScopeFactory scopeFactory, ILogger<PaymentExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                var affected = await payments.ExpirePaymentsAsync(DateTime.UtcNow);
                if (affected > 0) _logger.LogInformation("Đã xử lý hết hạn {Count} thanh toán/đơn giữ chỗ.", affected);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Lỗi khi xử lý hết hạn thanh toán.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
