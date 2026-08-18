using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.TrafficFines;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.TrafficFines.Queries;

// ── Query ─────────────────────────────────────────────────────────────────────
public record GetViolationsQuery(
    string? NationalNumber,
    string? PlateNumber
) : IRequest<Result<GetViolationsResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public class GetViolationsHandler(IAppDbContext context)
    : IRequestHandler<GetViolationsQuery, Result<GetViolationsResponse>>
{
    public async Task<Result<GetViolationsResponse>> Handle(
        GetViolationsQuery request,
        CancellationToken cancellationToken)
    {
        // يجب توفير رقم الهوية أو رقم المركبة على الأقل
        if (string.IsNullOrWhiteSpace(request.NationalNumber) &&
            string.IsNullOrWhiteSpace(request.PlateNumber))
            return Result<GetViolationsResponse>.Failure(
                "يجب إدخال رقم الهوية الوطنية أو رقم المركبة للاستعلام");

        // ── بناء الاستعلام ديناميكياً ────────────────────────────────
        var query = context.TrafficViolations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.NationalNumber))
            query = query.Where(v => v.CitizenNationalNumber == request.NationalNumber);

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
            query = query.Where(v => v.PlateNumber == request.PlateNumber);

        var violations = await query
            .OrderBy(v => v.Status == "Open" ? 0 : 1)   // المفتوحة أولاً
            .ThenByDescending(v => v.ViolationDate)
            .ToListAsync(cancellationToken);

        if (violations.Count == 0)
            return Result<GetViolationsResponse>.Failure(
                "لا توجد مخالفات مسجلة بهذه البيانات");

        var dtos = violations.Select(v => new ViolationDto(
            Id:              v.Id,
            ViolationNumber: v.ViolationNumber,
            PlateNumber:     v.PlateNumber,
            ViolationType:   v.ViolationType,
            Description:     v.Description,
            Amount:          v.Amount,
            Location:        v.Location,
            ViolationDate:   v.ViolationDate.ToString("dd/MM/yyyy HH:mm"),
            Status:          v.Status
        )).ToList();

        var openViolations = violations.Where(v => v.Status == "Open").ToList();

        return Result<GetViolationsResponse>.Success(new GetViolationsResponse(
            Violations:      dtos,
            TotalCount:      violations.Count,
            OpenCount:       openViolations.Count,
            TotalOpenAmount: openViolations.Sum(v => v.Amount)
        ));
    }
}
