using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Employee.Commands.UpdateRequestStatus;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record UpdateRequestStatusCommand : IRequest<Result<UpdateRequestStatusResponse>>
{
    public required Guid RequestId { get; init; }
    public required Guid ChangedByUserId { get; init; }   // من الـ JWT claim
    public required string NewStatus { get; init; }       // "UnderReview","Approved","Rejected","Completed"
    public string? RejectionReason { get; init; }         // مطلوب عند الرفض فقط
    public string? ProcessingNotes { get; init; }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record UpdateRequestStatusResponse
{
    public required Guid RequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
}

// ─── Validator ────────────────────────────────────────────────────────────────
public sealed class UpdateRequestStatusValidator : AbstractValidator<UpdateRequestStatusCommand>
{
    private static readonly string[] AllowedStatuses =
        ["UnderReview", "Approved", "Rejected", "Completed"];

    public UpdateRequestStatusValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"الحالة يجب أن تكون إحدى: {string.Join(", ", AllowedStatuses)}");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => x.NewStatus == "Rejected")
            .WithMessage("سبب الرفض مطلوب عند رفض الطلب");

        RuleFor(x => x.ChangedByUserId)
            .NotEmpty()
            .WithMessage("معرّف المستخدم المنفذ مطلوب");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class UpdateRequestStatusHandler(IAppDbContext context)
    : IRequestHandler<UpdateRequestStatusCommand, Result<UpdateRequestStatusResponse>>
{
    // State machine — الانتقالات المسموحة فقط
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        ["PendingPayment"]     = ["UnderReview", "Rejected"],
        ["UnderReview"] = ["Approved", "Rejected"],
        ["Approved"]    = ["Completed"],
        ["Rejected"]    = [],
        ["Completed"]   = []
    };

    public async Task<Result<UpdateRequestStatusResponse>> Handle(
        UpdateRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (serviceRequest is null)
            return Result<UpdateRequestStatusResponse>.Failure("الطلب غير موجود");

        if (!ValidTransitions.TryGetValue(serviceRequest.Status, out var allowed)
            || !allowed.Contains(request.NewStatus))
        {
            return Result<UpdateRequestStatusResponse>.Failure(
                $"لا يمكن الانتقال من '{serviceRequest.Status}' إلى '{request.NewStatus}'");
        }

        var oldStatus = serviceRequest.Status;
        serviceRequest.Status = request.NewStatus;

        if (!string.IsNullOrWhiteSpace(request.ProcessingNotes))
            serviceRequest.ProcessingNotes = request.ProcessingNotes;

        if (request.NewStatus == "Rejected")
            serviceRequest.RejectionReason = request.RejectionReason;

        if (request.NewStatus == "Completed")
            serviceRequest.CompletedAt = DateTime.UtcNow;

        // سجل التدقيق مع ChangedByUserId ✅
        await context.RequestAuditLogs.AddAsync(new RequestAuditLog
        {
            Id               = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            NewStatus        = request.NewStatus,
            Action           = $"StatusChanged:{oldStatus}->{request.NewStatus}",
            CreatedAt        = DateTime.UtcNow,
            ChangedByUserId  = request.ChangedByUserId
        }, cancellationToken);

        // إشعار للمواطن
        await context.Notifications.AddAsync(new Notification
        {
            Id               = Guid.NewGuid(),
            UserId           = serviceRequest.UserId,
            Title            = "تحديث حالة طلبك",
            Message          = BuildMessage(serviceRequest.ReferenceNumber,
                                   request.NewStatus, request.RejectionReason),
            NotificationType = request.NewStatus == "Rejected" ? "Warning" : "Info",
            IsRead           = false,
            CreatedAt        = DateTime.UtcNow
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<UpdateRequestStatusResponse>.Success(new UpdateRequestStatusResponse
        {
            RequestId       = serviceRequest.Id,
            ReferenceNumber = serviceRequest.ReferenceNumber,
            OldStatus       = oldStatus,
            NewStatus       = request.NewStatus
        });
    }

    private static string BuildMessage(string refNumber, string status, string? reason) =>
        status switch
        {
            "UnderReview" => $"طلبك {refNumber} قيد المراجعة الآن",
            "Approved"    => $"تمت الموافقة على طلبك {refNumber}",
            "Rejected"    => $"تم رفض طلبك {refNumber}. السبب: {reason}",
            "Completed"   => $"تم إكمال طلبك {refNumber} بنجاح",
            _             => $"تم تحديث حالة طلبك {refNumber}"
        };
}
