using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Ministries.Queries.GetMinistryBranches;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetMinistryBranchesQuery(Guid MinistryId)
    : IRequest<Result<List<MinistryBranchDto>>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record MinistryBranchDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public string? PhoneNumber { get; init; } // تم تغييره لـ string? لأنه في Branch.cs قد يكون null

    // للخريطة في الواجهة الأمامية
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
        // جلب الفروع النشطة للجهة الحكومية المحددة مع ترتيبها
        var branches = await context.Branches
            .AsNoTracking()
            .Where(b => b.GovernmentEntityId == request.MinistryId && b.IsActive)
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

        // إرجاع القائمة حتى لو كانت فارغة (Success)
        // إذا كان منطق العمل يتطلب فشل الطلب عند عدم وجود فروع، يمكن إضافة تحقق هنا.
        return Result<List<MinistryBranchDto>>.Success(branches);
    }
}