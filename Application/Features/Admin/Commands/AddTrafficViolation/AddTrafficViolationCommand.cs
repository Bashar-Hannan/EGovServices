using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.AddTrafficViolation;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record AddTrafficViolationCommand : IRequest<Result<AddTrafficViolationResponse>>
{
    public required string CitizenNationalNumber { get; init; }
    public required string PlateNumber { get; init; }
    public required string ViolationType { get; init; }
    public string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required string Location { get; init; }
    public required DateTime ViolationDate { get; init; }

    // مصدر المخالفة — اختياري: إمّا ضابط، أو كاميرا آلية
    public string? IssuedByOfficerName { get; init; }
    public string? VehicleType { get; init; }
    public string? CameraId { get; init; }
}

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record AddTrafficViolationResponse(
    Guid Id,
    string ViolationNumber,
    string Message
);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class AddTrafficViolationValidator : AbstractValidator<AddTrafficViolationCommand>
{
    public AddTrafficViolationValidator()
    {
        RuleFor(x => x.CitizenNationalNumber)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("رقم الهوية مطلوب (بحد أقصى 20 محرف — نفس عمود NationalNumber بجدول Citizens)");

        RuleFor(x => x.PlateNumber)
            .NotEmpty()
            .WithMessage("رقم اللوحة مطلوب");

        RuleFor(x => x.ViolationType)
            .NotEmpty()
            .WithMessage("نوع المخالفة مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("قيمة المخالفة يجب أن تكون أكبر من صفر");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("موقع المخالفة مطلوب");

        RuleFor(x => x.ViolationDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("تاريخ المخالفة لا يمكن أن يكون بالمستقبل");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class AddTrafficViolationHandler(IAppDbContext context)
    : IRequestHandler<AddTrafficViolationCommand, Result<AddTrafficViolationResponse>>
{
    public async Task<Result<AddTrafficViolationResponse>> Handle(
        AddTrafficViolationCommand request, CancellationToken cancellationToken)
    {
        var citizenExists = await context.Citizens
            .AnyAsync(c => c.NationalNumber == request.CitizenNationalNumber, cancellationToken);

        if (!citizenExists)
            return Result<AddTrafficViolationResponse>.Failure(
                $"لا يوجد مواطن مسجَّل برقم الهوية {request.CitizenNationalNumber}");

        var violationNumber = GenerateViolationNumber();

        var violation = new TrafficViolation
        {
            Id = Guid.NewGuid(),
            ViolationNumber = violationNumber,
            PlateNumber = request.PlateNumber,
            CitizenNationalNumber = request.CitizenNationalNumber,
            ViolationType = request.ViolationType,
            Description = request.Description,
            Amount = request.Amount,
            Location = request.Location,
            ViolationDate = request.ViolationDate,
            Status = "Open",
            IssuedByOfficerName = request.IssuedByOfficerName,
            VehicleType = request.VehicleType,
            CameraId = request.CameraId
        };

        await context.TrafficViolations.AddAsync(violation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<AddTrafficViolationResponse>.Success(new AddTrafficViolationResponse(
            Id: violation.Id,
            ViolationNumber: violationNumber,
            Message: $"تمت إضافة المخالفة {violationNumber} بنجاح"
        ));
    }

    private static string GenerateViolationNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var uniquePart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"VIO-{timestamp}-{uniquePart}";
    }
}
