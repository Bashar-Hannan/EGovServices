using EGovServices.Application.Common;
using EGovServices.Application.DTOs.CivilRecord;
using MediatR;

namespace EGovServices.Application.Features.CivilRecord;

/// <summary>
/// Command: معالجة طلب إخراج قيد فردي مدني وتوليد PDF.
///
/// Flow:
/// 1. تحميل ServiceRequest
/// 2. استخراج NationalNumber من JWT
/// 3. جلب بيانات Citizen
/// 4. توليد PDF عبر IPdfService.GenerateCivilRecordAsync()
/// 5. حفظ المسار في Attachments
/// 6. تحديث Status → Completed
/// 7. إرجاع الرد
/// </summary>
public sealed record CreateCivilRecordCommand
    : IRequest<Result<CreateCivilRecordResponse>>
{
    public required Guid ServiceRequestId { get; init; }
}
