using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.TimeSlots;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeSlotsController : ControllerBase
{
    private readonly ITimeSlotService _timeSlotService;

    public TimeSlotsController(ITimeSlotService timeSlotService)
    {
        _timeSlotService = timeSlotService;
    }

    /// <summary>
    /// [GET ALL + OData] Lấy danh sách khung giờ & giá (Hỗ trợ $filter, $orderby, $top, $skip)
    /// </summary>
    [HttpGet]
    [EnableQuery]
    [AllowAnonymous]
    public IQueryable<TimeSlotDto> GetAll([FromQuery] int? facilityId = null)
    {
        return _timeSlotService.GetAllQuery(facilityId);
    }

    /// <summary>
    /// [GET BY ID] Lấy chi tiết 1 khung giờ
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _timeSlotService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kiểm tra danh sách khung giờ còn trống hoặc đã đặt theo ngày và sân
    /// </summary>
    [HttpGet("available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableTimeSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableTimeSlotDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int facilityId, 
        [FromQuery] int courtId, 
        [FromQuery] DateTime playDate)
    {
        var result = await _timeSlotService.GetAvailableSlotsAsync(facilityId, courtId, playDate);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [POST] Tạo khung giờ & giá mới cho cơ sở
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveTimeSlotDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<TimeSlotDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _timeSlotService.CreateAsync(currentUserId, role, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// [PUT] Cập nhật khung giờ & giá
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveTimeSlotDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<TimeSlotDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _timeSlotService.UpdateAsync(id, currentUserId, role, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [DELETE] Xóa khung giờ
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _timeSlotService.DeleteAsync(id, currentUserId, role);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

