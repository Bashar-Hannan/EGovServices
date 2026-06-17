using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Ministries.Queries.GetMinistryServices;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetMinistryServicesQuery : IRequest<Result<List<MinistryServiceDto>>>
{
    public required Guid MinistryId { get; init; }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record MinistryServiceDto
{
    public required Guid        Id          { get; init; }
    public required string      Name        { get; init; }
    public required string      Description { get; init; }
    public required string      Requirements { get; init; }
    public required decimal     ServiceFee  { get; init; }
    public required ServiceType ServiceType { get; init; }  // Digital | Appointment

    /// <summary>
    /// نص وصفي للنوع — يُعرض مباشرةً في الواجهة
    /// "إلكتروني بالكامل" أو "يتطلب حضوراً"
    /// </summary>
    public string ServiceTypeLabel => ServiceType == ServiceType.Digital
        ? "إلكتروني بالكامل"
        : "يتطلب حضوراً";
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetMinistryServicesHandler(IAppDbContext context)
    : IRequestHandler<GetMinistryServicesQuery, Result<List<MinistryServiceDto>>>
{
    public async Task<Result<List<MinistryServiceDto>>> Handle(
        GetMinistryServicesQuery request, CancellationToken cancellationToken)
    {
        // التحقق أن الوزارة موجودة
        var ministryExists = await context.GovernmentEntities
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.MinistryId && e.IsActive, cancellationToken);

        if (!ministryExists)
            return Result<List<MinistryServiceDto>>.Failure("الوزارة غير موجودة");

        var services = await context.GovernmentServices
            .AsNoTracking()
            .Where(s => s.GovernmentEntityId == request.MinistryId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new MinistryServiceDto
            {
                Id           = s.Id,
                Name         = s.Name,
                Description  = s.Description,
                Requirements = s.Requirements,
                ServiceFee   = s.ServiceFee,
                ServiceType  = s.ServiceType
            })
            .ToListAsync(cancellationToken);

        return Result<List<MinistryServiceDto>>.Success(services);
    }
}
