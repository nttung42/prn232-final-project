using BadmintonBooking.BLL.DTOs.Bookings;
using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// [GET ALL + OData + CSV/XML/JSON] Lấy danh sách đơn đặt sân (Player thấy đơn của mình, Provider thấy đơn cơ sở mình, Admin thấy toàn bộ)
    /// </summary>
    [HttpGet]
    [EnableQuery]
    [Authorize]
    [Produces("application/json", "application/xml", "text/csv")]
    public IQueryable<BookingDto> GetAll()
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        return _bookingService.GetAllQuery(currentUserId, role);
    }

    /// <summary>
    /// [GET BY ID] Xem chi tiết 1 đơn đặt sân
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _bookingService.GetByIdAsync(id, currentUserId, role);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [POST] Player đặt sân mới (Kiểm tra trùng lịch, tính TotalPrice, chuyển trạng thái Pending)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Player},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<BookingDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var result = await _bookingService.CreateAsync(currentUserId, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// [PUT] Cập nhật ghi chú hoặc chuyển trạng thái đơn đặt sân (Confirmed, Cancelled, Completed)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBookingStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<BookingDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _bookingService.UpdateStatusAsync(id, currentUserId, role, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [DELETE] Xóa đơn đặt sân (Chỉ cho phép khi đơn ở trạng thái Pending hoặc Cancelled)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _bookingService.DeleteAsync(id, currentUserId, role);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

