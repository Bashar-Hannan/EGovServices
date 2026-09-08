using EGovServices.Application.Features.Admin.Commands.AddCitizen;
using EGovServices.Application.Features.Admin.Commands.AddElectricityBill;
using EGovServices.Application.Features.Admin.Commands.AddTrafficViolation;
using EGovServices.Application.Features.Admin.Commands.CancelElectricityBill;
using EGovServices.Application.Features.Admin.Commands.DeleteTrafficViolation;
using EGovServices.Application.Features.Admin.Commands.ToggleServiceStatus;
using EGovServices.Application.Features.Admin.Commands.ToggleUserStatus;
using EGovServices.Application.Features.Admin.Commands.UpsertMedicalRecord;
using EGovServices.Application.Features.Admin.Queries;
using EGovServices.Application.Features.Admin.Queries.GetAdminRequestDetails;
using EGovServices.Application.Features.Admin.Queries.GetAdminRequests;
using EGovServices.Application.Features.Admin.Queries.GetAdminUsers;
using EGovServices.Application.Features.Admin.Queries.GetAppointmentServices;
using EGovServices.Application.Features.Admin.Queries.GetDashboardStats;
using EGovServices.Application.Features.Admin.Queries.GetMedicalRecordByNationalNumber;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
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
            Status = status,
            ServiceId = serviceId,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        });
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>تفاصيل طلب واحد</summary>
    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequestDetails(Guid id)
    {
        var result = await mediator.Send(new GetAdminRequestDetailsQuery(id));
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>إحصائيات لوحة التحكم</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await mediator.Send(new GetDashboardStatsQuery());
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
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
            IsActive = isActive,
            Page = page,
            PageSize = pageSize
        });
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>تفعيل / إيقاف حساب مستخدم</summary>
    [HttpPatch("users/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var result = await mediator.Send(new ToggleUserStatusCommand(id));
        return result.Match(
            onSuccess: data => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    [HttpPost("citizens")]
    public async Task<IActionResult> AddCitizen([FromBody] AddCitizenCommand command)
    {
        var result = await mediator.Send(command);
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    [HttpGet("citizens")]
    public async Task<IActionResult> GetCitizens()
    {
        var result = await mediator.Send(new GetCitizensQuery());
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    [HttpGet("citizens/{nationalNumber}")]
    public async Task<IActionResult> GetCitizen(string nationalNumber)
    {
        var result = await mediator.Send(new GetCitizenQuery(nationalNumber));
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ✅ جديد — تفعيل / تعطيل الخدمة الحكومية
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>تفعيل / تعطيل خدمة حكومية</summary>
    [HttpPatch("services/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleServiceStatus(Guid id)
    {
        var result = await mediator.Send(new ToggleServiceStatusCommand(id));
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ✅ جديد — إضافة فاتورة كهرباء لمواطن
    // ═══════════════════════════════════════════════════════════════════════

    [HttpPost("electricity-bills")]
    public async Task<IActionResult> AddElectricityBill([FromBody] AddElectricityBillCommand command)
    {
        var result = await mediator.Send(command);
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>إلغاء فاتورة كهرباء (إلغاء ناعم — تُبقى بالجدول بحالة Cancelled). مرفوض إن كانت مدفوعة مسبقاً</summary>
    [HttpPatch("electricity-bills/{id:guid}/cancel")]
    public async Task<IActionResult> CancelElectricityBill(Guid id)
    {
        var result = await mediator.Send(new CancelElectricityBillCommand(id));
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ✅ جديد — إضافة مخالفة مرورية لمواطن
    // ═══════════════════════════════════════════════════════════════════════

    [HttpPost("traffic-violations")]
    public async Task<IActionResult> AddTrafficViolation([FromBody] AddTrafficViolationCommand command)
    {
        var result = await mediator.Send(command);
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    /// <summary>حذف مخالفة مرورية — مرفوض إن كانت مدفوعة مسبقاً</summary>
    [HttpDelete("traffic-violations/{id:guid}")]
    public async Task<IActionResult> DeleteTrafficViolation(Guid id)
    {
        var result = await mediator.Send(new DeleteTrafficViolationCommand(id));
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ✅ جديد — إدارة الملف الطبي لمواطن (عرض + إنشاء/تعديل — Upsert)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>جلب الملف الطبي الحالي للمواطن (null إن لم يكن موجوداً بعد — وليس خطأً)</summary>
    [HttpGet("medical-records/{nationalNumber}")]
    public async Task<IActionResult> GetMedicalRecord(string nationalNumber)
    {
        var result = await mediator.Send(new GetMedicalRecordByNationalNumberQuery(nationalNumber));
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => NotFound(new { success = false, message = error }));
    }

    /// <summary>إنشاء أو تعديل الملف الطبي للمواطن (Upsert — سجل واحد فقط لكل مواطن)</summary>
    [HttpPut("medical-records/{nationalNumber}")]
    public async Task<IActionResult> UpsertMedicalRecord(
        string nationalNumber, [FromBody] UpsertMedicalRecordRequestBody body)
    {
        var command = new UpsertMedicalRecordCommand
        {
            CitizenNationalNumber = nationalNumber,
            BloodType = body.BloodType,
            HeightCm = body.HeightCm,
            WeightKg = body.WeightKg,
            Allergies = body.Allergies,
            ChronicDiseases = body.ChronicDiseases
        };

        var result = await mediator.Send(command);
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
    [HttpGet("services/appointment")]
    public async Task<IActionResult> GetAppointmentServices()
    {
        var result = await mediator.Send(new GetAppointmentServicesQuery());
        return result.Match<IActionResult>(
            onSuccess: data => Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }
}

/// <summary>Body لطلب PUT — بدون CitizenNationalNumber لأنه يأتي من الـ route.</summary>
public sealed record UpsertMedicalRecordRequestBody
{
    public required string BloodType { get; init; }
    public required int HeightCm { get; init; }
    public required int WeightKg { get; init; }
    public required string Allergies { get; init; }
    public required string ChronicDiseases { get; init; }
}
