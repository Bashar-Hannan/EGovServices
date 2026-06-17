using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Ministries.Queries.GetMinistryById;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetMinistryByIdQuery(Guid MinistryId)
    : IRequest<Result<MinistryDetailsDto>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record MinistryDetailsDto
{
    public required Guid   Id           { get; init; }
    public required string Name         { get; init; }
    public required string Description  { get; init; }
    public required int    ServiceCount { get; init; }
    public required int    BranchCount  { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetMinistryByIdHandler(IAppDbContext context)
    : IRequestHandler<GetMinistryByIdQuery, Result<MinistryDetailsDto>>
{
    public async Task<Result<MinistryDetailsDto>> Handle(
        GetMinistryByIdQuery request, CancellationToken cancellationToken)
    {
        var ministry = await context.GovernmentEntities
            .AsNoTracking()
            .Where(e => e.Id == request.MinistryId && e.IsActive)
            .Select(e => new MinistryDetailsDto
            {
                Id           = e.Id,
                Name         = e.Name,
                Description  = e.Description,
                ServiceCount = e.Services.Count(s => s.IsActive),
                BranchCount  = e.Branches.Count(b => b.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ministry is null)
            return Result<MinistryDetailsDto>.Failure("الوزارة غير موجودة");

        return Result<MinistryDetailsDto>.Success(ministry);
    }
}
