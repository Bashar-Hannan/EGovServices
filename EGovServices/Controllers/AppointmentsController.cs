using System.Security.Claims;
using EGovServices.Application.Features.Admin.Commands.CreateAppointmentSlot;
using EGovServices.Application.Features.Citizen.Commands.BookAppointment;
using EGovServices.Application.Features.Citizen.Queries.GetAvailableSlots;
using EGovServices.Application.Features.Employee.Commands.ReviewAppointmentDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGovServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppointmentsController(IMediator mediator) : ControllerBase
{
    // ════════════════════════════════════════════════════════════════════════
    // Admin — إدارة المواعيد المتاحة
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>إنشاء موعد جديد متاح للحجز</summary>
    [HttpPost("slots")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSlot([FromBody] CreateSlotRequest body)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new CreateAppointmentSlotCommand
        {
            GovernmentServiceId = body.GovernmentServiceId,
            CreatedByAdminId    = adminId,
            SlotDate            = body.SlotDate,
            StartTime           = body.StartTime,
            EndTime             = body.EndTime,
            TotalSeats          = body.TotalSeats
        });

        return result.Match(
            onSuccess: data  => (IActionResult)CreatedAtAction(
                nameof(GetAvailableSlots),
                new { serviceId = body.GovernmentServiceId },
                new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Citizen — عرض المواعيد وحجز موعد
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>المواعيد المتاحة لخدمة معينة</summary>
    [HttpGet("slots/{serviceId:guid}")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> GetAvailableSlots(
        Guid serviceId,
        [FromQuery] DateOnly? fromDate)
    {
        var result = await mediator.Send(new GetAvailableSlotsQuery
        {
            GovernmentServiceId = serviceId,
            FromDate            = fromDate
        });

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>حجز موعد وتقديم الملفات</summary>
    [HttpPost("book")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest body)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new BookAppointmentCommand
        {
            UserId              = userId,
            GovernmentServiceId = body.GovernmentServiceId,
            AppointmentSlotId   = body.AppointmentSlotId,
            FormData            = body.FormData
        });

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Employee — مراجعة الملفات وتأكيد أو إلغاء الموعد
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>مراجعة ملفات المواطن — تأكيد أو إلغاء الموعد</summary>
    [HttpPatch("requests/{requestId:guid}/review")]
    [Authorize(Roles = "Employee,Admin")]
    public async Task<IActionResult> ReviewDocuments(
        Guid requestId,
        [FromBody] ReviewDocumentsRequest body)
    {
        var employeeId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var result = await mediator.Send(new ReviewAppointmentDocumentsCommand
        {
            RequestId          = requestId,
            ReviewedByUserId   = employeeId,
            IsApproved         = body.IsApproved,
            RejectionReason    = body.RejectionReason,
            Notes              = body.Notes
        });

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}

// ─── Request Body DTOs ────────────────────────────────────────────────────────
public sealed record CreateSlotRequest(
    Guid      GovernmentServiceId,
    DateOnly  SlotDate,
    TimeOnly  StartTime,
    TimeOnly  EndTime,
    int       TotalSeats);

public sealed record BookAppointmentRequest(
    Guid   GovernmentServiceId,
    Guid   AppointmentSlotId,
    string FormData);

public sealed record ReviewDocumentsRequest(
    bool    IsApproved,
    string? RejectionReason,
    string? Notes);
