using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.ServiceSubmission;
using EGovServices.Domain.Entities;
using EGovServices.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EGovServices.Application.Features.Services.Commands.SubmitService;

public sealed record SubmitServiceCommand : IRequest<Result<SubmitServiceResponse>>
{
    public required Guid ServiceId { get; init; }
    public required Guid UserId { get; init; }
    public required Dictionary<string, object> FormData { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? AppointmentSlotId { get; init; }
}

public sealed partial class SubmitServiceHandler(IAppDbContext context)
    : IRequestHandler<SubmitServiceCommand, Result<SubmitServiceResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<SubmitServiceResponse>> Handle(
        SubmitServiceCommand request, CancellationToken cancellationToken)
    {
        // ── 1. التحقق من الخدمة ───────────────────────────────────────
        var service = await context.GovernmentServices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ServiceId, cancellationToken);

        if (service is null) return Result<SubmitServiceResponse>.Failure("الخدمة غير موجودة");
        if (!service.IsActive) return Result<SubmitServiceResponse>.Failure("الخدمة غير متاحة");

        // ── 2. التحقق من حقول النموذج ─────────────────────────────────
        var fields = await context.ServiceFormFields
            .AsNoTracking()
            .Where(f => f.GovernmentServiceId == request.ServiceId && f.IsActive)
            .ToListAsync(cancellationToken);

        if (fields.Count == 0)
            return Result<SubmitServiceResponse>.Failure("لا توجد حقول لهذه الخدمة");

        var validation = ValidateFormData(request.FormData, fields);
        if (!validation.IsSuccess)
            return Result<SubmitServiceResponse>.Failure(validation.Error!);

        // ── 3. التحقق من الفرع والموعد لخدمات Appointment ────────────
        AppointmentSlot? slot = null;

        if (service.ServiceType == ServiceType.Appointment)
        {
            // التحقق من الفرع
            if (!request.BranchId.HasValue)
                return Result<SubmitServiceResponse>.Failure(
                    "يجب اختيار فرع لهذه الخدمة");

            // التحقق من الموعد
            if (!request.AppointmentSlotId.HasValue)
                return Result<SubmitServiceResponse>.Failure(
                    "يجب اختيار موعد لهذه الخدمة");

            slot = await context.AppointmentSlots
                .FirstOrDefaultAsync(
                    s => s.Id == request.AppointmentSlotId.Value,
                    cancellationToken);

            if (slot is null)
                return Result<SubmitServiceResponse>.Failure("الموعد المحدد غير موجود");

            if (!slot.IsActive)
                return Result<SubmitServiceResponse>.Failure("الموعد المحدد غير متاح");

            if (slot.IsFull)
                return Result<SubmitServiceResponse>.Failure(
                    $"عذراً، الموعد ممتلئ. المقاعد المتاحة: {slot.AvailableSeats}");

            if (slot.GovernmentServiceId != request.ServiceId)
                return Result<SubmitServiceResponse>.Failure(
                    "الموعد المحدد لا ينتمي لهذه الخدمة");

            if (slot.SlotDate < DateOnly.FromDateTime(DateTime.UtcNow))
                return Result<SubmitServiceResponse>.Failure(
                    "الموعد المحدد قد انتهى تاريخه");
        }

        // ── 4. التحقق من رصيد المحفظة ─────────────────────────────────
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

        if (wallet is null)
            return Result<SubmitServiceResponse>.Failure("المحفظة غير موجودة");

        if (wallet.Balance < service.ServiceFee)
            return Result<SubmitServiceResponse>.Failure(
                $"رصيد المحفظة غير كافٍ. " +
                $"الرصيد الحالي: {wallet.Balance:F2} ليرة سورية، " +
                $"رسوم الخدمة: {service.ServiceFee:F2} ليرة سورية");

        // ── 5. خصم الرسوم ─────────────────────────────────────────────
        wallet.Balance -= service.ServiceFee;

