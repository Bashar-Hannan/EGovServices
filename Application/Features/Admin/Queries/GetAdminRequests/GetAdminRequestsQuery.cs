using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries.GetAdminRequests;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetAdminRequestsQuery : IRequest<Result<PagedResult<AdminRequestDto>>>
{
    public string? Status { get; init; }       // null = كل الحالات
    public Guid? ServiceId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record AdminRequestDto
{
    public required Guid Id { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string Status { get; init; }
    public required DateTime SubmissionDate { get; init; }
    public required string ServiceName { get; init; }
    public required string CitizenName { get; init; }
    public required string CitizenNationalNumber { get; init; }
    public string? ProcessingNotes { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetAdminRequestsHandler(IAppDbContext context)
    : IRequestHandler<GetAdminRequestsQuery, Result<PagedResult<AdminRequestDto>>>
{
    public async Task<Result<PagedResult<AdminRequestDto>>> Handle(
        GetAdminRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Citizen)
            .Include(r => r.GovernmentService)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);

        if (request.ServiceId.HasValue)
            query = query.Where(r => r.GovernmentServiceId == request.ServiceId);

        if (request.FromDate.HasValue)
            query = query.Where(r => r.SubmissionDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(r => r.SubmissionDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.SubmissionDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminRequestDto
            {
                Id = r.Id,
                ReferenceNumber = r.ReferenceNumber,
                Status = r.Status,
                SubmissionDate = r.SubmissionDate,
                ServiceName = r.GovernmentService.Name,
                CitizenName = r.User.Citizen != null
                    ? $"{r.User.Citizen.FirstName} {r.User.Citizen.LastName}"
                    : r.User.NationalNumber,
                CitizenNationalNumber = r.User.NationalNumber,
                ProcessingNotes = r.ProcessingNotes
            })
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AdminRequestDto>>.Success(new PagedResult<AdminRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
