using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.UpsertMedicalRecord;

// ── Command ───────────────────────────────────────────────────────────────────
// Upsert: سجل طبي واحد فقط لكل مواطن (قيد Unique بقاعدة البيانات) —
// لو موجود سابقاً يُعدَّل، لو غير موجود يُنشأ.
public sealed record UpsertMedicalRecordCommand : IRequest<Result<UpsertMedicalRecordResponse>>
{
    public required string CitizenNationalNumber { get; init; }
    public required string BloodType { get; init; }
    public required int HeightCm { get; init; }
    public required int WeightKg { get; init; }
    public required string Allergies { get; init; }         // "لا يوجد" إذا لا توجد
    public required string ChronicDiseases { get; init; }   // "لا يوجد" إذا لا توجد
}

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record UpsertMedicalRecordResponse(
    Guid Id,
    string CitizenNationalNumber,
    bool WasCreated,   // true = سجل جديد، false = تعديل سجل موجود
    string Message
);

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class UpsertMedicalRecordValidator : AbstractValidator<UpsertMedicalRecordCommand>
{
    public UpsertMedicalRecordValidator()
    {
        RuleFor(x => x.CitizenNationalNumber)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("رقم الهوية مطلوب (بحد أقصى 20 محرف — نفس عمود NationalNumber بجدول Citizens)");

        RuleFor(x => x.BloodType)
            .NotEmpty()
            .Must(b => new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }.Contains(b))
            .WithMessage("فصيلة الدم يجب أن تكون إحدى القيم المعروفة: A+, A-, B+, B-, AB+, AB-, O+, O-");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(30, 250)
            .WithMessage("الطول يجب أن يكون بين 30 و250 سم");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(1, 400)
            .WithMessage("الوزن يجب أن يكون بين 1 و400 كغ");

        RuleFor(x => x.Allergies)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("الحساسيات مطلوبة — اكتب \"لا يوجد\" في حال عدم وجودها");

        RuleFor(x => x.ChronicDiseases)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("الأمراض المزمنة مطلوبة — اكتب \"لا يوجد\" في حال عدم وجودها");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class UpsertMedicalRecordHandler(IAppDbContext context)
    : IRequestHandler<UpsertMedicalRecordCommand, Result<UpsertMedicalRecordResponse>>
{
    public async Task<Result<UpsertMedicalRecordResponse>> Handle(
        UpsertMedicalRecordCommand request, CancellationToken cancellationToken)
    {
        var citizenExists = await context.Citizens
            .AnyAsync(c => c.NationalNumber == request.CitizenNationalNumber, cancellationToken);

        if (!citizenExists)
            return Result<UpsertMedicalRecordResponse>.Failure(
                $"لا يوجد مواطن مسجَّل برقم الهوية {request.CitizenNationalNumber}");

        var existing = await context.MedicalRecords
            .FirstOrDefaultAsync(m => m.CitizenNationalNumber == request.CitizenNationalNumber, cancellationToken);

        bool wasCreated;
        Guid recordId;

        if (existing is null)
        {
            var record = new MedicalRecord
            {
                Id = Guid.NewGuid(),
                CitizenNationalNumber = request.CitizenNationalNumber,
                BloodType = request.BloodType,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                Allergies = request.Allergies,
                ChronicDiseases = request.ChronicDiseases,
                UpdatedAt = DateTime.UtcNow
            };

            await context.MedicalRecords.AddAsync(record, cancellationToken);
            wasCreated = true;
            recordId = record.Id;
        }
        else
        {
            existing.BloodType = request.BloodType;
            existing.HeightCm = request.HeightCm;
            existing.WeightKg = request.WeightKg;
            existing.Allergies = request.Allergies;
            existing.ChronicDiseases = request.ChronicDiseases;
            existing.UpdatedAt = DateTime.UtcNow;

            wasCreated = false;
            recordId = existing.Id;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<UpsertMedicalRecordResponse>.Success(new UpsertMedicalRecordResponse(
            Id: recordId,
            CitizenNationalNumber: request.CitizenNationalNumber,
            WasCreated: wasCreated,
            Message: wasCreated ? "تم إنشاء الملف الطبي بنجاح" : "تم تحديث الملف الطبي بنجاح"
        ));
    }
}
