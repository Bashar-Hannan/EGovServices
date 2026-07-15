using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.ClearanceCertificate;

/// <summary>
/// Handler المحدّث — يولّد VerificationToken ويخزّنه في Attachment.
/// التغيير الوحيد عن النسخة الأصلية هو:
/// 1. استقبال IVerificationTokenService
/// 2. توليد Token وتمريره لـ PdfService
/// 3. حفظه في Attachment
/// </summary>
public sealed partial class CreateClearanceCertificateHandler(
    IAppDbContext context,
    IPdfService pdfService,
    IHttpContextAccessor httpContextAccessor,
    IVerificationTokenService verificationTokenService)   // ← NEW
    : IRequestHandler<CreateClearanceCertificateCommand,
                      Result<CreateClearanceCertificateResponse>>
{
    public async Task<Result<CreateClearanceCertificateResponse>> Handle(
        CreateClearanceCertificateCommand request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<CreateClearanceCertificateResponse>.Failure("الطلب غير موجود");

        if (serviceRequest.Status == "Completed")
            return Result<CreateClearanceCertificateResponse>.Failure("تم معالجة هذا الطلب مسبقاً");

        var nationalNumber = ExtractNationalNumberFromClaims();
        if (string.IsNullOrEmpty(nationalNumber))
            return Result<CreateClearanceCertificateResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        var citizen = await context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalNumber == nationalNumber, cancellationToken);

        if (citizen is null)
            return Result<CreateClearanceCertificateResponse>.Failure("بيانات المواطن غير موجودة");

        var criminalRecords = await context.CriminalRecords
            .AsNoTracking()
            .Where(r => r.CitizenNationalNumber == nationalNumber)
            .OrderByDescending(r => r.JudgmentDate)
            .ToListAsync(cancellationToken);

        var (checkResult, hasActiveCrimes) = BuildCheckResult(criminalRecords);

        var fullName = $"{citizen.FirstName} {citizen.FatherName} {citizen.LastName}";

        // ── NEW: توليد Verification Token ───────────────────────────
        var verificationToken = verificationTokenService.GenerateToken();

        var pdfData = new ClearanceCertificatePdfData
        {
            FullName = fullName,
            NationalNumber = nationalNumber,
            CheckResult = checkResult,
            HasActiveCrimes = hasActiveCrimes,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReferenceNumber = serviceRequest.ReferenceNumber,
            FormDataJson = serviceRequest.FormData,
            VerificationToken = verificationToken   // ← NEW
        };

        string pdfFilePath;
        try { pdfFilePath = await pdfService.GenerateClearanceCertificateAsync(pdfData); }
        catch (Exception ex)
        {
            return Result<CreateClearanceCertificateResponse>
                .Failure($"فشل إنشاء ملف الشهادة: {ex.Message}");
        }

        // ── NEW: حفظ Token في Attachment ────────────────────────────
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            FileName = Path.GetFileName(pdfFilePath),
            FilePath = pdfFilePath,
            ContentType = "application/pdf",
            FileType = "ClearanceCertificate",
            FileSizeBytes = new FileInfo(pdfFilePath).Length,
            VerificationToken = verificationToken,
            VerificationTokenExpiresAt = null    // شهادة عدم المحكومية لا تنتهي
        };

        await context.Attachments.AddAsync(attachment, cancellationToken);

        serviceRequest.Status = "Completed";
        serviceRequest.CompletedAt = DateTime.UtcNow;
        serviceRequest.ProcessingNotes = hasActiveCrimes
            ? "تم إصدار الشهادة - يوجد سجل جنائي"
            : "تم إصدار الشهادة - لا توجد سوابق";

        var auditLog = new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            OldStatus = "Processing",
            NewStatus = "Completed",
            Action = "CertificateGenerated",
            Notes = "تم إصدار شهادة عدم المحكومية بنجاح",
            ChangedByUserId = null,
            CreatedAt = DateTime.UtcNow
        };

        await context.RequestAuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var resultMessage = hasActiveCrimes
            ? "تم إصدار الشهادة — يوجد سجل جنائي مرتبط بهويتك"
            : "تم إصدار شهادة عدم المحكومية بنجاح — لا توجد سوابق جنائية";

        return Result<CreateClearanceCertificateResponse>.Success(
            new CreateClearanceCertificateResponse
            {
                ServiceRequestId = serviceRequest.Id,
                ReferenceNumber = serviceRequest.ReferenceNumber,
                Status = "Completed",
                PdfFilePath = pdfFilePath,
                ResultMessage = resultMessage
            });
    }
}
