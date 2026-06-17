using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Ministries.Queries.GetMinistries;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetMinistriesQuery : IRequest<Result<List<MinistryDto>>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record MinistryDto
{
    public required Guid   Id          { get; init; }
    public required string Name        { get; init; }
    public required string Description { get; init; }
    public required int    ServiceCount { get; init; }  // عدد الخدمات للعرض في الكارد
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetMinistriesHandler(IAppDbContext context)
    : IRequestHandler<GetMinistriesQuery, Result<List<MinistryDto>>>
{
    public async Task<Result<List<MinistryDto>>> Handle(
        GetMinistriesQuery request, CancellationToken cancellationToken)
    {
        var ministries = await context.GovernmentEntities
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .Select(e => new MinistryDto
            {
                Id           = e.Id,
                Name         = e.Name,
                Description  = e.Description,
                ServiceCount = e.Services.Count(s => s.IsActive)
            })
            .ToListAsync(cancellationToken);

        return Result<List<MinistryDto>>.Success(ministries);
    }
}
