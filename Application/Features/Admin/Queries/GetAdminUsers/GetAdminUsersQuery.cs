using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries.GetAdminUsers;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetAdminUsersQuery : IRequest<Result<PagedResult<AdminUserDto>>>
{
    public string? SearchTerm { get; init; }   // بحث بالاسم أو رقم الهوية
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record AdminUserDto
{
    public required Guid Id { get; init; }
    public required string NationalNumber { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public required string Role { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsVerified { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? FullName { get; init; }
    public required int RequestCount { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetAdminUsersHandler(IAppDbContext context)
    : IRequestHandler<GetAdminUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(
        GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.Citizen)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(u =>
                u.NationalNumber.Contains(request.SearchTerm) ||
                (u.Citizen != null && (
                    u.Citizen.FirstName.Contains(request.SearchTerm) ||
                    u.Citizen.LastName.Contains(request.SearchTerm))));

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new AdminUserDto
            {
                Id             = u.Id,
                NationalNumber = u.NationalNumber,
                PhoneNumber    = u.PhoneNumber,
                Email          = u.Email,
                Role           = u.Role,
                IsActive       = u.IsActive,
                IsVerified     = u.IsVerified,
                CreatedAt      = u.CreatedAt,
                FullName       = u.Citizen != null
                    ? $"{u.Citizen.FirstName} {u.Citizen.LastName}"
                    : null,
                RequestCount   = u.ServiceRequests.Count
            })
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AdminUserDto>>.Success(new PagedResult<AdminUserDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize
        });
    }
}
