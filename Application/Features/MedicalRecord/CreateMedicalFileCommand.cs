using EGovServices.Application.Common;
using EGovServices.Application.DTOs.MedicalFile;
using MediatR;

namespace EGovServices.Application.Features.MedicalFile;

/// <summary>
/// Command: معالجة طلب "الملف الطبي الإلكتروني" (خدمة وزارة الصحة).
///
/// نفس نمط CreateClearanceCertificateCommand بالضبط:
/// 1. تحميل ServiceRequest من قاعدة البيانات
/// 2. استخراج NationalNumber من JWT claims
/// 3. جلب Citizen ثم MedicalRecord المرتبط به
/// 4. توليد PDF عبر IPdfService
/// 5. حفظ مسار الملف في جدول Attachments
/// 6. تحديث ServiceRequest → Status = "Completed"
/// 7. إرجاع مسار الملف ورسالة النتيجة
/// </summary>
public sealed record CreateMedicalFileCommand
    : IRequest<Result<CreateMedicalFileResponse>>
{
    /// <summary>
    /// ServiceRequest.Id الذي أنشأه المواطن عند تقديم الطلب.
    /// هذا هو الطلب الذي نعالجه الآن.
    /// </summary>
    public required Guid ServiceRequestId { get; init; }
}
