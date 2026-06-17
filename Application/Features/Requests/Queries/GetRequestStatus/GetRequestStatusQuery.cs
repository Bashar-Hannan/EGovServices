using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using EGovServices.Application.DTOs.RequestStatus;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Requests.Queries.GetRequestStatus;

// ── Query ─────────────────────────────────────────────────────────────────────
// UserId مأخوذ من الـ JWT في الـ Controller — المواطن يرى طلباته فقط
public record GetRequestStatusQuery(
    string ReferenceNumber,
    Guid UserId
) : IRequest<Result<RequestStatusDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public class GetRequestStatusHandler
    : IRequestHandler<GetRequestStatusQuery, Result<RequestStatusDto>>
{
    private readonly IAppDbContext _context;

    public GetRequestStatusHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RequestStatusDto>> Handle(
        GetRequestStatusQuery request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.GovernmentService)
            .Include(r => r.AuditLogs.OrderBy(a => a.CreatedAt))
            .FirstOrDefaultAsync(r => r.ReferenceNumber == request.ReferenceNumber, cancellationToken);

        if (serviceRequest is null)
            return Result<RequestStatusDto>.Failure("الطلب غير موجود");

        // التحقق من أن المواطن يطلع على طلبه فقط — مطابق لنمط الـ JWT في المشروع
        if (serviceRequest.UserId != request.UserId)
            return Result<RequestStatusDto>.Failure("غير مصرح لك بعرض هذا الطلب");

        var dto = new RequestStatusDto(
            ReferenceNumber:     serviceRequest.ReferenceNumber,
            ServiceName:         serviceRequest.GovernmentService.Name,
            CurrentStatus:       serviceRequest.Status,
            CurrentStatusArabic: GetArabicStatus(serviceRequest.Status),
            SubmissionDate:      serviceRequest.SubmissionDate,
            History:             serviceRequest.AuditLogs.Select(log => new StatusHistoryItemDto(
                OldStatus: log.OldStatus,
                NewStatus: log.NewStatus,
                Notes:     log.Notes,
                UpdatedAt: log.CreatedAt
            )).ToList()
        );

        return Result<RequestStatusDto>.Success(dto);
    }

    private static string GetArabicStatus(string status) => status switch
    {
        "PendingPayment" => "في انتظار الدفع",
        "Submitted"      => "تم التقديم",
        "UnderReview"    => "قيد المراجعة",
        "Approved"       => "موافق عليه",
        "Rejected"       => "مرفوض",
        "Processing"     => "قيد المعالجة",
        "Completed"      => "مكتمل",
        _                => status
    };
}
