using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Employee.Commands.UpdateRequestStatus;

public sealed record UpdateRequestStatusCommand : IRequest<Result<UpdateRequestStatusResponse>>
{
    public required Guid RequestId { get; init; }
    public required Guid ChangedByUserId { get; init; }
    public required string NewStatus { get; init; }
    public string? RejectionReason { get; init; }
    public string? ProcessingNotes { get; init; }
}

public sealed record UpdateRequestStatusResponse
{
    public required Guid RequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public decimal? RefundedAmount { get; init; }  // ← جديد: المبلغ المُستردّ إن وُجد
}

public sealed class UpdateRequestStatusValidator
    : AbstractValidator<UpdateRequestStatusCommand>
{
    private static readonly string[] AllowedStatuses =
    [
        "DocumentsUnderReview", "AppointmentConfirmed",
        "AppointmentCancelled", "Completed", "Rejected"
    ];

    public UpdateRequestStatusValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"الحالة يجب أن تكون إحدى: {string.Join(", ", AllowedStatuses)}");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => x.NewStatus is "Rejected" or "AppointmentCancelled")
            .WithMessage("سبب الرفض/الإلغاء مطلوب");

        RuleFor(x => x.ChangedByUserId)
            .NotEmpty()
            .WithMessage("معرّف المستخدم المنفذ مطلوب");
    }
}

public sealed class UpdateRequestStatusHandler(IAppDbContext context)
    : IRequestHandler<UpdateRequestStatusCommand, Result<UpdateRequestStatusResponse>>
{
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        ["Processing"] = ["Completed", "Rejected"],
        ["Submitted"] = ["DocumentsUnderReview", "AppointmentCancelled"],
        ["DocumentsUnderReview"] = ["AppointmentConfirmed", "AppointmentCancelled"],
        ["AppointmentConfirmed"] = ["Completed", "AppointmentCancelled"],
        ["AppointmentCancelled"] = [],
        ["Completed"] = [],
        ["Rejected"] = [],
    };

    public async Task<Result<UpdateRequestStatusResponse>> Handle(
        UpdateRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .Include(r => r.GovernmentService)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<UpdateRequestStatusResponse>.Failure("الطلب غير موجود");

        if (!ValidTransitions.TryGetValue(serviceRequest.Status, out var allowed)
            || !allowed.Contains(request.NewStatus))
            return Result<UpdateRequestStatusResponse>.Failure(
                $"لا يمكن الانتقال من '{serviceRequest.Status}' إلى '{request.NewStatus}'");

        var oldStatus = serviceRequest.Status;
        serviceRequest.Status = request.NewStatus;

        if (!string.IsNullOrWhiteSpace(request.ProcessingNotes))
            serviceRequest.ProcessingNotes = request.ProcessingNotes;

        if (request.NewStatus is "Rejected" or "AppointmentCancelled")
            serviceRequest.RejectionReason = request.RejectionReason;

        if (request.NewStatus == "Completed")
            serviceRequest.CompletedAt = DateTime.UtcNow;

        var now = DateTime.UtcNow;

        // ── استرداد المبلغ عند AppointmentCancelled ───────────────────
        decimal? refundedAmount = null;

        if (request.NewStatus == "AppointmentCancelled")
        {
            var refundResult = await ProcessRefund(serviceRequest, userId: null, now, cancellationToken);
            refundedAmount = refundResult;
        }

        // ── AuditLog ──────────────────────────────────────────────────
        var noteMessage = BuildMessage(
            serviceRequest.ReferenceNumber,
            request.NewStatus,
            request.RejectionReason,
            refundedAmount);

        await context.RequestAuditLogs.AddAsync(new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            Action = $"StatusChanged:{oldStatus}->{request.NewStatus}",
            Notes = noteMessage,
            CreatedAt = now,
            ChangedByUserId = request.ChangedByUserId
        }, cancellationToken);

        // ── إشعار للمواطن ─────────────────────────────────────────────
        await context.Notifications.AddAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = serviceRequest.UserId,
            Title = GetNotificationTitle(request.NewStatus),
            Message = noteMessage,
            NotificationType = GetNotificationType(request.NewStatus),
            IsRead = false,
            CreatedAt = now
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<UpdateRequestStatusResponse>.Success(new UpdateRequestStatusResponse
        {
            RequestId = serviceRequest.Id,
            ReferenceNumber = serviceRequest.ReferenceNumber,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            RefundedAmount = refundedAmount
        });
    }

    /// <summary>
    /// يسترد رسوم الخدمة للمحفظة عند إلغاء الموعد.
    /// يبحث عن WalletTransaction الأصلي ليعرف المبلغ المدفوع.
    /// </summary>
    private async Task<decimal?> ProcessRefund(
        ServiceRequest serviceRequest,
        Guid? userId,
        DateTime now,
        CancellationToken ct)
    {
        // جلب المعاملة الأصلية لمعرفة المبلغ المدفوع
        var originalTransaction = await context.WalletTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.ServiceRequestId == serviceRequest.Id &&
                t.TransactionType == "ServiceFeePayment", ct);

        if (originalTransaction is null) return null;

        // جلب المحفظة
        var wallet = await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == serviceRequest.UserId, ct);

        if (wallet is null) return null;

        // إضافة المبلغ للمحفظة
        wallet.Balance += originalTransaction.Amount;

        // تسجيل معاملة الاسترداد
        await context.WalletTransactions.AddAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = originalTransaction.Amount,
            TransactionType = "Refund",
            Description = $"استرداد رسوم طلب ملغي — {serviceRequest.ReferenceNumber}",
            ReferenceId = serviceRequest.ReferenceNumber,
            ServiceRequestId = serviceRequest.Id,
            CreatedAt = now
        }, ct);

        return originalTransaction.Amount;
    }

    private static string GetNotificationTitle(string status) => status switch
    {
        "DocumentsUnderReview" => "طلبك قيد مراجعة المستندات",
        "AppointmentConfirmed" => "تم تأكيد موعدك ✅",
        "AppointmentCancelled" => "تم إلغاء موعدك",
        "Completed" => "تم إنجاز طلبك ✅",
        "Rejected" => "تم رفض طلبك",
        _ => "تحديث حالة طلبك"
    };

    private static string GetNotificationType(string status) => status switch
    {
        "AppointmentConfirmed" or "Completed" => "Success",
        "AppointmentCancelled" or "Rejected" => "Error",
        "DocumentsUnderReview" => "Warning",
        _ => "Info"
    };

    private static string BuildMessage(
        string refNumber, string status,
        string? reason, decimal? refund) => status switch
        {
            "DocumentsUnderReview" =>
                $"طلبك {refNumber} قيد مراجعة المستندات من قِبل الموظف المختص",
            "AppointmentConfirmed" =>
                $"تم تأكيد موعدك للطلب {refNumber} — يرجى الحضور في الوقت المحدد بالمستندات الأصلية",
            "AppointmentCancelled" =>
                $"تم إلغاء موعدك للطلب {refNumber}. السبب: {reason}" +
                (refund.HasValue ? $" — تم استرداد {refund:F2} ريال لمحفظتك" : ""),
            "Completed" =>
                $"تم إنجاز طلبك {refNumber} بنجاح",
            "Rejected" =>
                $"تم رفض طلبك {refNumber}. السبب: {reason}",
            _ =>
                $"تم تحديث حالة طلبك {refNumber}"
        };
}
