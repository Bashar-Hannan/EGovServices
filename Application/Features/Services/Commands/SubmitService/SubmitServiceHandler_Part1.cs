using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.ServiceSubmission;
using EGovServices.Domain.Entities;
using EGovServices.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EGovServices.Application.Features.Services.Commands.SubmitService;

public sealed record SubmitServiceCommand : IRequest<Result<SubmitServiceResponse>>
{
    public required Guid ServiceId { get; init; }
    public required Guid UserId { get; init; }
    public required Dictionary<string, object> FormData { get; init; }
}

public sealed partial class SubmitServiceHandler(IAppDbContext context)
    : IRequestHandler<SubmitServiceCommand, Result<SubmitServiceResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<SubmitServiceResponse>> Handle(
        SubmitServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await context.GovernmentServices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ServiceId, cancellationToken);

        if (service is null) return Result<SubmitServiceResponse>.Failure("الخدمة غير موجودة");
        if (!service.IsActive) return Result<SubmitServiceResponse>.Failure("الخدمة غير متاحة");

        // استخراج BranchId من FormData
        Guid? branchId = null;
        if (request.FormData.TryGetValue("branchId", out var branchIdRaw)
            && Guid.TryParse(branchIdRaw?.ToString(), out var parsedBranchId))
        {
            branchId = parsedBranchId;
            request.FormData.Remove("branchId");
        }

        if (service.ServiceType == ServiceType.Appointment && branchId is null)
            return Result<SubmitServiceResponse>.Failure("يجب اختيار الفرع للخدمات التي تتطلب حضوراً");

        if (service.ServiceType == ServiceType.Digital && branchId is not null)
            return Result<SubmitServiceResponse>.Failure("الخدمة الإلكترونية لا تحتاج فرعاً");

        var fields = await context.ServiceFormFields.AsNoTracking()
            .Where(f => f.GovernmentServiceId == request.ServiceId && f.IsActive)
            .ToListAsync(cancellationToken);

        if (fields.Count == 0) return Result<SubmitServiceResponse>.Failure("لا توجد حقول لهذه الخدمة");

        var validation = ValidateFormData(request.FormData, fields);
        if (!validation.IsSuccess) return Result<SubmitServiceResponse>.Failure(validation.Error!);

        var referenceNumber = await GenerateReferenceNumber(cancellationToken);

        var serviceRequest = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            GovernmentServiceId = request.ServiceId,
            ReferenceNumber = referenceNumber,
            Status = "PendingPayment",
            SubmissionDate = DateTime.UtcNow,
            BranchId = branchId,
            FormData = JsonSerializer.Serialize(request.FormData, JsonOptions)
        };

        await context.ServiceRequests.AddAsync(serviceRequest, cancellationToken);
        await context.RequestAuditLogs.AddAsync(new RequestAuditLog
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            NewStatus = "PendingPayment",
            Action = "Submitted",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<SubmitServiceResponse>.Success(new SubmitServiceResponse
        {
            RequestId = serviceRequest.Id,
            ReferenceNumber = referenceNumber,
            Status = "PendingPayment",
            SubmissionDate = serviceRequest.SubmissionDate,
            Message = $"تم استلام طلبك بنجاح. رقم المرجع: {referenceNumber}"
        });
    }
}