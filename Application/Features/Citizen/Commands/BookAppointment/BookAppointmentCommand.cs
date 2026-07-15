using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using EGovServices.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Citizen.Commands.BookAppointment;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record BookAppointmentCommand : IRequest<Result<BookAppointmentResponse>>
{
    public required Guid UserId { get; init; }              // من الـ JWT
    public required Guid GovernmentServiceId { get; init; }
    public required Guid AppointmentSlotId { get; init; }
    public required string FormData { get; init; }          // JSON للحقول الديناميكية
}

// ─── Response DTO ─────────────────────────────────────────────────────────────
public sealed record BookAppointmentResponse
{
    public required Guid RequestId { get; init; }
    public required string ReferenceNumber { get; init; }
    public required DateOnly AppointmentDate { get; init; }
    public required TimeOnly AppointmentTime { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}

// ─── Validator ────────────────────────────────────────────────────────────────
public sealed class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.FormData)
            .NotEmpty()
            .WithMessage("بيانات النموذج مطلوبة");

        RuleFor(x => x.AppointmentSlotId)
            .NotEmpty()
            .WithMessage("يجب اختيار موعد");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class BookAppointmentHandler(IAppDbContext context)
    : IRequestHandler<BookAppointmentCommand, Result<BookAppointmentResponse>>
{
    public async Task<Result<BookAppointmentResponse>> Handle(
        BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        // 1. التحقق أن الخدمة موجودة وهي خدمة موعد
        var service = await context.GovernmentServices
            .FirstOrDefaultAsync(s => s.Id == request.GovernmentServiceId, cancellationToken);

        if (service is null)
            return Result<BookAppointmentResponse>.Failure("الخدمة غير موجودة");

        if (service.ServiceType != ServiceType.Appointment)
            return Result<BookAppointmentResponse>.Failure(
                "هذه الخدمة رقمية ولا تحتاج حجز موعد");

        // 2. القيد: لا يمكن التقديم على نفس الخدمة أكثر من مرة
        var hasActiveRequest = await context.ServiceRequests
            .AnyAsync(r =>
                r.UserId == request.UserId &&
                r.GovernmentServiceId == request.GovernmentServiceId &&
                !r.IsDeleted &&
                r.Status != RequestStatus.Completed &&
                r.Status != RequestStatus.AppointmentCancelled,
                cancellationToken);

        if (hasActiveRequest)
            return Result<BookAppointmentResponse>.Failure(
                "لديك طلب نشط لهذه الخدمة، لا يمكنك التقديم مجدداً حتى إتمامه أو إلغائه");

        // 3. التحقق من الموعد وتوفر مقاعد — مع Lock لمنع Race Condition
        var slot = await context.AppointmentSlots
            .FirstOrDefaultAsync(s =>
                s.Id == request.AppointmentSlotId &&
                s.GovernmentServiceId == request.GovernmentServiceId &&
                s.IsActive,
                cancellationToken);

        if (slot is null)
            return Result<BookAppointmentResponse>.Failure("الموعد غير موجود أو غير متاح");

        if (slot.IsFull)
            return Result<BookAppointmentResponse>.Failure(
                "عذراً، هذا الموعد ممتلئ. يرجى اختيار موعد آخر");

        if (slot.SlotDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<BookAppointmentResponse>.Failure(
                "لا يمكن حجز موعد في تاريخ ماضٍ");

        // 4. إنشاء الطلب
        var referenceNumber = GenerateReferenceNumber();
        var serviceRequest = new ServiceRequest
        {
            Id                  = Guid.NewGuid(),
            UserId              = request.UserId,
            GovernmentServiceId = request.GovernmentServiceId,
            AppointmentSlotId   = slot.Id,
            ReferenceNumber     = referenceNumber,
            Status              = RequestStatus.PendingPayment,
            FormData            = request.FormData,
            SubmissionDate      = DateTime.UtcNow,
            IsDeleted           = false
        };

        // 5. زيادة المقاعد المحجوزة
        slot.BookedSeats++;

        // 6. إشعار تأكيد للمواطن
        var notification = new Notification
        {
            Id               = Guid.NewGuid(),
            UserId           = request.UserId,
            Title            = "تم استلام طلبك",
            Message          = $"تم حجز موعدك بنجاح في {slot.SlotDate} الساعة {slot.StartTime:hh\\:mm}. " +
                               $"رقم الطلب: {referenceNumber}. سيتم مراجعة ملفاتك قريباً.",
            NotificationType = "Success",
            IsRead           = false,
            CreatedAt        = DateTime.UtcNow
        };

        await context.ServiceRequests.AddAsync(serviceRequest, cancellationToken);
        await context.Notifications.AddAsync(notification, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<BookAppointmentResponse>.Success(new BookAppointmentResponse
        {
            RequestId       = serviceRequest.Id,
            ReferenceNumber = referenceNumber,
            AppointmentDate = slot.SlotDate,
            AppointmentTime = slot.StartTime,
            Status          = serviceRequest.Status,
            Message         = "تم الحجز بنجاح، سيقوم الموظف بمراجعة ملفاتك"
        });
    }

    private static string GenerateReferenceNumber() =>
        $"APT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
