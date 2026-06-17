using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Requests.Queries.GetMyRequests;

// ── Query ─────────────────────────────────────────────────────────────────────
/// <summary>
/// يرجع كل طلبات المواطن الحالي، كل عنصر معه StatusCategory
/// تحدّد أي تبويب ينتمي له في الواجهة (قيد التنفيذ / مكتملة / ملغاة).
/// </summary>
public sealed record GetMyRequestsQuery(Guid UserId)
    : IRequest<Result<List<MyRequestDto>>>;

// ── DTO ───────────────────────────────────────────────────────────────────────
public sealed record MyRequestDto
{
    public required Guid    ServiceRequestId { get; init; }
    public required string  ReferenceNumber  { get; init; }
    public required string  ServiceName      { get; init; }   // "تصديق شهادة جامعية"
    public required string  MinistryName     { get; init; }   // "وزارة التعليم العالي"
    public required string  SubmissionDate   { get; init; }   // مُنسَّق dd/MM/yyyy
    public required string  Status           { get; init; }   // القيمة الخام من DB
    public required string  StatusLabel      { get; init; }   // "بانتظار الموافقة"
    public required string  StatusCategory   { get; init; }   // "InProgress" | "Completed" | "Cancelled"
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class GetMyRequestsHandler(IAppDbContext context)
    : IRequestHandler<GetMyRequestsQuery, Result<List<MyRequestDto>>>
{
    public async Task<Result<List<MyRequestDto>>> Handle(
        GetMyRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var requests = await context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.GovernmentService)
                .ThenInclude(s => s.GovernmentEntity)
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.SubmissionDate)
            .ToListAsync(cancellationToken);

        var dtos = requests.Select(r => new MyRequestDto
        {
            ServiceRequestId = r.Id,
            ReferenceNumber  = r.ReferenceNumber,
            ServiceName      = r.GovernmentService.Name,
            MinistryName     = r.GovernmentService.GovernmentEntity.Name,
            SubmissionDate   = r.SubmissionDate.ToString("dd/MM/yyyy"),
            Status           = r.Status,
            StatusLabel      = GetStatusLabel(r.Status),
            StatusCategory   = GetStatusCategory(r.Status)
        }).ToList();

        return Result<List<MyRequestDto>>.Success(dtos);
    }

    // ── تصنيف الحالة لأحد التبويبات الثلاثة ──────────────────────────
    private static string GetStatusCategory(string status) => status switch
    {
        "Pending"              => "InProgress",
        "Processing"           => "InProgress",
        "DocumentsUnderReview" => "InProgress",
        "AppointmentConfirmed" => "InProgress",
        "Completed"            => "Completed",
        "Rejected"             => "Cancelled",
        "AppointmentCancelled" => "Cancelled",
        _                      => "InProgress"
    };

    // ── النص الدقيق الذي يظهر في الشارة (Badge) ──────────────────────
    private static string GetStatusLabel(string status) => status switch
    {
        "Pending"              => "بانتظار الموافقة",
        "Processing"           => "قيد المعالجة",
        "DocumentsUnderReview" => "قيد مراجعة المستندات",
        "AppointmentConfirmed" => "تم تأكيد الموعد",
        "Completed"            => "مكتمل",
        "Rejected"             => "مرفوض",
        "AppointmentCancelled" => "تم الإلغاء",
        _                      => status
    };
}
