using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries.GetMedicalRecordByNationalNumber;

// ─── Query ────────────────────────────────────────────────────────────────────
// تجلب السجل الطبي الحالي للمواطن (إن وُجد) — تُستخدم لتعبئة نموذج
// التعديل بالداشبورد قبل إرسال UpsertMedicalRecordCommand.
public sealed record GetMedicalRecordByNationalNumberQuery(string NationalNumber)
    : IRequest<Result<MedicalRecordDto?>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record MedicalRecordDto
{
    public required Guid Id { get; init; }
    public required string CitizenNationalNumber { get; init; }
    public required string BloodType { get; init; }
    public required int HeightCm { get; init; }
    public required int WeightKg { get; init; }
    public required string Allergies { get; init; }
    public required string ChronicDiseases { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetMedicalRecordByNationalNumberHandler(IAppDbContext context)
    : IRequestHandler<GetMedicalRecordByNationalNumberQuery, Result<MedicalRecordDto?>>
{
    public async Task<Result<MedicalRecordDto?>> Handle(
        GetMedicalRecordByNationalNumberQuery request, CancellationToken cancellationToken)
    {
        var citizenExists = await context.Citizens
            .AnyAsync(c => c.NationalNumber == request.NationalNumber, cancellationToken);

        if (!citizenExists)
            return Result<MedicalRecordDto?>.Failure(
                $"لا يوجد مواطن مسجَّل برقم الهوية {request.NationalNumber}");

        var record = await context.MedicalRecords
            .AsNoTracking()
            .Where(m => m.CitizenNationalNumber == request.NationalNumber)
            .Select(m => new MedicalRecordDto
            {
                Id = m.Id,
                CitizenNationalNumber = m.CitizenNationalNumber,
                BloodType = m.BloodType,
                HeightCm = m.HeightCm,
                WeightKg = m.WeightKg,
                Allergies = m.Allergies,
                ChronicDiseases = m.ChronicDiseases,
                UpdatedAt = m.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        // null = لا يوجد سجل طبي بعد لهذا المواطن (وليس خطأً — الفرونت يعرض نموذجاً فارغاً)
        return Result<MedicalRecordDto?>.Success(record);
    }
}