        // معرّف الطلب يُولَّد هنا مبكرًا (بدل الخطوة 9) حتى نتمكن من
        // ربط معاملة الدفع بالطلب مباشرة عبر ServiceRequestId — بدون
        // هذا الربط، عملية الاسترداد التلقائي عند الإلغاء لن تعمل أبداً
        // لأنها تبحث عن المعاملة عبر ServiceRequestId.
        var serviceRequestId = Guid.NewGuid();

        // ── 6. تسجيل معاملة المحفظة ───────────────────────────────────
        await context.WalletTransactions.AddAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = service.ServiceFee,
            TransactionType = "ServiceFeePayment",
            Description = $"دفع رسوم خدمة {service.Name}",
            CreatedAt = DateTime.UtcNow,
            ServiceRequestId = serviceRequestId
        }, cancellationToken);

        // ── 7. حجز المقعد ─────────────────────────────────────────────
        if (slot is not null)
            slot.BookedSeats += 1;

        // ── 8. تحديد الحالة الأولية ───────────────────────────────────
        var initialStatus = service.ServiceType == ServiceType.Digital
            ? "Processing"
            : "Submitted";

        // ── 9. إنشاء الطلب ────────────────────────────────────────────
        var referenceNumber = await GenerateReferenceNumber(cancellationToken);

        var serviceRequest = new ServiceRequest
        {
            Id = serviceRequestId,
            UserId = request.UserId,
            GovernmentServiceId = request.ServiceId,
            ReferenceNumber = referenceNumber,
            Status = initialStatus,
            SubmissionDate = DateTime.UtcNow,
            BranchId = request.BranchId,
            AppointmentSlotId = request.AppointmentSlotId,
            FormData = JsonSerializer.Serialize(request.FormData, JsonOptions)
        };

        await context.ServiceRequests.AddAsync(serviceRequest, cancellationToken);

        // ── 10. AuditLog ──────────────────────────────────────────────
        var slotInfo = slot is not null
            ? $" — الموعد: {slot.SlotDate:dd/MM/yyyy} الساعة {slot.StartTime:HH:mm}"
            : "";

        await context.RequestAuditLogs.AddAsync(new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            OldStatus = null,
            NewStatus = initialStatus,
            Action = "Submitted",
            Notes = service.ServiceType == ServiceType.Digital
                ? "تم تقديم الطلب ودفع الرسوم — جارٍ المعالجة التلقائية"
                : $"تم تقديم الطلب ودفع الرسوم — في انتظار مراجعة الموظف{slotInfo}",
            ChangedByUserId = null,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // ── 11. إشعار للمواطن ─────────────────────────────────────────
        await context.Notifications.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = $"تم تقديم طلب {service.Name} بنجاح",
            Message = service.ServiceType == ServiceType.Digital
                ? $"تم تقديم طلبك وخصم {service.ServiceFee:F2} ليرة سورية — جارٍ المعالجة"
                : $"تم تقديم طلبك وخصم {service.ServiceFee:F2} ليرة سورية" +
                  $" — موعدك: {slot!.SlotDate:dd/MM/yyyy} الساعة {slot.StartTime:HH:mm}",
            NotificationType = "Success",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // ── 12. حفظ كل شيء دفعة واحدة ───────────────────────────────
        await context.SaveChangesAsync(cancellationToken);

        // ── 13. بناء الرد ─────────────────────────────────────────────
        var message = service.ServiceType == ServiceType.Digital
            ? $"تم استلام طلبك بنجاح ودفع الرسوم ({service.ServiceFee:F2} ليرة سورية). رقم المرجع: {referenceNumber}"
            : $"تم استلام طلبك بنجاح ودفع الرسوم ({service.ServiceFee:F2} ليرة سورية). " +
              $"موعدك: {slot!.SlotDate:dd/MM/yyyy} الساعة {slot.StartTime:HH:mm}. " +
              $"رقم المرجع: {referenceNumber}";

        return Result<SubmitServiceResponse>.Success(new SubmitServiceResponse
        {
            RequestId = serviceRequest.Id,
            ReferenceNumber = referenceNumber,
            Status = initialStatus,
            SubmissionDate = serviceRequest.SubmissionDate,
            Message = message
        });
    }
}
