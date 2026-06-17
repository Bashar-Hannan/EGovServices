using EGovServices.Application.Features.Admin.Commands.ToggleUserStatus;
using EGovServices.Application.Features.Admin.Queries.GetAdminRequestDetails;
using EGovServices.Application.Features.Admin.Queries.GetAdminRequests;
using EGovServices.Application.Features.Admin.Queries.GetAdminUsers;
using EGovServices.Application.Features.Admin.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>قائمة الطلبات مع فلترة وتقسيم صفحات</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? status,
        [FromQuery] Guid? serviceId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new GetAdminRequestsQuery
        {
            Status    = status,
            ServiceId = serviceId,
            FromDate  = fromDate,
            ToDate    = toDate,
            Page      = page,
            PageSize  = pageSize
        });
        return result.Match<IActionResult>(
            onSuccess: data  => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>تفاصيل طلب واحد</summary>
    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequestDetails(Guid id)
    {
        var result = await mediator.Send(new GetAdminRequestDetailsQuery(id));
        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>إحصائيات لوحة التحكم</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await mediator.Send(new GetDashboardStatsQuery());
        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>قائمة المستخدمين مع بحث وفلترة</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new GetAdminUsersQuery
        {
            SearchTerm = search,
            IsActive   = isActive,
            Page       = page,
            PageSize   = pageSize
        });
        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>تفعيل / إيقاف حساب مستخدم</summary>
    [HttpPatch("users/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var result = await mediator.Send(new ToggleUserStatusCommand(id));
        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}
