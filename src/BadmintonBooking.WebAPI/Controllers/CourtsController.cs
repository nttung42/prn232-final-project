using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Courts;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourtsController : ControllerBase
{
    private readonly ICourtService _courtService;

    public CourtsController(ICourtService courtService)
    {
        _courtService = courtService;
    }

    /// <summary>
    /// [GET ALL + OData] Lấy danh sách sân con (Hỗ trợ $filter, $orderby, $top, $skip)
    /// </summary>
    [HttpGet]
    [EnableQuery]
    [AllowAnonymous]
    public IQueryable<CourtDto> GetAll([FromQuery] int? facilityId = null)
    {
        return _courtService.GetAllQuery(facilityId);
    }

    /// <summary>
    /// [GET BY ID] Xem chi tiết 1 sân cầu lông
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CourtDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CourtDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _courtService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [POST] Thêm sân con mới vào cơ sở
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<CourtDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CourtDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveCourtDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CourtDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _courtService.CreateAsync(currentUserId, role, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// [PUT] Cập nhật thông tin sân con
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<CourtDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CourtDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCourtDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CourtDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _courtService.UpdateAsync(id, currentUserId, role, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [DELETE] Xóa sân con
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _courtService.DeleteAsync(id, currentUserId, role);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

