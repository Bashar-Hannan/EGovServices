using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Services.Queries.SearchServices;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record SearchServicesQuery : IRequest<Result<List<ServiceSearchResultDto>>>
{
    public required string SearchTerm { get; init; }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record ServiceSearchResultDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal ServiceFee { get; init; }
    public required ServiceType ServiceType { get; init; }
    public required string ServiceTypeLabel { get; init; }

    public required Guid MinistryId { get; init; }
    public required string MinistryName { get; init; }

    public string? PaymentType { get; init; }
    public string? PaymentTypeLabel { get; init; }
}

// ─── Validator ────────────────────────────────────────────────────────────────
public sealed class SearchServicesValidator : AbstractValidator<SearchServicesQuery>
{
    public SearchServicesValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("أدخل كلمة للبحث")
            .MinimumLength(2).WithMessage("كلمة البحث يجب أن تكون حرفين على الأقل");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class SearchServicesHandler(IAppDbContext context)
    : IRequestHandler<SearchServicesQuery, Result<List<ServiceSearchResultDto>>>
{
    public async Task<Result<List<ServiceSearchResultDto>>> Handle(
        SearchServicesQuery request, CancellationToken cancellationToken)
    {
        var term = request.SearchTerm.Trim();

        var rawResults = await context.GovernmentServices
            .AsNoTracking()
            .Include(s => s.GovernmentEntity)
            .Where(s =>
                s.IsActive &&
                s.GovernmentEntity.IsActive &&
                (s.Name.Contains(term) || s.Description.Contains(term)))
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.ServiceFee,
                s.ServiceType,
                MinistryId = s.GovernmentEntityId,
                MinistryName = s.GovernmentEntity.Name
            })
            .ToListAsync(cancellationToken);

        var results = rawResults.Select(s =>
        {
            var (paymentType, paymentTypeLabel) = DeterminePaymentType(s.Name);

            return new ServiceSearchResultDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                ServiceFee = s.ServiceFee,
                ServiceType = s.ServiceType,
                ServiceTypeLabel = BuildServiceTypeLabel(s.ServiceType, paymentType),
                MinistryId = s.MinistryId,
                MinistryName = s.MinistryName,
                PaymentType = paymentType,
                PaymentTypeLabel = paymentTypeLabel
            };
        }).ToList();

        return Result<List<ServiceSearchResultDto>>.Success(results);
    }

    /// <summary>نفس المنطق في GetMinistryServicesQuery — حافظ على تطابقهما</summary>
    private static (string? type, string? label) DeterminePaymentType(string serviceName)
    {
        if (serviceName.Contains("مخالف"))
            return ("violation", "المخالفات المرورية");

        if (serviceName.Contains("كهرباء"))
            return ("electricity", "فواتير الكهرباء");

        return (null, null);
    }

    /// <summary>نفس المنطق في GetMinistryServicesQuery — حافظ على تطابقهما</summary>
    private static string BuildServiceTypeLabel(ServiceType serviceType, string? paymentType)
    {
        if (paymentType is not null)
            return "خدمة دفع إلكتروني";

        if (serviceType == ServiceType.Directory)
            return "دليل على الخريطة";

        return serviceType == ServiceType.Digital
            ? "إلكتروني بالكامل"
            : "يتطلب حضوراً";
    }
}
