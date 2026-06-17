using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGovServices.Application.Features.ClearanceCertificate;

/// <summary>
/// Handler for CreateClearanceCertificateCommand.
///
/// Uses:
/// - IAppDbContext         → read/write database
/// - IPdfService           → generate PDF file
/// - IHttpContextAccessor  → extract NationalNumber from JWT claims
/// </summary>
public sealed partial class CreateClearanceCertificateHandler(
    IAppDbContext context,
    IPdfService pdfService,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<CreateClearanceCertificateCommand,
                      Result<CreateClearanceCertificateResponse>>
{
    public async Task<Result<CreateClearanceCertificateResponse>> Handle(
        CreateClearanceCertificateCommand request,
        CancellationToken cancellationToken)
    {
        // ============================================================
        // STEP 1: Load the ServiceRequest
        // ============================================================

        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<CreateClearanceCertificateResponse>
                .Failure("الطلب غير موجود");

        if (serviceRequest.Status == "Completed")
            return Result<CreateClearanceCertificateResponse>
                .Failure("تم معالجة هذا الطلب مسبقاً");

        // ============================================================
        // STEP 2: Extract NationalNumber from JWT Claims
        // (as requested in Option A)
        // ============================================================

        var nationalNumber = ExtractNationalNumberFromClaims();

        if (string.IsNullOrEmpty(nationalNumber))
            return Result<CreateClearanceCertificateResponse>
                .Failure("تعذّر التحقق من هوية المستخدم");

        // ============================================================
        // STEP 3: Load Citizen data (for the PDF)
        // ============================================================

        var citizen = await context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalNumber == nationalNumber, cancellationToken);

        if (citizen is null)
            return Result<CreateClearanceCertificateResponse>
                .Failure("بيانات المواطن غير موجودة");

        // ============================================================
        // STEP 4: Query CriminalRecords table
        // ============================================================

        var criminalRecords = await context.CriminalRecords
            .AsNoTracking()
            .Where(r => r.CitizenNationalNumber == nationalNumber)
            .OrderByDescending(r => r.JudgmentDate)
            .ToListAsync(cancellationToken);

        // ============================================================
        // STEP 5: Determine check result
        // ============================================================

        var (checkResult, hasActiveCrimes) = BuildCheckResult(criminalRecords);

        // ============================================================
        // STEP 6: Generate PDF
        // ============================================================

        var fullName = $"{citizen.FirstName} {citizen.FatherName} {citizen.LastName}";

        var pdfData = new ClearanceCertificatePdfData
        {
            FullName = fullName,
            NationalNumber = nationalNumber,
            CheckResult = checkResult,
            HasActiveCrimes = hasActiveCrimes,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReferenceNumber = serviceRequest.ReferenceNumber,
            FormDataJson = serviceRequest.FormData
        };

        string pdfFilePath;
        try
        {
            pdfFilePath = await pdfService.GenerateClearanceCertificateAsync(pdfData);
        }
        catch (Exception ex)
        {
            return Result<CreateClearanceCertificateResponse>
                .Failure($"فشل إنشاء ملف الشهادة: {ex.Message}");
        }

        // ============================================================
        // STEP 7: Save Attachment record
        // ============================================================

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            FileName = Path.GetFileName(pdfFilePath),
            FilePath = pdfFilePath,
            ContentType = "application/pdf",
            FileType = "ClearanceCertificate",
            FileSizeBytes = new FileInfo(pdfFilePath).Length
        };

        await context.Attachments.AddAsync(attachment, cancellationToken);

        // ============================================================
        // STEP 8: Update ServiceRequest → Completed
        // ============================================================

        serviceRequest.Status = "Completed";
        serviceRequest.CompletedAt = DateTime.UtcNow;
        serviceRequest.ProcessingNotes = hasActiveCrimes
            ? "تم إصدار الشهادة - يوجد سجل جنائي"
            : "تم إصدار الشهادة - لا توجد سوابق";

        // ============================================================
        // STEP 9: Audit Log
        // ============================================================

        var auditLog = new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            NewStatus = "Completed",
            Action = "CertificateGenerated",
            CreatedAt = DateTime.UtcNow
        };

        await context.RequestAuditLogs.AddAsync(auditLog, cancellationToken);

        // ============================================================
        // STEP 10: Save everything (atomic)
        // ============================================================

        await context.SaveChangesAsync(cancellationToken);

        // ============================================================
        // STEP 11: Return response
        // ============================================================

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

    // Helper methods in Part 2 (partial class)
}
