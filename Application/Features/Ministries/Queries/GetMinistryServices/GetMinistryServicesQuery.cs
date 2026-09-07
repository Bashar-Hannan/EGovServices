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
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Requirements { get; init; }
    public required decimal ServiceFee { get; init; }
    public required ServiceType ServiceType { get; init; }  // Digital | Appointment

    /// <summary>
    /// نص وصفي لنوع الخدمة. للخدمات المرتبطة بنظام الدفع الموحَّد
    /// (PaymentType != null) يُعرض نص خاص بالدفع بدل "إلكتروني بالكامل"
    /// أو "يتطلب حضوراً" — لأن هذين الوصفين مضللان لخدمة دفع فواتير.
    /// </summary>
    public required string ServiceTypeLabel { get; init; }

    /// <summary>
    /// يُستخدم فقط للخدمات المرتبطة بنظام الدفع الموحَّد
    /// (المخالفات المرورية، فواتير الكهرباء). الفرونت يستخدم هذه القيمة
    /// مباشرةً لبناء GET /api/payments/{paymentType}/{referenceNumber}
    /// null لأي خدمة أخرى لا علاقة لها بنظام الدفع.
    /// </summary>
    public string? PaymentType { get; init; }

    /// <summary>نص عربي واضح يُعرض للمستخدم بدل القيمة التقنية.</summary>
    public string? PaymentTypeLabel { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetMinistryServicesHandler(IAppDbContext context)
    : IRequestHandler<GetMinistryServicesQuery, Result<List<MinistryServiceDto>>>
{
    public async Task<Result<List<MinistryServiceDto>>> Handle(
        GetMinistryServicesQuery request, CancellationToken cancellationToken)
    {
        var ministryExists = await context.GovernmentEntities
            .AsNoTracking()
            .AnyAsync(e => e.Id == request.MinistryId && e.IsActive, cancellationToken);

        if (!ministryExists)
            return Result<List<MinistryServiceDto>>.Failure("الوزارة غير موجودة");

        var rawServices = await context.GovernmentServices
            .AsNoTracking()
            .Where(s => s.GovernmentEntityId == request.MinistryId && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.Requirements,
                s.ServiceFee,
                s.ServiceType
            })
            .ToListAsync(cancellationToken);

        var services = rawServices.Select(s =>
        {
            var (paymentType, paymentTypeLabel) = DeterminePaymentType(s.Name);

            return new MinistryServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Requirements = s.Requirements,
                ServiceFee = s.ServiceFee,
                ServiceType = s.ServiceType,
                ServiceTypeLabel = BuildServiceTypeLabel(s.ServiceType, paymentType),
                PaymentType = paymentType,
                PaymentTypeLabel = paymentTypeLabel
            };
        }).ToList();

        return Result<List<MinistryServiceDto>>.Success(services);
    }

    /// <summary>
    /// يحدد نوع الدفع بناءً على اسم الخدمة مباشرةً. أضف حالة جديدة هنا
    /// عند إضافة نوع دفع مستقبلي — وحدّث نفس الدالة في الملفين الآخرين
    /// (SearchServicesQuery, GetServiceFormSchemaHandler).
    /// </summary>
    private static (string? type, string? label) DeterminePaymentType(string serviceName)
    {
        if (serviceName.Contains("مخالف"))
            return ("violation", "المخالفات المرورية");

        if (serviceName.Contains("كهرباء"))
            return ("electricity", "فواتير الكهرباء");

        return (null, null);
    }

    /// <summary>
    /// خدمات الدفع تحصل على تسمية خاصة بها بدل Digital/Appointment
    /// العادية، لأن "يتطلب حضوراً" مضلل لخدمة دفع فاتورة إلكترونياً.
    /// </summary>
    private static string BuildServiceTypeLabel(ServiceType serviceType, string? paymentType)
    {
        if (paymentType is not null)
            return "خدمة دفع إلكتروني";

        return serviceType == ServiceType.Digital
            ? "إلكتروني بالكامل"
            : "يتطلب حضوراً";
    }
}
