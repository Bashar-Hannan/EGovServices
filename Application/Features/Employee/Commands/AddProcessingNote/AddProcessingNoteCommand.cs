using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Employee.Commands.AddProcessingNote;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record AddProcessingNoteCommand : IRequest<Result>
{
    public required Guid RequestId { get; init; }
    public required string Note { get; init; }
}

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class AddProcessingNoteHandler(IAppDbContext context)
    : IRequestHandler<AddProcessingNoteCommand, Result>
{
    public async Task<Result> Handle(
        AddProcessingNoteCommand request, CancellationToken cancellationToken)
    {
        var serviceRequest = await context.ServiceRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (serviceRequest is null)
            return Result.Failure("الطلب غير موجود");

        // إضافة الملاحظة مع الطابع الزمني
        var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");
        serviceRequest.ProcessingNotes = string.IsNullOrWhiteSpace(serviceRequest.ProcessingNotes)
            ? $"[{timestamp}] {request.Note}"
            : $"{serviceRequest.ProcessingNotes}\n[{timestamp}] {request.Note}";

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
