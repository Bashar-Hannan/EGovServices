using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Citizen.Queries.GetAvailableSlots;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetAvailableSlotsQuery : IRequest<Result<List<AvailableSlotDto>>>
{
    public required Guid GovernmentServiceId { get; init; }
    public DateOnly? FromDate { get; init; }    // اختياري — لفلترة بالتاريخ
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record AvailableSlotDto
{
    public required Guid SlotId { get; init; }
    public required DateOnly SlotDate { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public required int AvailableSeats { get; init; }   // المقاعد المتبقية
    public required int TotalSeats { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetAvailableSlotsHandler(IAppDbContext context)
    : IRequestHandler<GetAvailableSlotsQuery, Result<List<AvailableSlotDto>>>
{
    public async Task<Result<List<AvailableSlotDto>>> Handle(
        GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.FromDate ?? today;

        var slots = await context.AppointmentSlots
            .AsNoTracking()
            .Where(s =>
                s.GovernmentServiceId == request.GovernmentServiceId &&
                s.IsActive &&
                s.SlotDate >= fromDate &&
                s.BookedSeats < s.TotalSeats)   // يُظهر فقط ما فيه مقاعد متاحة
            .OrderBy(s => s.SlotDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new AvailableSlotDto
            {
                SlotId         = s.Id,
                SlotDate       = s.SlotDate,
                StartTime      = s.StartTime,
                EndTime        = s.EndTime,
                AvailableSeats = s.TotalSeats - s.BookedSeats,
                TotalSeats     = s.TotalSeats
            })
            .ToListAsync(cancellationToken);

        return Result<List<AvailableSlotDto>>.Success(slots);
    }
}
