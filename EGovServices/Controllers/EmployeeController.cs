using System.Security.Claims;
using EGovServices.Application.Features.Admin.Queries.GetAdminRequests;
using EGovServices.Application.Features.Employee.Commands.AddProcessingNote;
using EGovServices.Application.Features.Employee.Commands.UpdateRequestStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Employee,Admin")]   // Admin يملك صلاحيات الموظف أيضاً
public sealed class EmployeeController(IMediator mediator) : ControllerBase
{
    /// <summary>الطلبات المعلقة وقيد المراجعة</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetPendingRequests(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // الموظف يرى فقط Pending و UnderReview افتراضياً
        var effectiveStatus = status ?? "Pending";

        var result = await mediator.Send(new GetAdminRequestsQuery
        {
            Status   = effectiveStatus,
            Page     = page,
            PageSize = pageSize
        });
        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>تغيير حالة طلب</summary>
    [HttpPatch("requests/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest body)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new UpdateRequestStatusCommand
        {
            RequestId       = id,
            ChangedByUserId = userId,
            NewStatus       = body.NewStatus,
            RejectionReason = body.RejectionReason,
            ProcessingNotes = body.ProcessingNotes
        });
        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>إضافة ملاحظة معالجة على طلب</summary>
    [HttpPost("requests/{id:guid}/note")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddNoteRequest body)
    {
        var result = await mediator.Send(new AddProcessingNoteCommand
        {
            RequestId = id,
            Note      = body.Note
        });
        return result.Match(
            onSuccess: ()    => (IActionResult)Ok(new { success = true, message = "تمت إضافة الملاحظة" }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}

// ─── Request Body DTOs ────────────────────────────────────────────────────────
public sealed record UpdateStatusRequest(
    string NewStatus,
    string? RejectionReason,
    string? ProcessingNotes);

public sealed record AddNoteRequest(string Note);
