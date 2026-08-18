using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed record NotificationDto
{
    public required Guid     Id               { get; init; }
    public required string   Title            { get; init; }
    public required string   Message          { get; init; }
    public required string   NotificationType { get; init; }
    public required bool     IsRead           { get; init; }
    public required DateTime CreatedAt        { get; init; }
}

public sealed record GetMyNotificationsResponse
{
    public required int UnreadCount { get; init; }
    public required List<NotificationDto> Notifications { get; init; }
}

public sealed record GetMyNotificationsQuery(Guid UserId, bool UnreadOnly = false)
    : IRequest<Result<GetMyNotificationsResponse>>;

public sealed class GetMyNotificationsHandler(IAppDbContext context)
    : IRequestHandler<GetMyNotificationsQuery, Result<GetMyNotificationsResponse>>
{
    public async Task<Result<GetMyNotificationsResponse>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == request.UserId);

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)   // حد أقصى — يمنع جلب آلاف السجلات لمستخدم قديم
            .Select(n => new NotificationDto
            {
                Id               = n.Id,
                Title            = n.Title,
                Message          = n.Message,
                NotificationType = n.NotificationType,
                IsRead           = n.IsRead,
                CreatedAt        = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var unreadCount = await context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == request.UserId && !n.IsRead, cancellationToken);

        return Result<GetMyNotificationsResponse>.Success(new GetMyNotificationsResponse
        {
            UnreadCount   = unreadCount,
            Notifications = notifications
        });
    }
}
