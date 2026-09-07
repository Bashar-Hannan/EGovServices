using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Ministries.Queries.GetMinistryBranches;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetMinistryBranchesQuery : IRequest<Result<List<MinistryBranchDto>>>
{
    public required Guid MinistryId { get; init; }

    /// <summary>
    /// ✅ جديد — فلترة اختيارية بالمحافظة (عمود City في Branch).
    /// null يعني إرجاع كل الفروع بدون فلترة (السلوك القديم كما هو).
    /// تُستخدم بواسطة خدمة "عرض المستشفيات الحكومية" تحت وزارة الصحة.
    /// </summary>
    public string? Governorate { get; init; }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record MinistryBranchDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public string? PhoneNumber { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetMinistryBranchesHandler(IAppDbContext context)
    : IRequestHandler<GetMinistryBranchesQuery, Result<List<MinistryBranchDto>>>
{
    public async Task<Result<List<MinistryBranchDto>>> Handle(
        GetMinistryBranchesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Branches
            .AsNoTracking()
            .Where(b => b.GovernmentEntityId == request.MinistryId && b.IsActive);

        // ✅ فلترة بالمحافظة إذا طُلبت
        if (!string.IsNullOrWhiteSpace(request.Governorate))
            query = query.Where(b => b.City == request.Governorate);

        var branches = await query
            .OrderBy(b => b.City)
            .ThenBy(b => b.Name)
            .Select(b => new MinistryBranchDto
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address,
                City = b.City,
                PhoneNumber = b.PhoneNumber,
                Latitude = b.Latitude,
                Longitude = b.Longitude
            })
            .ToListAsync(cancellationToken);

        return Result<List<MinistryBranchDto>>.Success(branches);
    }
}
