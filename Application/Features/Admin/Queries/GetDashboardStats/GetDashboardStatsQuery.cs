using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries.GetDashboardStats;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public sealed record DashboardStatsDto
{
    public required int TotalRequests { get; init; }
    public required int PendingPaymentRequests { get; init; }
    public required int UnderReviewRequests { get; init; }
    public required int ApprovedRequests { get; init; }
    public required int RejectedRequests { get; init; }
    public required int CompletedRequests { get; init; }
    public required int TotalUsers { get; init; }
    public required decimal TotalRevenue { get; init; }
    public required List<ServiceStatDto> TopServices { get; init; }
}

public sealed record ServiceStatDto
{
    public required string ServiceName { get; init; }
    public required int RequestCount { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetDashboardStatsHandler(IAppDbContext context)
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var requestStats = await context.ServiceRequests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalUsers = await context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalRevenue = await context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.TransactionType == "ServicePayment")
            .SumAsync(t => t.Amount, cancellationToken);

        var topServices = await context.ServiceRequests
            .AsNoTracking()
            .GroupBy(r => r.GovernmentService.Name)
            .Select(g => new ServiceStatDto
            {
                ServiceName = g.Key,
                RequestCount = g.Count()
            })
            .OrderByDescending(s => s.RequestCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Result<DashboardStatsDto>.Success(new DashboardStatsDto
        {
            TotalRequests       = requestStats.Sum(s => s.Count),
            PendingPaymentRequests     = requestStats.FirstOrDefault(s => s.Status == "PendingPayment")?.Count ?? 0,
            UnderReviewRequests = requestStats.FirstOrDefault(s => s.Status == "UnderReview")?.Count ?? 0,
            ApprovedRequests    = requestStats.FirstOrDefault(s => s.Status == "Approved")?.Count ?? 0,
            RejectedRequests    = requestStats.FirstOrDefault(s => s.Status == "Rejected")?.Count ?? 0,
            CompletedRequests   = requestStats.FirstOrDefault(s => s.Status == "Completed")?.Count ?? 0,
            TotalUsers          = totalUsers,
            TotalRevenue        = totalRevenue,
            TopServices         = topServices
        });
    }
}
