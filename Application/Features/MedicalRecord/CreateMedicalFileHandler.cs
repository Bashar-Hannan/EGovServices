using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.MedicalFile;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.MedicalFile;

/// <summary>
/// Handler — يعيد استخدام نفس نمط CreateClearanceCertificateHandler
/// بالضبط: يستخرج NationalNumber من JWT claims، يجلب Citizen،
/// ثم يجلب MedicalRecord المرتبط به. لا يوجد أي اعتماد على FormData
/// إطلاقاً — المواطن لا يُدخل أي بيانات طبية بنفسه.
///
/// إضافة عن نمط الشهادة الأصلي: تحقق صريح من ملكية الطلب
/// (serviceRequest.UserId == currentUserId) — مطلوب صراحة لهذه
/// الخدمة تحديداً بسبب حساسية البيانات الطبية.
/// </summary>
public sealed partial class CreateMedicalFileHandler(
    IAppDbContext context,
    IPdfService pdfService,
    IHttpContextAccessor httpContextAccessor,
    IVerificationTokenService verificationTokenService)
    : IRequestHandler<CreateMedicalFileCommand, Result<CreateMedicalFileResponse>>
{
    public async Task<Result<CreateMedicalFileResponse>> Handle(
        CreateMedicalFileCommand request, CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<CreateMedicalFileResponse>.Failure("الطلب غير موجود");

        if (serviceRequest.Status == "Completed")
            return Result<CreateMedicalFileResponse>.Failure("تم معالجة هذا الطلب مسبقاً");

        // ── Authorization: تحقق صريح من الملكية ────────────────────────
        var currentUserId = ExtractUserId();
        if (currentUserId is null)
            return Result<CreateMedicalFileResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        if (serviceRequest.UserId != currentUserId.Value)
            return Result<CreateMedicalFileResponse>.Failure("غير مصرح لك بمعالجة هذا الطلب");

        // ── استخراج NationalNumber من JWT (نفس نمط الشهادة بالضبط) ─────
        var nationalNumber = ExtractNationalNumberFromClaims();
        if (string.IsNullOrEmpty(nationalNumber))
            return Result<CreateMedicalFileResponse>.Failure("تعذّر التحقق من هوية المستخدم");

        var citizen = await context.Citizens
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalNumber == nationalNumber, cancellationToken);

        if (citizen is null)
            return Result<CreateMedicalFileResponse>.Failure("بيانات المواطن غير موجودة");

        var medicalRecord = await context.MedicalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CitizenNationalNumber == nationalNumber, cancellationToken);

        if (medicalRecord is null)
            return Result<CreateMedicalFileResponse>.Failure(
                "لا يوجد سجل طبي مسجَّل لهذا المواطن في قاعدة البيانات الحكومية");

        var fullName = $"{citizen.FirstName} {citizen.FatherName} {citizen.LastName}";
        var verificationToken = verificationTokenService.GenerateToken();

        var pdfData = new MedicalFilePdfData
        {
            FullName       = fullName,
            NationalNumber = nationalNumber,
            BirthDate      = citizen.BirthDate.ToString("dd/MM/yyyy"),
            Gender         = citizen.Gender,   // "ذكر"/"أنثى" — يُمرَّر كما هو، بدون أي تحويل

            BloodType       = medicalRecord.BloodType,
            HeightCm        = medicalRecord.HeightCm,
            WeightKg        = medicalRecord.WeightKg,
            Allergies       = medicalRecord.Allergies,
            ChronicDiseases = medicalRecord.ChronicDiseases,

            ReferenceNumber   = serviceRequest.ReferenceNumber,
            IssueDate         = DateTime.UtcNow.ToString("dd/MM/yyyy"),
            VerificationToken = verificationToken
        };

        string pdfFilePath;
        try { pdfFilePath = await pdfService.GenerateMedicalFileAsync(pdfData); }
        catch (Exception ex)
        {
            return Result<CreateMedicalFileResponse>
                .Failure($"فشل إنشاء ملف الوثيقة: {ex.Message}");
        }

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            FileName = Path.GetFileName(pdfFilePath),
            FilePath = pdfFilePath,
            ContentType = "application/pdf",
            FileType = "MedicalFile",
            FileSizeBytes = new FileInfo(pdfFilePath).Length,
            VerificationToken = verificationToken,
            VerificationTokenExpiresAt = null
        };

        await context.Attachments.AddAsync(attachment, cancellationToken);

        serviceRequest.Status = "Completed";
        serviceRequest.CompletedAt = DateTime.UtcNow;
        serviceRequest.ProcessingNotes = "تم إصدار الملف الطبي الإلكتروني بنجاح";

        await context.RequestAuditLogs.AddAsync(new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            OldStatus = "Processing",
            NewStatus = "Completed",
            Action = "MedicalFileGenerated",
            Notes = "تم إصدار الملف الطبي الإلكتروني بنجاح",
            ChangedByUserId = null,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateMedicalFileResponse>.Success(new CreateMedicalFileResponse
        {
            ServiceRequestId = serviceRequest.Id,
            ReferenceNumber  = serviceRequest.ReferenceNumber,
            Status           = "Completed",
            PdfFilePath      = pdfFilePath,
            ResultMessage    = "تم إصدار الملف الطبي الإلكتروني بنجاح"
        });
    }
}
