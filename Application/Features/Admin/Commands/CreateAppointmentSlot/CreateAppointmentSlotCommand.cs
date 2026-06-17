using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using EGovServices.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.CreateAppointmentSlot;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record CreateAppointmentSlotCommand : IRequest<Result<CreateAppointmentSlotResponse>>
{
    public required Guid GovernmentServiceId { get; init; }
    public required Guid CreatedByAdminId { get; init; }    // من الـ JWT
    public required DateOnly SlotDate { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public required int TotalSeats { get; init; }
}

// ─── Response DTO ─────────────────────────────────────────────────────────────
public sealed record CreateAppointmentSlotResponse
{
    public required Guid SlotId { get; init; }
    public required DateOnly SlotDate { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public required int TotalSeats { get; init; }
}

// ─── Validator ────────────────────────────────────────────────────────────────
public sealed class CreateAppointmentSlotValidator
    : AbstractValidator<CreateAppointmentSlotCommand>
{
    public CreateAppointmentSlotValidator()
    {
        RuleFor(x => x.SlotDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("تاريخ الموعد يجب أن يكون في المستقبل");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("وقت الانتهاء يجب أن يكون بعد وقت البداية");

        RuleFor(x => x.TotalSeats)
            .GreaterThan(0)
            .WithMessage("عدد المقاعد يجب أن يكون أكبر من صفر");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class CreateAppointmentSlotHandler(IAppDbContext context)
    : IRequestHandler<CreateAppointmentSlotCommand, Result<CreateAppointmentSlotResponse>>
{
    public async Task<Result<CreateAppointmentSlotResponse>> Handle(
        CreateAppointmentSlotCommand request, CancellationToken cancellationToken)
    {
        // التحقق أن الخدمة موجودة وهي خدمة موعد
        var service = await context.GovernmentServices
            .FirstOrDefaultAsync(s => s.Id == request.GovernmentServiceId, cancellationToken);

        if (service is null)
            return Result<CreateAppointmentSlotResponse>.Failure("الخدمة غير موجودة");

        if (service.ServiceType != ServiceType.Appointment)
            return Result<CreateAppointmentSlotResponse>.Failure(
                "هذه الخدمة رقمية ولا تدعم المواعيد");

        // التحقق من عدم وجود تعارض في نفس اليوم والوقت
        var hasConflict = await context.AppointmentSlots
            .AnyAsync(s =>
                s.GovernmentServiceId == request.GovernmentServiceId &&
                s.SlotDate == request.SlotDate &&
                s.IsActive &&
                s.StartTime < request.EndTime &&
                s.EndTime > request.StartTime,
                cancellationToken);

        if (hasConflict)
            return Result<CreateAppointmentSlotResponse>.Failure(
                "يوجد موعد متعارض في نفس الوقت لهذه الخدمة");

        var slot = new AppointmentSlot
        {
            Id                  = Guid.NewGuid(),
            GovernmentServiceId = request.GovernmentServiceId,
            CreatedByAdminId    = request.CreatedByAdminId,
            SlotDate            = request.SlotDate,
            StartTime           = request.StartTime,
            EndTime             = request.EndTime,
            TotalSeats          = request.TotalSeats,
            BookedSeats         = 0,
            IsActive            = true,
            CreatedAt           = DateTime.UtcNow
        };

        await context.AppointmentSlots.AddAsync(slot, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CreateAppointmentSlotResponse>.Success(new CreateAppointmentSlotResponse
        {
            SlotId     = slot.Id,
            SlotDate   = slot.SlotDate,
            StartTime  = slot.StartTime,
            EndTime    = slot.EndTime,
            TotalSeats = slot.TotalSeats
        });
    }
}
