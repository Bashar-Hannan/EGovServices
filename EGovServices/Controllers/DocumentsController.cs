using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.Features.CivilRecord;
using EGovServices.Application.Features.ClearanceCertificate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.API.Controllers;

/// <summary>
/// نقطتان موحَّدتان فقط لكل خدمات توليد الوثائق (PDF) في المنصة:
///
///   POST /api/documents/process/{requestId}   ← يولّد الوثيقة المناسبة تلقائياً
///   GET  /api/documents/download/{requestId}  ← يحمّل أي وثيقة تم توليدها
///
/// الفرونت يحتاج فقط requestId — لا يحتاج معرفة نوع الخدمة أو استدعاء
/// endpoint مختلف لكل نوع وثيقة (clearance / civilrecord / أي نوع مستقبلي).
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController(IAppDbContext context, IMediator mediator)
    : ControllerBase
{
    // ════════════════════════════════════════════════════════════════
    // خريطة التوجيه: GovernmentServiceId → نوع المعالجة المطلوبة
    //
    // عند إضافة خدمة جديدة تولّد PDF، فقط أضف سطراً واحداً هنا —
    // لا حاجة لإنشاء Controller أو Endpoint جديد على الإطلاق.
    // ════════════════════════════════════════════════════════════════
    private static readonly Dictionary<Guid, DocumentKind> ServiceDocumentMap = new()
    {
        // شهادة عدم المحكومية ("الهوية الشخصية" في تسمية قاعدة البيانات الحالية)
        [Guid.Parse("013B2088-EC8E-4C9E-A3B8-C9CA793C12D0")] = DocumentKind.ClearanceCertificate,

        // إخراج قيد فردي مدني
        [Guid.Parse("F47AC10B-58CC-4372-A567-0E02B2C3D479")] = DocumentKind.CivilRecord,
    };

    private enum DocumentKind
    {
        ClearanceCertificate,
        CivilRecord
    }

    // ════════════════════════════════════════════════════════════════
    // 1. المعالجة الموحَّدة — توليد الوثيقة المناسبة تلقائياً
    // ════════════════════════════════════════════════════════════════
    /// <summary>
    /// يولّد الوثيقة الصحيحة بحسب نوع الخدمة المرتبطة بالطلب — بدون
    /// أي حاجة من الفرونت لمعرفة نوع الخدمة مسبقاً.
    ///
    /// POST /api/documents/process/{requestId}
    /// </summary>
    [HttpPost("process/{requestId:guid}")]
    public async Task<IActionResult> Process(Guid requestId, CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (serviceRequest is null)
            return NotFound(new { success = false, message = "الطلب غير موجود" });

        if (!ServiceDocumentMap.TryGetValue(serviceRequest.GovernmentServiceId, out var kind))
            return BadRequest(new
            {
                success = false,
                message = "هذه الخدمة لا تدعم توليد وثيقة إلكترونية"
            });

        // ── توجيه داخلي للـ Command الصحيح عبر MediatR ────────────────
        return kind switch
        {
            DocumentKind.ClearanceCertificate => await ProcessClearance(requestId),
            DocumentKind.CivilRecord          => await ProcessCivilRecord(requestId),
            _ => BadRequest(new { success = false, message = "نوع وثيقة غير معروف" })
        };
    }

    private async Task<IActionResult> ProcessClearance(Guid requestId)
    {
        var result = await mediator.Send(
            new CreateClearanceCertificateCommand { ServiceRequestId = requestId });

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    private async Task<IActionResult> ProcessCivilRecord(Guid requestId)
    {
        var result = await mediator.Send(
            new CreateCivilRecordCommand { ServiceRequestId = requestId });

        return result.Match(
            onSuccess: data  => (IActionResult)Ok(new { success = true, data }),
            onFailure: error => BadRequest(new { success = false, message = error }));
    }

    // ════════════════════════════════════════════════════════════════
    // 2. التحميل الموحَّد — يعمل لكل أنواع الوثائق بدون أي شرط نوع
    // ════════════════════════════════════════════════════════════════
    /// <summary>
    /// تحميل الوثيقة المرتبطة بطلب معيّن — أي نوع وثيقة.
    /// البحث في Attachments عبر ServiceRequestId فقط، بدون شرط FileType.
    ///
    /// GET /api/documents/download/{requestId}
    /// </summary>
    [HttpGet("download/{requestId:guid}")]
    public async Task<IActionResult> Download(Guid requestId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var serviceRequest = await context.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (serviceRequest is null)
            return NotFound(new { success = false, message = "الطلب غير موجود" });

        if (serviceRequest.UserId != userId)
            return Unauthorized(new { success = false, message = "غير مصرح لك بتحميل هذه الوثيقة" });

        var attachment = await context.Attachments
            .AsNoTracking()
            .Where(a => a.ServiceRequestId == requestId)
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (attachment is null)
            return NotFound(new { success = false, message = "لم تُولَّد أي وثيقة لهذا الطلب بعد" });

        if (!System.IO.File.Exists(attachment.FilePath))
            return NotFound(new { success = false, message = "ملف الوثيقة غير موجود على السيرفر" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath, cancellationToken);

        return File(
            fileContents:     fileBytes,
            contentType:      attachment.ContentType,
            fileDownloadName: attachment.FileName);
    }
}
