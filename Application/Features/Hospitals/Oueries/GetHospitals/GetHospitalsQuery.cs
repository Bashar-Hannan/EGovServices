using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.Features.Hospitals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Hospitals.Queries.GetHospitals;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetHospitalsQuery : IRequest<Result<List<HospitalDto>>>
{
    public required Guid MinistryId { get; init; }   // GovernmentEntityId لوزارة الصحة

    /// <summary>فلترة اختيارية بالمحافظة — كود إنكليزي (مثال: Aleppo). null = كل المحافظات.</summary>
    public string? Governorate { get; init; }

    /// <summary>فلترة اختيارية بالاختصاص — كود إنكليزي (مثال: Pediatrics). null = كل الاختصاصات.</summary>
    public string? Specialty { get; init; }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record HospitalDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Governorate { get; init; }
    public string? Address { get; init; }
    public string? Specialty { get; init; }
    public string? InfrastructureStatus { get; init; }
    public string? OperationalStatus { get; init; }
    public int? BedsActual { get; init; }
    public int? BedsTheoretical { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AdminPhoneNumber { get; init; }
    public string? DirectorPhoneNumber { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetHospitalsHandler(IAppDbContext context)
    : IRequestHandler<GetHospitalsQuery, Result<List<HospitalDto>>>
{
    public async Task<Result<List<HospitalDto>>> Handle(
        GetHospitalsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Hospitals
            .AsNoTracking()
            .Where(h => h.GovernmentEntityId == request.MinistryId && h.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Governorate) && request.Governorate != "all_governorates")
        {
            var governorateArabic = HospitalLookups.GovernorateToArabic(request.Governorate);
            query = query.Where(h => h.Governorate == governorateArabic);
        }

        if (!string.IsNullOrWhiteSpace(request.Specialty))
        {
            var specialtyArabic = HospitalLookups.SpecialtyToArabic(request.Specialty);
            query = query.Where(h => h.Specialty == specialtyArabic);
        }

        var hospitals = await query
            .OrderBy(h => h.Governorate)
            .ThenBy(h => h.Name)
            .Select(h => new HospitalDto
            {
                Id = h.Id,
                Name = h.Name,
                Governorate = h.Governorate,
                Address = h.Address,
                Specialty = h.Specialty,
                InfrastructureStatus = h.InfrastructureStatus,
                OperationalStatus = h.OperationalStatus,
                BedsActual = h.BedsActual,
                BedsTheoretical = h.BedsTheoretical,
                PhoneNumber = h.PhoneNumber,
                AdminPhoneNumber = h.AdminPhoneNumber,
                DirectorPhoneNumber = h.DirectorPhoneNumber,
                Latitude = h.Latitude,
                Longitude = h.Longitude
            })
            .ToListAsync(cancellationToken);

        return Result<List<HospitalDto>>.Success(hospitals);
    }
}
