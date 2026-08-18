using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.CivilRecord;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.CivilRecord;

public sealed class CreateCivilRecordHandler(
    IAppDbContext context,
    IPdfService pdfService,
    IHttpContextAccessor httpContextAccessor,
    IVerificationTokenService verificationTokenService)   // ← أضفنا هذا
    : IRequestHandler<CreateCivilRecordCommand, Result<CreateCivilRecordResponse>>
{
    public async Task<Result<CreateCivilRecordResponse>> Handle(
        CreateCivilRecordCommand request,
        CancellationToken cancellationToken)
    {
        // ── STEP 1: Load ServiceRequest ───────────────────────────────
        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<CreateCivilRecordResponse>.Failure("الطلب غير موجود");

        if (serviceRequest.Status == "Completed")
            return Result<CreateCivilRecordResponse>.Failure("تم معالجة هذا الطلب مسبقاً");

        // ── STEP 2: Extract NationalNumber from JWT ───────────────────
        var nationalNumber = httpContextAccessor.HttpContext?.User
            .FindFirst("NationalNumber")?.Value;

        if (string.IsNullOrEmpty(nationalNumber))
            return Result<CreateCivilRecordResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        // ── STEP 3: Load Citizen data ─────────────────────────────────
        var citizen = await context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalNumber == nationalNumber, cancellationToken);

        if (citizen is null)
            return Result<CreateCivilRecordResponse>.Failure("بيانات المواطن غير موجودة");

        // ── STEP 4: توليد Verification Token ─────────────────────────
        var verificationToken = verificationTokenService.GenerateToken();

        // ── STEP 5: Build PDF data ────────────────────────────────────
        var now = DateTime.UtcNow;

        var pdfData = new CivilRecordPdfData
        {
            NationalNumber = citizen.NationalNumber,
            FirstName = citizen.FirstName,
            FatherName = citizen.FatherName,
            LastName = citizen.LastName,
            DateOfBirth = citizen.BirthDate.ToString("dd/MM/yyyy"),
            ReferenceNumber = serviceRequest.ReferenceNumber,
            IssueDate = DateOnly.FromDateTime(now).ToString("dd/MM/yyyy"),
            PrintDate = now.ToString("dd/MM/yyyy HH:mm"),
            DocumentSerial = GenerateDocumentSerial(serviceRequest.ReferenceNumber),
            PlaceOfBirth = citizen.PlaceOfBirth ?? "—",
            MaritalStatus = citizen.MaritalStatus ?? "—",
            MotherFullName = citizen.MotherName ?? citizen.MotherName,
            Religion = citizen.Religion ?? "—",
            Gender = citizen.Gender,
            RecordPlace = citizen.RecordPlace ?? "—",
            RecordNumber = citizen.RecordNumber ?? "—",
            VerificationToken = verificationToken      // ← السطر الناقص
        };

        // ── STEP 6: Generate PDF ──────────────────────────────────────
        string pdfFilePath;
        try
        {
            pdfFilePath = await pdfService.GenerateCivilRecordAsync(pdfData);
        }
        catch (Exception ex)
        {
            return Result<CreateCivilRecordResponse>
                .Failure($"فشل إنشاء وثيقة القيد: {ex.Message}");
        }

        // ── STEP 7: Save Attachment مع الـ Token ─────────────────────
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            FileName = Path.GetFileName(pdfFilePath),
            FilePath = pdfFilePath,
            ContentType = "application/pdf",
            FileType = "CivilRecord",
            FileSizeBytes = new FileInfo(pdfFilePath).Length,
            VerificationToken = verificationToken   // ← السطر الناقص
        };

        await context.Attachments.AddAsync(attachment, cancellationToken);

        // ── STEP 8: Update ServiceRequest → Completed ────────────────
        serviceRequest.Status = "Completed";
        serviceRequest.CompletedAt = now;
        serviceRequest.ProcessingNotes = "تم إصدار وثيقة إخراج القيد الفردي بنجاح";

        // ── STEP 9: AuditLog ──────────────────────────────────────────
        var auditLog = new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            NewStatus = "Completed",
            Action = "CivilRecordGenerated",
            CreatedAt = now
        };

        await context.RequestAuditLogs.AddAsync(auditLog, cancellationToken);

        // ── STEP 10: Save everything atomically ──────────────────────
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateCivilRecordResponse>.Success(new CreateCivilRecordResponse
        {
            ServiceRequestId = serviceRequest.Id,
            ReferenceNumber = serviceRequest.ReferenceNumber,
            Status = "Completed",
            PdfFilePath = pdfFilePath,
            ResultMessage = "تم إصدار وثيقة إخراج القيد الفردي بنجاح"
        });
    }

    private static string GenerateDocumentSerial(string referenceNumber)
        => referenceNumber.Replace("REQ-", "CR-");
}
