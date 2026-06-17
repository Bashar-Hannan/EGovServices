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
    public required string SearchTerm { get; init; }  // الكلمة المدخلة في البار
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record ServiceSearchResultDto
{
    public required Guid        Id              { get; init; }
    public required string      Name            { get; init; }
    public required string      Description     { get; init; }
    public required decimal     ServiceFee      { get; init; }
    public required ServiceType ServiceType     { get; init; }
    public required string      ServiceTypeLabel { get; init; }

    // معلومات الوزارة — مهمة لأن البحث يمتد عبر كل الوزارات
    public required Guid        MinistryId      { get; init; }
    public required string      MinistryName    { get; init; }
}

// ─── Validator ────────────────────────────────────────────────────────────────
public sealed class SearchServicesValidator : AbstractValidator<SearchServicesQuery>
{
    public SearchServicesValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("أدخل كلمة للبحث")
            .MinimumLength(2)
            .WithMessage("كلمة البحث يجب أن تكون حرفين على الأقل");
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

        var results = await context.GovernmentServices
            .AsNoTracking()
            .Include(s => s.GovernmentEntity)
            .Where(s =>
                s.IsActive &&
                s.GovernmentEntity.IsActive &&
                (s.Name.Contains(term) ||
                 s.Description.Contains(term)))
            .OrderBy(s => s.Name)
            .Select(s => new ServiceSearchResultDto
            {
                Id               = s.Id,
                Name             = s.Name,
                Description      = s.Description,
                ServiceFee       = s.ServiceFee,
                ServiceType      = s.ServiceType,
                ServiceTypeLabel = s.ServiceType == ServiceType.Digital
                    ? "إلكتروني بالكامل"
                    : "يتطلب حضوراً",
                MinistryId       = s.GovernmentEntityId,
                MinistryName     = s.GovernmentEntity.Name
            })
            .ToListAsync(cancellationToken);

        return Result<List<ServiceSearchResultDto>>.Success(results);
    }
}
