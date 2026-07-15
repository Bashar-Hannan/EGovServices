using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.CivilRecord;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.CivilRecord;

/// <summary>
/// Handler لمعالجة طلب إخراج القيد الفردي.
/// يتبع نفس Pattern الخاص بـ CreateClearanceCertificateHandler تماماً.
/// </summary>
public sealed class CreateCivilRecordHandler(
    IAppDbContext context,
    IPdfService pdfService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<CreateCivilRecordCommand, Result<CreateCivilRecordResponse>>
{
    public async Task<Result<CreateCivilRecordResponse>> Handle(
        CreateCivilRecordCommand request,
        CancellationToken cancellationToken)
    {
        // ── STEP 1: تحميل ServiceRequest ─────────────────────────────
        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<CreateCivilRecordResponse>.Failure("الطلب غير موجود");

        if (serviceRequest.Status == "Completed")
            return Result<CreateCivilRecordResponse>.Failure("تم معالجة هذا الطلب مسبقاً");

        // ── STEP 2: استخراج NationalNumber من JWT ────────────────────
        var nationalNumber = httpContextAccessor.HttpContext?.User
            .FindFirst("NationalNumber")?.Value;

        if (string.IsNullOrEmpty(nationalNumber))
            return Result<CreateCivilRecordResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        // ── STEP 3: جلب بيانات Citizen ───────────────────────────────
        var citizen = await context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalNumber == nationalNumber, cancellationToken);

        if (citizen is null)
            return Result<CreateCivilRecordResponse>.Failure("بيانات المواطن غير موجودة");

        // ── STEP 4: بناء بيانات الـ PDF ──────────────────────────────
        var now = DateTime.UtcNow;

        var pdfData = new CivilRecordPdfData
        {
            NationalNumber = citizen.NationalNumber,
            FirstName = citizen.FirstName,
            FatherName = citizen.FatherName,
            LastName = citizen.LastName,
            DateOfBirth = citizen.BirthDate.ToString("dd/MM/yyyy"),
            PlaceOfBirth = citizen.PlaceOfBirth,
            MaritalStatus = citizen.MaritalStatus,
            ReferenceNumber = serviceRequest.ReferenceNumber,
            IssueDate = DateOnly.FromDateTime(now).ToString("dd/MM/yyyy"),
            PrintDate = now.ToString("dd/MM/yyyy HH:mm"),
            DocumentSerial = GenerateDocumentSerial(serviceRequest.ReferenceNumber),

            // الحقول nullable — تُستبدل بـ "—" إذا كانت فارغة
            MotherFullName = citizen.MotherName ?? "—",
            Religion = citizen.Religion ?? "—",
            Gender = citizen.Gender ?? "—",
            RecordPlace = citizen.RecordPlace ?? "—",
            RecordNumber = citizen.RecordNumber ?? "—",
        };

        // ── STEP 5: توليد PDF ─────────────────────────────────────────
        string pdfFilePath;
        try
        {
            pdfFilePath = await pdfService.GenerateCivilRecordAsync(pdfData);
        }
        catch (Exception ex)
        {
            return Result<CreateCivilRecordResponse>.Failure($"فشل إنشاء وثيقة القيد: {ex.Message}");
        }

        // ── STEP 6: حفظ Attachment ────────────────────────────────────
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            FileName = Path.GetFileName(pdfFilePath),
            FilePath = pdfFilePath,
            ContentType = "application/pdf",
            FileType = "CivilRecord",
            FileSizeBytes = new FileInfo(pdfFilePath).Length
        };

        await context.Attachments.AddAsync(attachment, cancellationToken);

        // ── STEP 7: تحديث Status → Completed ─────────────────────────
        serviceRequest.Status = "Completed";
        serviceRequest.CompletedAt = now;
        serviceRequest.ProcessingNotes = "تم إصدار وثيقة إخراج القيد الفردي بنجاح";

        // ── STEP 8: AuditLog ──────────────────────────────────────────
        var auditLog = new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            OldStatus = serviceRequest.Status,
            NewStatus = "Completed",
            Action = "CivilRecordGenerated",
            Notes = "تم إصدار وثيقة إخراج القيد الفردي بنجاح",
            CreatedAt = now
        };

        await context.RequestAuditLogs.AddAsync(auditLog, cancellationToken);

        // ── STEP 9: حفظ كل شيء دفعة واحدة ──────────────────────────
        await context.SaveChangesAsync(cancellationToken);

        // ── STEP 10: الرد ─────────────────────────────────────────────
        return Result<CreateCivilRecordResponse>.Success(new CreateCivilRecordResponse
        {
            ServiceRequestId = serviceRequest.Id,
            ReferenceNumber = serviceRequest.ReferenceNumber,
            Status = "Completed",
            PdfFilePath = pdfFilePath,
            ResultMessage = "تم إصدار وثيقة إخراج القيد الفردي بنجاح"
        });
    }

    /// <summary>
    /// يولّد رقم تسلسلي للوثيقة من رقم المرجع.
    /// مثال: REQ-2026-000001 → CR-2026-000001
    /// </summary>
    private static string GenerateDocumentSerial(string referenceNumber)
        => referenceNumber.Replace("REQ-", "CR-");
}
