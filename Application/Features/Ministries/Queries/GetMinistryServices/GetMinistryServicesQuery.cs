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
//public sealed record MinistryServiceDto
//{
//    public required Guid        Id          { get; init; }
//    public required string      Name        { get; init; }
//    public required string      Description { get; init; }
//    public required string      Requirements { get; init; }
//    public required decimal     ServiceFee  { get; init; }
//    public required ServiceType ServiceType { get; init; }  // Digital | Appointment

//    /// <summary>
//    /// نص وصفي للنوع — يُعرض مباشرةً في الواجهة
//    /// "إلكتروني بالكامل" أو "يتطلب حضوراً"
//    /// </summary>
//    public string ServiceTypeLabel => ServiceType == ServiceType.Digital
//        ? "إلكتروني بالكامل"
//        : "يتطلب حضوراً";
//}
public sealed record MinistryServiceDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string ServiceType { get; init; }
    public required decimal ServiceFee { get; init; }
    public string? Requirements { get; init; }

    // فقط تُملأ عندما ServiceType = "Appointment"
    // عند Digital تكون null مباشرة — الـ Frontend لا يرى الحقل أصلاً
    public List<BranchOptionDto>? Branches { get; init; }
}

public sealed record BranchOptionDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

// Application/Features/Ministries/GetMinistryServicesQuery.cs

public sealed class GetMinistryServicesHandler
    : IRequestHandler<GetMinistryServicesQuery, Result<List<MinistryServiceDto>>>
{
    private readonly IAppDbContext _context;
    public GetMinistryServicesHandler(IAppDbContext context) => _context = context;

    public async Task<Result<List<MinistryServiceDto>>> Handle(
        GetMinistryServicesQuery request, CancellationToken cancellationToken)
    {

        var branches = await _context.Branches
            .Where(b => b.GovernmentEntityId == request.MinistryId && b.IsActive)
            .Select(b => new BranchOptionDto
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address
            })
            .ToListAsync(cancellationToken);

        var services = await _context.GovernmentServices
            .Where(s => s.GovernmentEntityId == request.MinistryId && s.IsActive)
            .Select(s => new MinistryServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                ServiceType = s.ServiceType.ToString(),
                ServiceFee = s.ServiceFee,
                Requirements = s.Requirements,

                // Appointment → يُرجع الفروع | Digital → null
                Branches = s.ServiceType == ServiceType.Appointment
                    ? branches
                    : null
            })
            .ToListAsync(cancellationToken);

        return Result<List<MinistryServiceDto>>.Success(services);
    }
}