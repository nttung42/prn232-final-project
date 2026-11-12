using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Facilities;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.Domain.Constants;
using BadmintonBooking.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacilitiesController : ControllerBase
{
    private readonly IFacilityService _facilityService;

    public FacilitiesController(IFacilityService facilityService)
    {
        _facilityService = facilityService;
    }

    /// <summary>
    /// [GET ALL + OData] Lấy danh sách cơ sở cầu lông (Hỗ trợ $filter, $orderby, $top, $skip)
    /// </summary>
    [HttpGet]
    [EnableQuery]
    [AllowAnonymous]
    public IQueryable<FacilityDto> GetAll([FromQuery] bool? onlyApproved = null)
    {
        // By default, if anonymous or role is Player, show approved facilities
        var role = User.GetUserRole();
        bool filterApproved = onlyApproved ?? (role != AppRoles.Admin && role != AppRoles.Provider);
        var query = _facilityService.GetAllQuery(filterApproved);

        // Provider chỉ thấy cơ sở của chính mình (Admin thấy tất cả)
        if (role == AppRoles.Provider)
        {
            var currentUserId = User.GetUserId();
            query = query.Where(f => f.ProviderId == currentUserId);
        }

        return query;
    }

    /// <summary>
    /// [GET BY ID] Lấy chi tiết cơ sở cầu lông
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _facilityService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [POST] Provider đăng ký cơ sở cầu lông mới
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFacilityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<FacilityDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var result = await _facilityService.CreateAsync(currentUserId, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// [PUT] Cập nhật thông tin cơ sở cầu lông
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFacilityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<FacilityDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _facilityService.UpdateAsync(id, currentUserId, role, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [PATCH] Admin phê duyệt hoặc đình chỉ cơ sở
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FacilityDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFacilityStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<FacilityDto>.Fail("Dữ liệu không hợp lệ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _facilityService.UpdateStatusAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [DELETE] Xóa cơ sở cầu lông
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{AppRoles.Provider},{AppRoles.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.GetUserId();
        var role = User.GetUserRole();
        var result = await _facilityService.DeleteAsync(id, currentUserId, role);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

