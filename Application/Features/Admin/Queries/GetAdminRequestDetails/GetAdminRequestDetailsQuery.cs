using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Queries.GetAdminRequestDetails;

// ─── Query ────────────────────────────────────────────────────────────────────
public sealed record GetAdminRequestDetailsQuery(Guid RequestId)
    : IRequest<Result<AdminRequestDetailsDto>>;

// ─── DTOs ─────────────────────────────────────────────────────────────────────
public sealed record AdminRequestDetailsDto
{
    public required Guid Id { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string Status { get; init; }
    public required DateTime SubmissionDate { get; init; }
    public required string ServiceName { get; init; }
    public required string CitizenName { get; init; }
    public required string CitizenNationalNumber { get; init; }
    public required string FormData { get; init; }
    public string? ProcessingNotes { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required List<AuditLogDto> AuditLogs { get; init; }
    public required List<AttachmentDto> Attachments { get; init; }
}

public sealed record AuditLogDto
{
    public required string Action { get; init; }
    public required string NewStatus { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? ChangedByName { get; init; }   // اسم الموظف/المسؤول
}

public sealed record AttachmentDto
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string FileType { get; init; }
    public required long FileSizeBytes { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class GetAdminRequestDetailsHandler(IAppDbContext context)
    : IRequestHandler<GetAdminRequestDetailsQuery, Result<AdminRequestDetailsDto>>
{
    public async Task<Result<AdminRequestDetailsDto>> Handle(
        GetAdminRequestDetailsQuery request, CancellationToken cancellationToken)
    {
        var req = await context.ServiceRequests
            .AsNoTracking()
            .Include(r => r.User).ThenInclude(u => u.Citizen)
            .Include(r => r.GovernmentService)
            .Include(r => r.AuditLogs).ThenInclude(a => a.ChangedByUser).ThenInclude(u => u!.Citizen)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (req is null)
            return Result<AdminRequestDetailsDto>.Failure("الطلب غير موجود");

        return Result<AdminRequestDetailsDto>.Success(new AdminRequestDetailsDto
        {
            Id = req.Id,
            ReferenceNumber = req.ReferenceNumber,
            Status = req.Status,
            SubmissionDate = req.SubmissionDate,
            ServiceName = req.GovernmentService.Name,
            CitizenName = req.User.Citizen != null
                ? $"{req.User.Citizen.FirstName} {req.User.Citizen.LastName}"
                : req.User.NationalNumber,
            CitizenNationalNumber = req.User.NationalNumber,
            FormData = req.FormData,
            ProcessingNotes = req.ProcessingNotes,
            RejectionReason = req.RejectionReason,
            CompletedAt = req.CompletedAt,
            AuditLogs = req.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AuditLogDto
                {
                    Action = a.Action,
                    NewStatus = a.NewStatus,
                    CreatedAt = a.CreatedAt,
                    ChangedByName = a.ChangedByUser?.Citizen != null
                        ? $"{a.ChangedByUser.Citizen.FirstName} {a.ChangedByUser.Citizen.LastName}"
                        : a.ChangedByUser?.NationalNumber
                }).ToList(),
            Attachments = req.Attachments
                .Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileType = a.FileType,
                    FileSizeBytes = a.FileSizeBytes
                }).ToList()
        });
    }
}
