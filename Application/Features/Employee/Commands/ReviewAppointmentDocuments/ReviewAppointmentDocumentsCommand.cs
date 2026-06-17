using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Employee.Commands.ReviewAppointmentDocuments;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record ReviewAppointmentDocumentsCommand
    : IRequest<Result<ReviewAppointmentDocumentsResponse>>
{
    public required Guid RequestId { get; init; }
    public required Guid ReviewedByUserId { get; init; }    // من الـ JWT
    public required bool IsApproved { get; init; }          // true = تأكيد، false = إلغاء
    public string? RejectionReason { get; init; }           // مطلوب عند الإلغاء
    public string? Notes { get; init; }
}

// ─── Response DTO ─────────────────────────────────────────────────────────────
public sealed record ReviewAppointmentDocumentsResponse
{
    public required Guid RequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string NewStatus { get; init; }
    public required string Message { get; init; }
}

// ─── Validator ────────────────────────────────────────────────────────────────
public sealed class ReviewAppointmentDocumentsValidator
    : AbstractValidator<ReviewAppointmentDocumentsCommand>
{
    public ReviewAppointmentDocumentsValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => !x.IsApproved)
            .WithMessage("سبب الإلغاء مطلوب عند رفض الطلب");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class ReviewAppointmentDocumentsHandler(IAppDbContext context)
    : IRequestHandler<ReviewAppointmentDocumentsCommand, Result<ReviewAppointmentDocumentsResponse>>
{
    public async Task<Result<ReviewAppointmentDocumentsResponse>> Handle(
        ReviewAppointmentDocumentsCommand request, CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .Include(r => r.AppointmentSlot)
            .FirstOrDefaultAsync(r =>
                r.Id == request.RequestId && !r.IsDeleted,
                cancellationToken);

        if (serviceRequest is null)
            return Result<ReviewAppointmentDocumentsResponse>.Failure("الطلب غير موجود");

        // الحالة يجب أن تكون Pending أو DocumentsUnderReview فقط
        if (serviceRequest.Status != RequestStatus.Pending &&
            serviceRequest.Status != RequestStatus.DocumentsUnderReview)
        {
            return Result<ReviewAppointmentDocumentsResponse>.Failure(
                $"لا يمكن مراجعة طلب بحالة '{serviceRequest.Status}'");
        }

        var oldStatus = serviceRequest.Status;
        string newStatus;
        string notificationTitle;
        string notificationMessage;

        if (request.IsApproved)
        {
            // ✅ تأكيد — الموعد مؤكد
            newStatus            = RequestStatus.AppointmentConfirmed;
            notificationTitle   = "تم تأكيد موعدك";
            notificationMessage = $"تمت الموافقة على ملفاتك وتأكيد موعدك. " +
                                  $"تاريخ الموعد: {serviceRequest.AppointmentSlot!.SlotDate} " +
                                  $"الساعة {serviceRequest.AppointmentSlot.StartTime:hh\\:mm}. " +
                                  $"رقم الطلب: {serviceRequest.ReferenceNumber}";
        }
        else
        {
            // ❌ إلغاء — حذف منطقي + تحرير المقعد
            newStatus            = RequestStatus.AppointmentCancelled;
            notificationTitle   = "تم إلغاء حجز موعدك";
            notificationMessage = $"تم إلغاء طلبك {serviceRequest.ReferenceNumber}. " +
                                  $"السبب: {request.RejectionReason}. " +
                                  $"يمكنك التقديم مجدداً باختيار موعد آخر.";

            // الحذف المنطقي
            serviceRequest.IsDeleted  = true;
            serviceRequest.DeletedAt  = DateTime.UtcNow;

            // تحرير المقعد في الـ Slot
            if (serviceRequest.AppointmentSlot is not null &&
                serviceRequest.AppointmentSlot.BookedSeats > 0)
            {
                serviceRequest.AppointmentSlot.BookedSeats--;
            }
        }

        serviceRequest.Status           = newStatus;
        serviceRequest.RejectionReason  = request.RejectionReason;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            serviceRequest.ProcessingNotes = request.Notes;

        // سجل التدقيق
        await context.RequestAuditLogs.AddAsync(new RequestAuditLog
        {
            Id               = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            NewStatus        = newStatus,
            Action           = $"DocumentsReview:{oldStatus}->{newStatus}",
            ChangedByUserId  = request.ReviewedByUserId,
            CreatedAt        = DateTime.UtcNow
        }, cancellationToken);

        // إشعار للمواطن
        await context.Notifications.AddAsync(new Notification
        {
            Id               = Guid.NewGuid(),
            UserId           = serviceRequest.UserId,
            Title            = notificationTitle,
            Message          = notificationMessage,
            NotificationType = request.IsApproved ? "Success" : "Warning",
            IsRead           = false,
            CreatedAt        = DateTime.UtcNow
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<ReviewAppointmentDocumentsResponse>.Success(
            new ReviewAppointmentDocumentsResponse
            {
                RequestId       = serviceRequest.Id,
                ReferenceNumber = serviceRequest.ReferenceNumber,
                NewStatus       = newStatus,
                Message         = request.IsApproved
                    ? "تم تأكيد الموعد وإشعار المواطن"
                    : "تم إلغاء الموعد وتحرير المقعد وإشعار المواطن"
            });
    }
}
