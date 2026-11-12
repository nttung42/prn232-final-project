using System.Security.Cryptography;
using System.Text;
using BadmintonBooking.BLL.DTOs.Payments;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    /// <summary>Đường dẫn webhook SePay. Được miễn kiểm tra header CSRF (SePay không gửi header này) và xác thực bằng API key.</summary>
    public const string SepayWebhookPath = "/api/payments/sepay/webhook";

    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;

    public PaymentsController(IPaymentService paymentService, IConfiguration config)
    {
        _paymentService = paymentService;
        _config = config;
    }

    /// <summary>
    /// Webhook nhận biến động số dư từ SePay. Xác thực header Authorization: Apikey &lt;key&gt;.
    /// Trả đúng {"success": true} khi xử lý xong; lỗi server trả 500 để SePay gửi lại.
    /// </summary>
    [HttpPost("sepay/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookDto payload)
    {
        // Tạm tắt: luồng hiện tại dùng QR mặc định + đối soát thủ công. Bật lại bằng Sepay:Enabled=true.
        if (!string.Equals(_config["Sepay:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!IsValidSepayApiKey())
            return Unauthorized(new { success = false, message = "API key không hợp lệ." });

        await _paymentService.ProcessSepayWebhookAsync(payload);
        return Ok(new { success = true });
    }

    /// <summary>Tạo hoặc lấy lại yêu cầu chuyển khoản cho đơn đặt sân (kèm mã và QR)</summary>
    [HttpPost("booking/{bookingId}")]
    [Authorize(Roles = AppRoles.Player)]
    public async Task<IActionResult> CreateBookingPayment(int bookingId)
    {
        try
        {
            return Ok(await _paymentService.CreateBookingPaymentAsync(User.GetUserId(), bookingId));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    /// <summary>Trạng thái yêu cầu thanh toán (người chơi dùng để chờ xác nhận)</summary>
    [HttpGet("{code}/status")]
    [Authorize]
    public async Task<IActionResult> GetStatus(string code)
    {
        try
        {
            return Ok(await _paymentService.GetStatusAsync(User.GetUserId(), code));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { success = false, message = ex.Message }); }
    }

    /// <summary>
    /// Yêu cầu chuyển khoản đang chờ xác nhận. Admin: tất cả; Provider: đơn của cơ sở mình.
    /// </summary>
    [HttpGet("review")]
    [Authorize(Roles = "Admin,Provider")]
    public async Task<IActionResult> GetPendingReview()
    {
        var role = User.GetUserRole();
        return Ok(await _paymentService.GetPendingReviewAsync(User.GetUserId(), role));
    }

    /// <summary>
    /// Xác nhận đã nhận đủ tiền theo mã chuyển khoản (đối soát thủ công). Nạp ví: chỉ Admin. Đơn: Provider của cơ sở hoặc Admin.
    /// </summary>
    [HttpPost("{code}/confirm")]
    [Authorize(Roles = "Admin,Provider")]
    public async Task<IActionResult> ConfirmManually(string code)
    {
        try
        {
            await _paymentService.ConfirmManuallyAsync(code, User.GetUserId(), User.GetUserRole());
            return Ok(new { success = true, message = "Đã xác nhận thanh toán." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    /// <summary>Từ chối đơn đặt sân đang chờ; nếu đã trừ ví thì hoàn tiền về ví.</summary>
    [HttpPost("{code}/reject")]
    [Authorize(Roles = "Admin,Provider")]
    public async Task<IActionResult> Reject(string code)
    {
        try
        {
            await _paymentService.RejectAsync(code, User.GetUserId(), User.GetUserRole());
            return Ok(new { success = true, message = "Đã từ chối đơn." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    /// <summary>[ADMIN] Giao dịch ngân hàng gần đây, kể cả giao dịch không khớp mã để đối chiếu</summary>
    [HttpGet("admin/bank-transactions")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetBankTransactions([FromQuery] int take = 50)
    {
        return Ok(await _paymentService.GetBankTransactionsAsync(take));
    }

    private bool IsValidSepayApiKey()
    {
        var configured = _config["Sepay:ApiKey"];
        if (string.IsNullOrWhiteSpace(configured)) return false; // Chưa cấu hình: từ chối mọi webhook
        if (!Request.Headers.TryGetValue("Authorization", out var header)) return false;

        var expected = Encoding.UTF8.GetBytes($"Apikey {configured}");
        var actual = Encoding.UTF8.GetBytes(header.ToString());
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
