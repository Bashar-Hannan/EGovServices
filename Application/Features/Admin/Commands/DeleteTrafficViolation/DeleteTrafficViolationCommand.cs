using EGovServices.Application.Common;
using EGovServices.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EGovServices.Application.Features.Admin.Commands.DeleteTrafficViolation;

// ─── Command ──────────────────────────────────────────────────────────────────
public sealed record DeleteTrafficViolationCommand(Guid ViolationId)
    : IRequest<Result<DeleteTrafficViolationResponse>>;

// ─── DTO ──────────────────────────────────────────────────────────────────────
public sealed record DeleteTrafficViolationResponse(
    Guid ViolationId,
    string ViolationNumber,
    string Message
);

// ─── Handler ──────────────────────────────────────────────────────────────────
public sealed class DeleteTrafficViolationHandler(IAppDbContext context)
    : IRequestHandler<DeleteTrafficViolationCommand, Result<DeleteTrafficViolationResponse>>
{
    public async Task<Result<DeleteTrafficViolationResponse>> Handle(
        DeleteTrafficViolationCommand request, CancellationToken cancellationToken)
    {
        var violation = await context.TrafficViolations
            .FirstOrDefaultAsync(v => v.Id == request.ViolationId, cancellationToken);

        if (violation is null)
            return Result<DeleteTrafficViolationResponse>.Failure("المخالفة غير موجودة");

        // ⚠️ لا يُسمح بحذف مخالفة سبق ودفعها المواطن — لضمان عدم ضياع
        // الربط بسجل الدفع (WalletTransaction) وسجل التدقيق المالي.
        if (violation.Status == "Paid")
            return Result<DeleteTrafficViolationResponse>.Failure(
                "لا يمكن حذف مخالفة مدفوعة مسبقاً — هذه المخالفة مرتبطة بعملية دفع فعلية");

        var violationNumber = violation.ViolationNumber;

        context.TrafficViolations.Remove(violation);
        await context.SaveChangesAsync(cancellationToken);

        return Result<DeleteTrafficViolationResponse>.Success(new DeleteTrafficViolationResponse(
            ViolationId: request.ViolationId,
            ViolationNumber: violationNumber,
            Message: $"تم حذف المخالفة {violationNumber} بنجاح"
        ));
    }
}
