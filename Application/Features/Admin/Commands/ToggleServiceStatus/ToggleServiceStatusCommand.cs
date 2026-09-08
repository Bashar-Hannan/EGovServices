using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.ToggleServiceStatus;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record ToggleServiceStatusCommand(Guid ServiceId)
    : IRequest<Result<ToggleServiceStatusResponse>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record ToggleServiceStatusResponse
{
    public required Guid ServiceId { get; init; }
    public required string ServiceName { get; init; }
    public required bool IsActive { get; init; }
    public required string Message { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class ToggleServiceStatusHandler(IAppDbContext context)
    : IRequestHandler<ToggleServiceStatusCommand, Result<ToggleServiceStatusResponse>>
{
    public async Task<Result<ToggleServiceStatusResponse>> Handle(
        ToggleServiceStatusCommand request, CancellationToken cancellationToken)
    {
        var service = await context.GovernmentServices
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);

        if (service is null)
            return Result<ToggleServiceStatusResponse>.Failure("الخدمة غير موجودة");

        service.IsActive = !service.IsActive;
        await context.SaveChangesAsync(cancellationToken);

        return Result<ToggleServiceStatusResponse>.Success(new ToggleServiceStatusResponse
        {
            ServiceId   = service.Id,
            ServiceName = service.Name,
            IsActive    = service.IsActive,
            Message     = service.IsActive ? "تم تفعيل الخدمة بنجاح" : "تم تعطيل الخدمة بنجاح"
        });
    }
}
