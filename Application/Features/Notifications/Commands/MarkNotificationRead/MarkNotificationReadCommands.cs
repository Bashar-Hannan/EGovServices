using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Notifications.Commands.MarkNotificationRead;

// ── تعليم إشعار واحد كمقروء ──────────────────────────────────────────
public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId)
    : IRequest<Result>;

public sealed class MarkNotificationReadHandler(IAppDbContext context)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(
        MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId && n.UserId == request.UserId,
                cancellationToken);

        if (notification is null)
            return Result.Failure("الإشعار غير موجود");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
