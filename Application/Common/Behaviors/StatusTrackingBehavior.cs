using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Common.Behaviors;

/// <summary>
/// Automatically logs RequestAuditLog and sends Notification
/// for any Command that implements IStatusChangingCommand.
/// Runs alongside ValidationBehavior in the MediatR pipeline.
/// </summary>
public class StatusTrackingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAppDbContext _context;

    public StatusTrackingBehavior(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only intercept commands that change status
        if (request is not IStatusChangingCommand statusCommand)
            return await next();

        // ── 1. Read old status BEFORE execution ──────────────────────
        var oldStatus = await _context.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Id == statusCommand.ServiceRequestId)
            .Select(r => new { r.Status, r.UserId, r.ReferenceNumber })
            .FirstOrDefaultAsync(cancellationToken);

        // ── 2. Execute the command ────────────────────────────────────
        var response = await next();

        // ── 3. Read new status AFTER execution ───────────────────────
        var newStatus = await _context.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Id == statusCommand.ServiceRequestId)
            .Select(r => r.Status)
            .FirstOrDefaultAsync(cancellationToken);

        // ── 4. If status changed → log + notify ──────────────────────
        if (oldStatus is not null && newStatus is not null && oldStatus.Status != newStatus)
        {
            var now = DateTime.UtcNow;

            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = statusCommand.ServiceRequestId,
                OldStatus = oldStatus.Status,    // string? — متوافق مع Entity المحدّث
                NewStatus = newStatus,
                Action = $"Status changed from {oldStatus.Status} to {newStatus}",
                Notes = GetNoteForStatus(newStatus),
                CreatedAt = now
            });

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = oldStatus.UserId,
                Title = "تحديث حالة الطلب",
                Message = $"تم تحديث طلبك {oldStatus.ReferenceNumber} إلى: {GetArabicStatus(newStatus)}",
                NotificationType = GetNotificationType(newStatus),
                IsRead = false,
                CreatedAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static string GetNoteForStatus(string status) => status switch
    {
        "PendingPaymentPayment" => "في انتظار إتمام عملية الدفع",
        "Submitted"      => "تم استلام طلبك بنجاح وهو في طابور المعالجة",
        "UnderReview"    => "طلبك قيد المراجعة من قبل الموظف المختص",
        "Approved"       => "تمت الموافقة على طلبك",
        "Rejected"       => "تم رفض طلبك، يرجى مراجعة ملاحظات الطلب",
        "Processing"     => "جارٍ معالجة طلبك وتوليد المستندات",
        "Completed"      => "تم إنجاز طلبك بنجاح",
        _                => "تم تحديث حالة الطلب"
    };

    private static string GetArabicStatus(string status) => status switch
    {
        "PendingPaymentPayment" => "في انتظار الدفع",
        "Submitted"      => "تم التقديم",
        "UnderReview"    => "قيد المراجعة",
        "Approved"       => "موافق عليه",
        "Rejected"       => "مرفوض",
        "Processing"     => "قيد المعالجة",
        "Completed"      => "مكتمل",
        _                => status
    };

    private static string GetNotificationType(string status) => status switch
    {
        "Rejected"                  => "Error",
        "Approved" or "Completed"  => "Success",
        "UnderReview"               => "Warning",
        _                           => "Info"
    };
}
