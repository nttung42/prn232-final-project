using System.Security.Claims;
using BadmintonBooking.BLL.DTOs.Wallet;
using BadmintonBooking.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>
    /// Lấy thông tin Ví CourtSide của người dùng hiện tại (tự khởi tạo nếu chưa có)
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<WalletDto>> GetMyWallet()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var wallet = await _walletService.GetOrCreateWalletByUserIdAsync(userId);
        return Ok(wallet);
    }

    /// <summary>
    /// Lấy lịch sử biến động số dư và giao dịch nạp/rút/thanh toán của người dùng
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<WalletTransactionDto>>> GetMyTransactions()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var transactions = await _walletService.GetTransactionsByUserIdAsync(userId);
        return Ok(transactions);
    }

    /// <summary>
    /// [ADMIN] Danh sách các giao dịch nạp tiền đang chờ đối soát
    /// </summary>
    [HttpGet("admin/pending-topups")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PendingTopupDto>>> GetPendingTopups()
    {
        return Ok(await _walletService.GetPendingTopupsAsync());
    }

    /// <summary>
    /// Khởi tạo giao dịch nạp tiền vào ví qua VietQR Napas 247
    /// </summary>
    [HttpPost("topup")]
    public async Task<ActionResult<TopupResponseDto>> CreateTopup([FromBody] TopupRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var response = await _walletService.CreateTopupAsync(userId, request);
        return Ok(response);
    }

    /// <summary>
    /// Thanh toán đơn đặt sân bằng số dư Ví CourtSide
    /// </summary>
    [HttpPost("pay-booking")]
    public async Task<IActionResult> PayBookingByWallet([FromBody] PayBookingByWalletDto request)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        try
        {
            var success = await _walletService.PayBookingByWalletAsync(userId, request.BookingId);
            return Ok(new { success, message = "Thanh toán cước sân qua Ví CourtSide thành công!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

