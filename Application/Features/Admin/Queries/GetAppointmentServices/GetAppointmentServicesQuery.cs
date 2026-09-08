using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries.GetAppointmentServices;

// ─── Query ────────────────────────────────────────────────────────────────────
/// <summary>
/// يُرجع فقط الخدمات التي تحتاج موعداً (ServiceType = Appointment).
/// يُستخدم لتعبئة الـ Dropdown في شاشة "إضافة موعد جديد" بلوحة تحكم Admin
/// بدل إدخال Service Id يدوياً كنص حر.
/// </summary>
public sealed record GetAppointmentServicesQuery : IRequest<Result<List<AppointmentServiceOptionDto>>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record AppointmentServiceOptionDto
{
    public required Guid   Id           { get; init; }
    public required string Name         { get; init; }
    public required string MinistryName { get; init; }   // للعرض التوضيحي في الـ Dropdown
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetAppointmentServicesHandler(IAppDbContext context)
    : IRequestHandler<GetAppointmentServicesQuery, Result<List<AppointmentServiceOptionDto>>>
{
    public async Task<Result<List<AppointmentServiceOptionDto>>> Handle(
        GetAppointmentServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await context.GovernmentServices
            .AsNoTracking()
            .Include(s => s.GovernmentEntity)
            .Where(s => s.IsActive && s.ServiceType == ServiceType.Appointment)
            .OrderBy(s => s.Name)
            .Select(s => new AppointmentServiceOptionDto
            {
                Id           = s.Id,
                Name         = s.Name,
                MinistryName = s.GovernmentEntity.Name
            })
            .ToListAsync(cancellationToken);

        return Result<List<AppointmentServiceOptionDto>>.Success(services);
    }
}
