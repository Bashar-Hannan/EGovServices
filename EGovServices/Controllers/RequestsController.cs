using EGovServices.Application.Features.Requests.Queries.GetMyRequests;
using EGovServices.Application.Features.Requests.Queries.GetRequestStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// يرجع كل طلبات المواطن الحالي — تُستخدم في شاشة "معاملاتي".
    /// الفرونت يفلتر بـ StatusCategory محلياً لكل تبويب
    /// (InProgress / Completed / Cancelled).
    ///
    /// GET /api/requests/mine
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyRequestsQuery(userId), cancellationToken);

        return Ok(new { success = true, data = result.Value });
    }

    /// <summary>
    /// يرجع الحالة الحالية + التاريخ الكامل لطلب معين عبر رقم المرجع.
    /// GET /api/requests/{referenceNumber}/status
    /// </summary>
    [HttpGet("{referenceNumber}/status")]
    public async Task<IActionResult> GetStatus(string referenceNumber, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);

        var result = await _mediator.Send(
            new GetRequestStatusQuery(referenceNumber, userId),
            cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(result.Value);
    }
}
