using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.ToggleUserStatus;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record ToggleUserStatusCommand(Guid UserId)
    : IRequest<Result<ToggleUserStatusResponse>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record ToggleUserStatusResponse
{
    public required Guid UserId { get; init; }
    public required bool IsActive { get; init; }
    public required string Message { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class ToggleUserStatusHandler(IAppDbContext context)
    : IRequestHandler<ToggleUserStatusCommand, Result<ToggleUserStatusResponse>>
{
    public async Task<Result<ToggleUserStatusResponse>> Handle(
        ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<ToggleUserStatusResponse>.Failure("المستخدم غير موجود");

        if (user.Role == "Admin")
            return Result<ToggleUserStatusResponse>.Failure("لا يمكن تعطيل حساب مسؤول");

        user.IsActive = !user.IsActive;
        await context.SaveChangesAsync(cancellationToken);

        return Result<ToggleUserStatusResponse>.Success(new ToggleUserStatusResponse
        {
            UserId   = user.Id,
            IsActive = user.IsActive,
            Message  = user.IsActive ? "تم تفعيل الحساب بنجاح" : "تم إيقاف الحساب بنجاح"
        });
    }
}
