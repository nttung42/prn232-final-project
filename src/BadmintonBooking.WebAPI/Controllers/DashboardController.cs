using BadmintonBooking.BLL.DTOs.Common;
using BadmintonBooking.BLL.DTOs.Dashboard;
using BadmintonBooking.BLL.Services.Interfaces;
using BadmintonBooking.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// [UC-A03] Thống kê tổng quan sàn cho Admin
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _dashboardService.GetSystemStatsAsync();
        return Ok(result);
    }
}

