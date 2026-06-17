using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.FormSchema;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EGovServices.Application.Features.Services.Queries.GetServiceFormSchema;

public sealed record GetServiceFormSchemaQuery(Guid ServiceId)
    : IRequest<Result<FormSchemaResponse>>;

public sealed class GetServiceFormSchemaHandler(IAppDbContext context)
    : IRequestHandler<GetServiceFormSchemaQuery, Result<FormSchemaResponse>>
{
    public async Task<Result<FormSchemaResponse>> Handle(
        GetServiceFormSchemaQuery request, CancellationToken cancellationToken)
    {
        var service = await context.GovernmentServices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ServiceId, cancellationToken);

        if (service is null)
            return Result<FormSchemaResponse>.Failure("الخدمة المطلوبة غير موجودة");

        if (!service.IsActive)
            return Result<FormSchemaResponse>.Failure("هذه الخدمة غير متاحة حالياً");

        var fields = await context.ServiceFormFields
            .AsNoTracking()
            .Include(f => f.Options.Where(o => o.IsActive))
            .Where(f => f.GovernmentServiceId == request.ServiceId && f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync(cancellationToken);

        var fieldDtos = fields.Select(f => new FormFieldDto
        {
            Name = f.FieldName, Label = f.Label, Type = f.FieldType,
            Required = f.IsRequired, Order = f.DisplayOrder,
            Placeholder = f.Placeholder, DefaultValue = f.DefaultValue, HelpText = f.HelpText,
            Validation = Deserialize<ValidationRulesDto>(f.ValidationRules),
            Metadata = Deserialize<Dictionary<string, object>>(f.Metadata),
            Options = f.Options.Count != 0
                ? f.Options.OrderBy(o => o.DisplayOrder)
                    .Select(o => new FieldOptionDto { Value = o.OptionValue, Label = o.OptionLabel })
                    .ToList()
                : null
        }).ToList();

        return Result<FormSchemaResponse>.Success(new FormSchemaResponse
        {
            ServiceId = service.Id, ServiceName = service.Name,
            Description = service.Description, Requirements = service.Requirements,
            ServiceFee = service.ServiceFee, Fields = fieldDtos
        });
    }

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return default; }
    }
}
