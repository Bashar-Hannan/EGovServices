using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Hospitals.Queries.GetHospitalFilters;

// ─── Query ────────────────────────────────────────────────────────────────────
// يستخدمها الفرونت لملء قائمتي فلترة "المحافظة" و"الاختصاص" ديناميكياً
// من البيانات الفعلية. القيم عربية (نفس ما هي مخزّنة بالجدول).
public sealed record GetHospitalFiltersQuery : IRequest<Result<HospitalFiltersDto>>
{
    public required Guid MinistryId { get; init; }
}

public sealed record HospitalFiltersDto
{
    public required List<string> Governorates { get; init; }
    public required List<string> Specialties { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetHospitalFiltersHandler(IAppDbContext context)
    : IRequestHandler<GetHospitalFiltersQuery, Result<HospitalFiltersDto>>
{
    public async Task<Result<HospitalFiltersDto>> Handle(
        GetHospitalFiltersQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = context.Hospitals
            .AsNoTracking()
            .Where(h => h.GovernmentEntityId == request.MinistryId && h.IsActive);

        var governorates = await baseQuery
            .Select(h => h.Governorate)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(cancellationToken);

        var specialties = await baseQuery
            .Where(h => h.Specialty != null)
            .Select(h => h.Specialty!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

        return Result<HospitalFiltersDto>.Success(new HospitalFiltersDto
        {
            Governorates = governorates,
            Specialties = specialties
        });
    }
}
